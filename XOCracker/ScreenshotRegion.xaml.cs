using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Media;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace XOCracker
{
    /// <summary>
    /// Логика взаимодействия для ScreenshotRegion.xaml
    /// </summary>
    public partial class ScreenshotRegion
    {
        public static readonly RoutedCommand MyCommand = new RoutedCommand();
        private readonly Window _parentWindow;
        private readonly RectangleGeometry _region;
        private readonly WindowState _prewParentState;
        private const int MinImgSize = 6;
        private const int MagicShift = 7;
        private double _startX;
        private double _startY;
        private double _curX;
        private double _curY;
        private bool _isMouseDown;
        public Bitmap Picture;
        private double _width;
        private double _height;

        public ScreenshotRegion(Window parentWindow)
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
            _parentWindow = parentWindow;
            _prewParentState = _parentWindow.WindowState;
            _parentWindow.WindowState = WindowState.Minimized;
            Owner = _parentWindow;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            _parentWindow.WindowState = _prewParentState;
            _parentWindow.Topmost = true;
            _parentWindow.Topmost = false;
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
            _curX = _startX = e.GetPosition(null).X;
            _curY = _startY = e.GetPosition(null).Y;
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isMouseDown)
            {
                _curX = e.GetPosition(null).X;
                _curY = e.GetPosition(null).Y;
                _width = Math.Abs(_curX - _startX);
                _height = Math.Abs(_curY - _startY);
                _region.Rect = new Rect(GetMin(_startX, _curX), GetMin(_startY, _curY), _width, _height);
                var regionBorder = new System.Windows.Shapes.Rectangle();
                SolidColorBrush brush = new SolidColorBrush(Colors.White) { Opacity = 0.5 };
                regionBorder.Stroke = brush;
                regionBorder.StrokeThickness = 1;
                regionBorder.Width = _width;
                regionBorder.Height = _height;
                Canvas.Children.Clear();
                Canvas.Children.Add(regionBorder);
                System.Windows.Controls.Canvas.SetLeft(regionBorder, _region.Rect.X);
                System.Windows.Controls.Canvas.SetTop(regionBorder, _region.Rect.Y);
                Screener_MouseUp(e.LeftButton);
            }
        }

        private Bitmap PixelTraceCapture(int curX, int curY)
        {
            var screen = Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(this).Handle);
            var screenshot = CaptureScreen(0, 0, screen.Bounds.Width, screen.Bounds.Height);
            var analyzedArea = new bool[screenshot.Width, screenshot.Height];
            var topX = curX;
            var topY = curY;
            var downX = curX;
            var downY = curY;
            var x = curX;
            var y = curY;
            var piColor = screenshot.GetPixel(curX, curY);
            var pixelQueue = new Queue<Point>();
            do
            {
                if (topX > x) { topX = x; }
                if (topY > y) { topY = y; }
                if (downX < x) { downX = x; }
                if (downY < y) { downY = y; }
                if (screenshot.GetPixel(x, y) != piColor)
                {
                    var curPoint = pixelQueue.Dequeue();
                    x = curPoint.X;
                    y = curPoint.Y;
                    continue;
                }

                if (x - 1 >= 0 && !analyzedArea[x - 1, y])
                {
                    pixelQueue.Enqueue(new Point(x - 1, y));
                    analyzedArea[x - 1, y] = true;
                }
                if (x + 1 < screen.Bounds.Width && !analyzedArea[x + 1, y])
                {
                    pixelQueue.Enqueue(new Point(x + 1, y));
                    analyzedArea[x + 1, y] = true;
                }
                if (y - 1 >= 0 && !analyzedArea[x, y - 1])
                {
                    pixelQueue.Enqueue(new Point(x, y - 1));
                    analyzedArea[x, y - 1] = true;
                }
                if (y + 1 < screen.Bounds.Height && !analyzedArea[x, y + 1])
                {
                    pixelQueue.Enqueue(new Point(x, y + 1));
                    analyzedArea[x, y + 1] = true;
                }
                if (pixelQueue.Any())
                {
                    var curPoint = pixelQueue.Dequeue();
                    x = curPoint.X;
                    y = curPoint.Y;
                }
            } while (pixelQueue.Count > 0);
            var width = downX - topX + 1;
            var height = downY - topY + 1;
            return CaptureScreen(topX, topY, width, height);
        }

        private double GetMin(double a, double b)
        {
            return a < b ? a : b;
        }

        private Bitmap CaptureScreen(double x, double y, double width, double height)
        {
            if (width <= 0 || height <= 0)
            {
                return null;
            }

            var screen = Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(this).Handle);
            var ix = screen.Bounds.X + Convert.ToInt32(x) - MagicShift;
            var iy = screen.Bounds.Y + Convert.ToInt32(y) - MagicShift;
            var iw = Convert.ToInt32(width);
            var ih = Convert.ToInt32(height);

            Bitmap image = new Bitmap(iw, ih, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var gr = Graphics.FromImage(image))
            {
                gr.CopyFromScreen(ix, iy, 0, 0, new Size(iw, ih));
            }

            return image;
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

        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Screener_MouseUp(e.LeftButton);
        }

        private void Screener_MouseUp(MouseButtonState mouseState)
        {
            if (mouseState == MouseButtonState.Released && _isMouseDown)
            {
                WindowState = WindowState.Minimized;
                if (_width >= MinImgSize || _height >= MinImgSize)
                {
                    var picture = CaptureScreen(GetMin(_startX, _curX), GetMin(_startY, _curY), _width, _height);
                    if (picture != null)
                    {
                        Picture = picture;
                    }
                }
                else
                {
                    Picture = PixelTraceCapture((int)_curX, (int)_curY);
                }

                _curX = _curY = _startX = _startY = 0;
                _isMouseDown = false;
                if (!DialogResult.HasValue)
                {
                    DialogResult = Picture != null;
                    Close();
                }
            }
        }
    }
}
