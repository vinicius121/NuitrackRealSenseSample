using System;
using System.Windows.Media.Imaging;
using System.Drawing;
using nuitrack;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Media;

namespace Extras
{
    public static class Utilities
    {
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr handle);
        public static BitmapSource bitmapSource;
        public static IntPtr intPointer;
        public static BitmapSource BitmapToBitmapSource(System.Drawing.Bitmap bitmap)
        {
            intPointer = bitmap.GetHbitmap();

            bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(intPointer,
                IntPtr.Zero,
                System.Windows.Int32Rect.Empty,
                System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
            
            DeleteObject(intPointer);
            return bitmapSource;
        }

        #region Drawing
        public static void DrawSkeleton(this Canvas canvas, Skeleton body, Bitmap bitmap)
        {
            if (body == null) return;

            foreach (Joint joint in body.Joints)
            {
                if(joint.Confidence > 0)
                {
                    canvas.DrawPoint(joint, bitmap);
                }               
            }
            
            canvas.DrawLine(body.Joints[(int)JointType.Head], body.Joints[(int)JointType.Neck], bitmap);
            canvas.DrawLine(body.Joints[(int)JointType.Neck], body.Joints[(int)JointType.LeftCollar], bitmap);
            canvas.DrawLine(body.Joints[(int)JointType.Neck], body.Joints[(int)JointType.RightCollar], bitmap);
            canvas.DrawLine(body.Joints[(int)JointType.Neck], body.Joints[(int)JointType.Torso], bitmap);
            canvas.DrawLine(body.Joints[(int)JointType.LeftCollar], body.Joints[(int)JointType.LeftShoulder], bitmap);
            canvas.DrawLine(body.Joints[(int)JointType.RightCollar], body.Joints[(int)JointType.RightShoulder], bitmap);
            canvas.DrawLine(body.Joints[(int)JointType.LeftShoulder], body.Joints[(int)JointType.LeftElbow], bitmap);
            canvas.DrawLine(body.Joints[(int)JointType.RightShoulder], body.Joints[(int)JointType.RightElbow], bitmap);
            canvas.DrawLine(body.Joints[(int)JointType.LeftElbow], body.Joints[(int)JointType.LeftWrist], bitmap);
            canvas.DrawLine(body.Joints[(int)JointType.RightElbow], body.Joints[(int)JointType.RightWrist], bitmap);
            canvas.DrawLine(body.Joints[(int)JointType.LeftWrist], body.Joints[(int)JointType.LeftHand], bitmap);
            canvas.DrawLine(body.Joints[(int)JointType.RightWrist], body.Joints[(int)JointType.RightHand], bitmap);
            //canvas.DrawLine(body.Joints[(int)JointType.LeftHand], body.Joints[(int)JointType.LeftFingertip], bitmap);
            //canvas.DrawLine(body.Joints[(int)JointType.RightHand], body.Joints[(int)JointType.RightFingertip], bitmap);
            canvas.DrawLine(body.Joints[(int)JointType.Torso], body.Joints[(int)JointType.Waist], bitmap);
            canvas.DrawLine(body.Joints[(int)JointType.Waist], body.Joints[(int)JointType.LeftHip], bitmap);
            canvas.DrawLine(body.Joints[(int)JointType.Waist], body.Joints[(int)JointType.RightHip], bitmap);
            canvas.DrawLine(body.Joints[(int)JointType.LeftHip], body.Joints[(int)JointType.LeftKnee], bitmap);
            canvas.DrawLine(body.Joints[(int)JointType.RightHip], body.Joints[(int)JointType.RightKnee], bitmap);
            canvas.DrawLine(body.Joints[(int)JointType.LeftKnee], body.Joints[(int)JointType.LeftAnkle], bitmap);
            canvas.DrawLine(body.Joints[(int)JointType.RightKnee], body.Joints[(int)JointType.RightAnkle], bitmap);
            //canvas.DrawLine(body.Joints[(int)JointType.LeftAnkle], body.Joints[(int)JointType.LeftFoot], bitmap);
            //canvas.DrawLine(body.Joints[(int)JointType.RightAnkle], body.Joints[(int)JointType.RightFoot], bitmap);
        }

        public static void DrawPoint(this Canvas canvas, Joint joint, Bitmap bitmap)
        {
            if(joint.Type == JointType.None) { return; }
            Ellipse ellipse = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = new SolidColorBrush(Colors.LightSkyBlue)
            };

            Canvas.SetLeft(ellipse, joint.Proj.X * bitmap.Width - ellipse.Width / 2);
            Canvas.SetTop(ellipse, joint.Proj.Y * bitmap.Height - ellipse.Height / 2);

            canvas.Children.Add(ellipse);
        }

        public static void DrawLine(this Canvas canvas, Joint first, Joint second, Bitmap bitmap)
        {           
            Line line = new Line
            {
                X1 = first.Proj.X * bitmap.Width,
                Y1 = first.Proj.Y * bitmap.Height,
                X2 = second.Proj.X * bitmap.Width,
                Y2 = second.Proj.Y * bitmap.Height,
                StrokeThickness = 5,
                Stroke = new SolidColorBrush(Colors.Red)
            };

            canvas.Children.Add(line);
        }
        #endregion
    }
}
