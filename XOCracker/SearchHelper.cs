using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace XOCracker
{
    public class ImageContainer
    {
        private readonly int[,] _sums;
        private readonly int _bmph;
        private readonly int _bmpw;
        private readonly byte[,] _rgbValues;

        public ImageContainer(Bitmap img)
        {
            _bmph = img.Height;
            _bmpw = img.Width;
            _rgbValues = SearchHelper.ByteGray(img);
            lock (this)
            {
                _sums = getImSumm(_rgbValues, _bmph, _bmpw);
            }
        }

        int[,] getImSumm(byte[,] rgbValues, int n, int m)      // метод построения матрицы сумм заданного изображения
        {
            // пересчет матрицы сумм
            int[,] rgbs = new int[n, m];
            rgbs[0, 0] = rgbValues[0, 0];
            for (int i = 1; i < n; i++)                               // для первого столбца
                rgbs[i, 0] = rgbs[i - 1, 0] + rgbValues[i, 0];
            for (int j = 1; j < m; j++)                               // для первой строки
                rgbs[0, j] = rgbs[0, j - 1] + rgbValues[0, j];
            for (int i = 1; i < n; i++)                               // для всего остального поля 
                for (int j = 1; j < m; j++)
                    rgbs[i, j] = rgbValues[i, j] + rgbs[i - 1, j] + rgbs[i, j - 1] - rgbs[i - 1, j - 1];
            return rgbs;
        }

        public int Height => _bmph;

        public int Width => _bmpw;

        /// <summary>
        /// Поиск изображения.
        /// </summary>
        /// <param name="fbmp">Искомое изображение.</param>
        /// <param name="deep">Точность анализа.</param>
        /// <param name="fault">Величина погрешности.</param>
        /// <returns></returns>
        public Point Find(ImageContainer fbmp, int deep, int fault)
        {
            Point pos = new Point();
            int fbmph = fbmp.Height,
                fbmpw = fbmp.Width;
            int mindef = fault + 1;

            // поиск области с нужной суммой
            for (int y = fbmph + 1; y < _bmph; y++)
                for (var x = fbmpw + 1; x < _bmpw; x++)
                {
                    var bdef = Difference(ref fbmp, x - fbmpw, y - fbmph, x, y, new Size(fbmpw, fbmph), deep);
                    if (bdef < mindef)
                    {
                        pos.X = x;
                        pos.Y = y;
                        mindef = bdef;
                    }
                }
            return pos;
        }

        /// <summary>
        /// Сравнение на идентичность эталонного изображения и участка из скриншота.
        /// </summary>
        /// <param name="frgbS">Искомое изображение.</param>
        /// <param name="x0">Координата X верхнего левого угла сравниваемой области.</param>
        /// <param name="y0">Координата Y верхнего левого угла сравниваемой области.</param>
        /// <param name="x1">Координата X нижнего правого угла сравниваемой области.</param>
        /// <param name="y1">Координата Y нижнего правого угла сравниваемой области.</param>
        /// <param name="size">Размер искомого изображения.</param>
        /// <param name="deep">Глубина сравнения (точность)</param>
        /// <returns></returns>
        public int Difference(ref ImageContainer frgbS, int x0, int y0, int x1, int y1, Size size, int deep)
        {
            bool hor;
            if (x1 >= Width || y1 >= Height)
            {
                return int.MaxValue;
            }
            if (deep == 0)  // если достигнута максимальная глубина рекурсии - начать подъем
                return Math.Abs(RSumm(x1 - size.Width + 1, y1 - size.Height + 1, x1, y1) 
                        - frgbS.RSumm(x1 - x0 - size.Width, y1 - y0 - size.Height, x1 - x0 - 1, y1 - y0 - 1));
            if (size.Height > size.Width)
            {
                hor = true;
                size.Height /= 2;
            }
            else
            {
                hor = false;
                size.Width /= 2;
            }
            int res = Difference(ref frgbS, x0, y0, x1, y1, size, deep - 1);
            if (hor)                                       // получениe координат второй половины
                y1 -= size.Height;
            else
                x1 -= size.Width;
            res += Difference(ref frgbS, x0, y0, x1, y1, size, deep - 1);
            return res;
        }

        public int RSumm(int j0, int i0, int j1, int i1)  // нахождение суммы прямоугольника на матрице сумм
        {
            lock (this)
            {
                if (i0 > 0 && j0 > 0)
                {
                    int it = 0;

                    Dispatcher.CurrentDispatcher.Invoke(() =>
                    {
                        it = _sums[i1, j1] + _sums[i0 - 1, j0 - 1] - _sums[i0 - 1, j1] - _sums[i1, j0 - 1];
                    });

                    return it;
                }
                if (i0 > 0) return _sums[i1, j1] - _sums[i0 - 1, j1];
                if (j0 > 0) return _sums[i1, j1] - _sums[i1, j0 - 1];
                return _sums[i1, j1];
            }
        }

        public int? GetPixel(int x, int y)
        {
            if (x < 0 || y < 0 || x >= _bmpw || y >= _bmph)
            {
                return null;
            }

            return _rgbValues[y, x];
        }
    }

    public static class SearchHelper
    {
        public const int MagicShift = 7;

        public static int ColorsCmp(Color c1, Color c2)  // метод определения разности между двумя цветами (для построения игрового поля)
        {
            return Math.Abs(c1.B - c2.B);
        }

        public static Bitmap CaptureScreen(double x, double y, double width, double height, IntPtr winHndl)
        {
            if (width <= 0 || height <= 0)
            {
                return null;
            }

            var screen = Screen.FromHandle(winHndl);
            var ix = screen.Bounds.X + Convert.ToInt32(x) - MagicShift;
            var iy = screen.Bounds.Y + Convert.ToInt32(y) - MagicShift;
            var iw = Convert.ToInt32(width);
            var ih = Convert.ToInt32(height);

            Bitmap image = new Bitmap(iw, ih, PixelFormat.Format32bppArgb);
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
                bitmap.Save(memory, ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();
                return bitmapimage;
            }
        }

        public static Bitmap ToGray(Bitmap img)      // метод построения матрицы сумм заданного изображения
        {
            int imgh = img.Height,
                imgw = img.Width;
            // получение изображения в градациях серого
            BitmapData bmpd = img.LockBits(new Rectangle(0, 0, imgw, imgh), ImageLockMode.ReadWrite, PixelFormat.Format32bppRgb);
            int bytes = bmpd.Stride * imgh;
            byte[] rgbValues = new byte[bytes];
            Marshal.Copy(bmpd.Scan0, rgbValues, 0, bytes);
            for (int i = 0; i < rgbValues.Length; i += 4)
            {
                var icolor = (byte)((rgbValues[i] + rgbValues[i + 1] + rgbValues[i + 2]) / 3);
                rgbValues[i] = icolor;
                rgbValues[i + 1] = icolor;
                rgbValues[i + 2] = icolor;
            }
            Marshal.Copy(rgbValues, 0, bmpd.Scan0, rgbValues.Length);
            img.UnlockBits(bmpd);
            return img;
        }

        public static byte[,] ByteGray(Bitmap img)
        {
            byte[,] mbGray = new byte[img.Height, img.Width];
            int imgh = img.Height,
                imgw = img.Width;
            BitmapData bmpd = img.LockBits(new Rectangle(0, 0, imgw, imgh), ImageLockMode.ReadWrite, PixelFormat.Format32bppRgb);
            int bytes = bmpd.Stride * imgh;
            byte[] rgbValues = new byte[bytes];
            Marshal.Copy(bmpd.Scan0, rgbValues, 0, bytes);
            for (int i = 0; i < imgh; i++)
                for (int j = 0; j < imgw; j++)
                    mbGray[i, j] = rgbValues[j * 4 + i * bmpd.Stride];
            img.UnlockBits(bmpd);

            return mbGray;
        }
    }
}
