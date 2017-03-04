using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace XOCracker
{
    public class Mouse
    {
        // подключение метода клика мышью
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern bool GetCursorPos(ref Point pos);

        [Flags]
        public enum Buttons : uint
        {
            Left = 0x00000002 | 0x00000004,
            Right = 0x00000008 | 0x00000010
        }

        public static void ClickIt(Point pos, int speed, Buttons button)   // перемещение курсора по координатам и клик в точке назначения
        {
            Point p = new Point();
            int x = pos.X, y = pos.Y;
            speed+=2;
            do
            {
                GetCursorPos(ref p);
                double kx = x - p.X;
                double ky = y - p.Y;
                var lon = Math.Sqrt(kx * kx + ky * ky);
                if (lon == 0) break;
                p.Y += (int)(speed * ky / lon);
                p.X += (int)(speed * kx / lon);
                Cursor.Position = p;
                Thread.Sleep(10);
            }
            while (Math.Abs(p.X - x) + Math.Abs(p.Y - y) > speed);
            Cursor.Position = new Point(x, y);
            mouse_event((uint)(button), (uint)Cursor.Position.X, (uint)Cursor.Position.Y, 0, UIntPtr.Zero);
        }

    }
}
