using Extras;
using nuitrack;
using nuitrack.issues;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace NuitrackRealSenseSample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public class DirectBitmap : IDisposable
    {
        public Bitmap Bitmap { get; private set; }

        public Int32[] Bits { get; private set; }
        public bool Disposed { get; private set; }
        public int Height { get; private set; }
        public int Width { get; private set; }

        protected GCHandle BitsHandle { get; private set; }

        public DirectBitmap(int width, int height)
        {
            Width = width;
            Height = height;
            Bits = new Int32[width * height];
            BitsHandle = GCHandle.Alloc(Bits, GCHandleType.Pinned);
            Bitmap = new Bitmap(width, height, width * 4, System.Drawing.Imaging.PixelFormat.Format32bppPArgb, BitsHandle.AddrOfPinnedObject());
        }

        public void SetPixel(int x, int y, System.Drawing.Color colour)
        {
            int index = x + (y * Width);
            int col = colour.ToArgb();

            Bits[index] = col;
        }

        public System.Drawing.Color GetPixel(int x, int y)
        {
            int index = x + (y * Width);
            int col = Bits[index];
            System.Drawing.Color result = System.Drawing.Color.FromArgb(col);

            return result;
        }

        public void Dispose()
        {
            if (Disposed)
                return;
            Disposed = true;
            Bitmap.Dispose();
            BitsHandle.Free();
        }
    }
    public partial class MainWindow : Window
    {
        private DirectBitmap _bitmap;
        private bool _visualizeColorImage = false;
        private bool _colorStreamEnabled = false;

        private DepthSensor _depthSensor;
        private ColorSensor _colorSensor;
        private UserTracker _userTracker;
        private SkeletonTracker _skeletonTracker;
        private GestureRecognizer _gestureRecognizer;
        private HandTracker _handTracker;

        private DepthFrame _depthFrame;
        private SkeletonData _skeletonData;
        private HandTrackerData _handTrackerData;
        private IssuesData _issuesData = null;
        DispatcherTimer timer = new DispatcherTimer();

        public MainWindow()
        {
            InitializeComponent();

            try
            {
                Nuitrack.Init();
            }
            catch (nuitrack.Exception exception)
            {
                Debug.WriteLine("Can not initialize Nuitrack. Exception: ", exception);
            }

            try
            {
                // Create and setup all required modules
                _depthSensor = DepthSensor.Create();
                _colorSensor = ColorSensor.Create();
                _userTracker = UserTracker.Create();
                _skeletonTracker = SkeletonTracker.Create();
                _handTracker = HandTracker.Create();
                _gestureRecognizer = GestureRecognizer.Create();
            }
            catch (nuitrack.Exception exception)
            {
                Debug.WriteLine("Cannot create Nuitrack module.");
                throw exception;
            }

            // Add event handlers for all modules
            _depthSensor.OnUpdateEvent += onDepthSensorUpdate;
            _colorSensor.OnUpdateEvent += onColorSensorUpdate;
            _userTracker.OnUpdateEvent += onUserTrackerUpdate;
            _skeletonTracker.OnSkeletonUpdateEvent += onSkeletonUpdate;
            _handTracker.OnUpdateEvent += onHandTrackerUpdate;
            _gestureRecognizer.OnNewGesturesEvent += onNewGestures;

            // Add an event handler for the IssueUpdate event 
            Nuitrack.onIssueUpdateEvent += onIssueDataUpdate;

            // Create and configure the Bitmap object according to the depth sensor output mode
            OutputMode mode = _depthSensor.GetOutputMode();
            OutputMode colorMode = _colorSensor.GetOutputMode();

            if (mode.XRes < colorMode.XRes)
                mode.XRes = colorMode.XRes;
            if (mode.YRes < colorMode.YRes)
                mode.YRes = colorMode.YRes;
            _bitmap = new DirectBitmap(mode.XRes, mode.YRes);

            for (int y = 0; y < mode.YRes; ++y)
            {
                for (int x = 0; x < mode.XRes; ++x)
                    _bitmap.SetPixel(x, y, System.Drawing.Color.FromKnownColor(KnownColor.Aqua));
            }

        }

        private void NuitrackInitButton_Click(object sender, RoutedEventArgs e)
        {
            //Nuitrack.Init();
            TextBlock.Text = "Nuitrack.Init Success";

            // Run Nuitrack. This starts sensor data processing.
            try
            {
                Nuitrack.Run();
            }
            catch (nuitrack.Exception exception)
            {
                Debug.WriteLine("Cannot start Nuitrack.");
                throw exception;
            }

            timer.Interval = new TimeSpan(0, 0, 0, 0, 10);
            timer.Tick += new EventHandler(ProcessingThread);
            timer.Start();
        }

        private void ProcessingThread(object sender, EventArgs e)
        {
            Thread.CurrentThread.Priority = ThreadPriority.Highest;
            // Update Nuitrack data. Data will be synchronized with skeleton time stamps.
            try
            {
                Nuitrack.Update(_skeletonTracker);//
            }
            catch (LicenseNotAcquiredException exception)
            {
                Debug.WriteLine("LicenseNotAcquired exception. Exception: ", exception);
                throw exception;
            }
            catch (nuitrack.Exception exception)
            {
                Debug.WriteLine("Nuitrack update failed. Exception: ", exception);
            }

            Dispatcher.Invoke(DispatcherPriority.Render, new TimeSpan(0, 0, 0, 0, 200), (Action)(() =>
            {
                // Visualize depth or color image
                if (_bitmap != null) { imgView.Source = Utilities.BitmapToBitmapSource(_bitmap.Bitmap); }

                // Draw skeleton joints
                if (_skeletonData != null)
                {
                    canvas.Children.Clear();
                    foreach (var skeleton in _skeletonData.Skeletons)
                    {
                        canvas.DrawSkeleton(skeleton, _bitmap.Bitmap);
                    }
                }
            }));
        }

        // Nuitrack depth sensor module data update callback
        private void onDepthSensorUpdate(DepthFrame depthFrame)
        {
            _depthFrame = depthFrame;
        }

        private void onIssueDataUpdate(IssuesData issuesData)
        {
            _issuesData = issuesData;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Release Nuitrack and remove all modules
            try
            {
                Nuitrack.onIssueUpdateEvent -= onIssueDataUpdate;

                _depthSensor.OnUpdateEvent -= onDepthSensorUpdate;
                _colorSensor.OnUpdateEvent -= onColorSensorUpdate;
                _userTracker.OnUpdateEvent -= onUserTrackerUpdate;
                _skeletonTracker.OnSkeletonUpdateEvent -= onSkeletonUpdate;
                _handTracker.OnUpdateEvent -= onHandTrackerUpdate;
                _gestureRecognizer.OnNewGesturesEvent -= onNewGestures;

                Nuitrack.Release();
            }
            catch (nuitrack.Exception exception)
            {
                Debug.WriteLine("Nuitrack release failed.");
                throw exception;
            }
        }

        // Event handler for the ColorSensorUpdate event
        private void onColorSensorUpdate(ColorFrame colorFrame)
        {
            if (!_visualizeColorImage)
                return;

            _colorStreamEnabled = true;

            float wStep = (float)_bitmap.Width / colorFrame.Cols;
            float hStep = (float)_bitmap.Height / colorFrame.Rows;

            float nextVerticalBorder = hStep;

            Byte[] data = colorFrame.Data;
            int colorPtr = 0;
            int bitmapPtr = 0;
            const int elemSizeInBytes = 3;

            for (int i = 0; i < _bitmap.Height; ++i)
            {
                if (i == (int)nextVerticalBorder)
                {
                    colorPtr += colorFrame.Cols * elemSizeInBytes;
                    nextVerticalBorder += hStep;
                }

                int offset = 0;
                int argb = data[colorPtr]
                    | (data[colorPtr + 1] << 8)
                    | (data[colorPtr + 2] << 16)
                    | (0xFF << 24);
                float nextHorizontalBorder = wStep;
                for (int j = 0; j < _bitmap.Width; ++j)
                {
                    if (j == (int)nextHorizontalBorder)
                    {
                        offset += elemSizeInBytes;
                        argb = data[colorPtr + offset]
                            | (data[colorPtr + offset + 1] << 8)
                            | (data[colorPtr + offset + 2] << 16)
                            | (0xFF << 24);
                        nextHorizontalBorder += wStep;
                    }

                    _bitmap.Bits[bitmapPtr++] = argb;
                }
            }
        }

        // Event handler for the UserTrackerUpdate event
        private void onUserTrackerUpdate(UserFrame userFrame)
        {
            if (_visualizeColorImage && _colorStreamEnabled)
                return;
            if (_depthFrame == null)
                return;

            const int MAX_LABELS = 7;
            bool[] labelIssueState = new bool[MAX_LABELS];
            for (UInt16 label = 0; label < MAX_LABELS; ++label)
            {
                labelIssueState[label] = false;
                if (_issuesData != null)
                {
                    FrameBorderIssue frameBorderIssue = _issuesData.GetUserIssue<FrameBorderIssue>(label);
                    labelIssueState[label] = (frameBorderIssue != null);
                }
            }

            float wStep = (float)_bitmap.Width / _depthFrame.Cols;
            float hStep = (float)_bitmap.Height / _depthFrame.Rows;

            float nextVerticalBorder = hStep;

            Byte[] dataDepth = _depthFrame.Data;
            Byte[] dataUser = userFrame.Data;
            int dataPtr = 0;
            int bitmapPtr = 0;
            const int elemSizeInBytes = 2;
            for (int i = 0; i < _bitmap.Height; ++i)
            {
                if (i == (int)nextVerticalBorder)
                {
                    dataPtr += _depthFrame.Cols * elemSizeInBytes;
                    nextVerticalBorder += hStep;
                }

                int offset = 0;
                int argb = 0;
                int label = dataUser[dataPtr] | dataUser[dataPtr + 1] << 8;
                int depth = Math.Min(255, (dataDepth[dataPtr] | dataDepth[dataPtr + 1] << 8) / 32);
                float nextHorizontalBorder = wStep;
                for (int j = 0; j < _bitmap.Width; ++j)
                {
                    if (j == (int)nextHorizontalBorder)
                    {
                        offset += elemSizeInBytes;
                        label = dataUser[dataPtr + offset] | dataUser[dataPtr + offset + 1] << 8;
                        if (label == 0)
                            depth = Math.Min(255, (dataDepth[dataPtr + offset] | dataDepth[dataPtr + offset + 1] << 8) / 32);
                        nextHorizontalBorder += wStep;
                    }

                    if (label > 0)
                    {
                        int user = label * 40;
                        if (!labelIssueState[label])
                            user += 40;
                        argb = 0 | (user << 8) | (0 << 16) | (0xFF << 24);
                    }
                    else
                    {
                        argb = depth | (depth << 8) | (depth << 16) | (0xFF << 24);
                    }

                    _bitmap.Bits[bitmapPtr++] = argb;
                }
            }

        }

        // Event handler for the SkeletonUpdate event
        private void onSkeletonUpdate(SkeletonData skeletonData)
        {
            _skeletonData = skeletonData;
        }

        // Event handler for the HandTrackerUpdate event
        private void onHandTrackerUpdate(HandTrackerData handTrackerData)
        {
            _handTrackerData = handTrackerData;

            if (_handTrackerData != null)
            {
                foreach (var hand in _handTrackerData.UsersHands)
                {
                    if (hand.RightHand != null && hand.LeftHand != null)
                    {
                        Debug.WriteLine("Left hand {0} | Right hand {1}", hand.LeftHand.Value.Pressure.ToString(), hand.RightHand.Value.Pressure.ToString());
                    }
                }
            }
        }

        // Event handler for the gesture detection event
        private void onNewGestures(GestureData gestureData)
        {
            // Display the information about detected gestures in the Debug
            foreach (var gesture in gestureData.Gestures)
                Debug.WriteLine("Recognized {0} from user {1}", gesture.Type.ToString(), gesture.UserID);
        }

        //Change between depth and color 
        private void btnChangeView_Click(object sender, RoutedEventArgs e)
        {
            _visualizeColorImage = !_visualizeColorImage;
        }
    }
}
