using System;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Media;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Size = System.Drawing.Size;

namespace XOCracker
{
    /// <summary>
    /// Логика взаимодействия для ScreenshotRegion.xaml
    /// </summary>
    public partial class ScreenshotRegion
    {
        public static readonly RoutedCommand MyCommand = new RoutedCommand();
        private readonly RectangleGeometry _region;
        private const int MinImgSize = 6;
        private const int MagicShift = 7;
        private double _startX;
        private double _startY;
        private bool _isMouseDown;
        public Bitmap Picture;

        public ScreenshotRegion()
        {
            InitializeComponent();
            MyCommand.InputGestures.Add(new KeyGesture(Key.Escape));
            var screen = Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(this).Handle);
            var background = new RectangleGeometry(
                new Rect(MagicShift, MagicShift, screen.Bounds.Width, screen.Bounds.Height));
            _region = new RectangleGeometry(new Rect(0, 0, 0, 0));
            var regiondata = new CombinedGeometry(background, _region)
            {
                GeometryCombineMode = GeometryCombineMode.Exclude,
            };
            Cnv.Data = regiondata;
        }

        private void AbortCommand(object sender, ExecutedRoutedEventArgs executedRoutedEventArgs)
        {
            if (_isMouseDown)
            {
                _isMouseDown = false;
                _region.Rect = new Rect();
                Canvas.Children.Clear();
            }
            else
            {
                DialogResult = false;
                Close();
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _isMouseDown = true;
            _startX = e.GetPosition(null).X;
            _startY = e.GetPosition(null).Y;
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isMouseDown)
            {
                double curX = e.GetPosition(null).X;
                double curY = e.GetPosition(null).Y;
                var width = Math.Abs(curX - _startX);
                var height = Math.Abs(curY - _startY);
                _region.Rect = new Rect(GetMin(_startX, curX), GetMin(_startY, curY), width, height);
                var regionBorder = new System.Windows.Shapes.Rectangle();
                SolidColorBrush brush = new SolidColorBrush(Colors.White) { Opacity = 0.5 };
                regionBorder.Stroke = brush;
                regionBorder.StrokeThickness = 1;
                regionBorder.Width = width;
                regionBorder.Height = height;
                Canvas.Children.Clear();
                Canvas.Children.Add(regionBorder);
                System.Windows.Controls.Canvas.SetLeft(regionBorder, _region.Rect.X);
                System.Windows.Controls.Canvas.SetTop(regionBorder, _region.Rect.Y);
                if (e.LeftButton == MouseButtonState.Released)
                {
                    if (width >= MinImgSize || height >= MinImgSize)
                    {
                        CaptureScreen(GetMin(_startX, curX), GetMin(_startY, curY), width, height);
                        _startX = _startY = 0;
                        _isMouseDown = false;
                        DialogResult = true;
                        Close();
                    }
                }
            }
        }

        private double GetMin(double a, double b)
        {
            return a < b ? a : b;
        }

        private void CaptureScreen(double x, double y, double width, double height)
        {
            var ix = Convert.ToInt32(x) - MagicShift;
            var iy = Convert.ToInt32(y) - MagicShift;
            var iw = Convert.ToInt32(width);
            var ih = Convert.ToInt32(height);
            Bitmap image = new Bitmap(iw, ih, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var gr = Graphics.FromImage(image))
            {
                gr.CopyFromScreen(ix, iy, 0, 0, new Size(iw, ih));
            }

            Picture = image;
        }

        public static BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            if (bitmap == null)
            {
                return null;
            }

            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();
                return bitmapimage;
            }
        }

        internal static class NativeMethods
        {

            [DllImport("user32.dll")]
            public static extern IntPtr GetDesktopWindow();
            [DllImport("user32.dll")]
            public static extern IntPtr GetWindowDC(IntPtr hwnd);
            [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
            public static extern IntPtr GetForegroundWindow();
            [DllImport("gdi32.dll")]
            public static extern UInt64 BitBlt(IntPtr hDestDc, int x, int y, int nWidth, int nHeight, IntPtr hSrcDc, int xSrc, int ySrc, Int32 dwRop);

            [DllImport("User32.dll")]
            public static extern IntPtr GetDC(IntPtr hwnd);
            [DllImport("User32.dll")]
            public static extern void ReleaseDC(IntPtr hwnd, IntPtr dc);
        }
    }
}
