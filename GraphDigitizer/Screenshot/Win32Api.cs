using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GraphDigitizer.Screenshot
{
    public static class Win32Api
    {
        /// <summary>
        /// 对指定的源设备环境区域中的像素进行位块（bit_block）转换，以传送到目标设备环境
        /// </summary>
        /// <param name="hdcDest">指向目标设备环境的句柄</param>
        /// <param name="nx">指定目标矩形区域左上角的X轴逻辑坐标</param>
        /// <param name="ny">指定目标矩形区域左上角的Y轴逻辑坐标</param>
        /// <param name="nWidth">指定源在目标矩形区域的逻辑宽度</param>
        /// <param name="nHeight">指定源在目标矩形区域的逻辑高度</param>
        /// <param name="hdcSource">指向源设备环境的句柄</param>
        /// <param name="xSrc">指定源矩形区域左上角的X轴逻辑坐标</param>
        /// <param name="ySrc">指定源矩形区域左上角的Y轴逻辑坐标</param>
        /// <param name="rop">指定光栅操作代码。这些代码将定义源矩形区域的颜色数据，如何与目标矩形区域的颜色数据组合以完成最后的颜色</param>
        /// <returns>如果函数成功，那么返回true；如果函数失败，则返回false</returns>
        [DllImport("gdi32.dll")]
        //public static extern bool BitBlt(IntPtr hDestDC, int x, int y, int nWidth, int nHeight, IntPtr hSrcDC, int xSrc, int ySrc, int dwRop);
        public static extern bool BitBlt(IntPtr hdcDest, int nx, int ny, int nWidth, int nHeight, IntPtr hdcSource, int xSrc, int ySrc, System.Drawing.CopyPixelOperation rop);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateDC(string Display, string c, object b, object a);
        [DllImport("gdi32.dll")]
        public static extern bool DeleteDC(IntPtr handle);

        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hwnd);

    }
}
