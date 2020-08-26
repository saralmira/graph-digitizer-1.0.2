using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace GraphDigitizer.Screenshot
{
    public static class Util
    {
        public static void GetScreenResolution(out double width, out double height)
        {
            GetScreenResoCoeff(out double width_coeff, out double height_coeff);
            width = SystemParameters.PrimaryScreenWidth / width_coeff;
            height = SystemParameters.PrimaryScreenHeight / height_coeff;
        }

        public static void GetScreenResoCoeff(out double width_coeff, out double height_coeff)
        {
            width_coeff = height_coeff = 1.0;
            Window win = Application.Current.MainWindow;
            PresentationSource ps = PresentationSource.FromVisual(win);
            if (ps == null)
                return;
            Matrix matrix = ps.CompositionTarget.TransformFromDevice;
            //double widthRatio = matrix.M11;
            //double heightRatio = matrix.M22;
            width_coeff = matrix.M11;
            height_coeff = matrix.M22;
        }

        /*
        public static bool CopyFromScreen(Image img, Point location)
        {
            bool flag = true;
            IntPtr zero = IntPtr.Zero;
            try
            {
                zero = Win32Api.GetDC(IntPtr.Zero);
                using (Graphics graphics = Graphics.FromImage(img))
                {
                    IntPtr hDestDC = IntPtr.Zero;
                    try
                    {
                        try
                        {
                            hDestDC = graphics.GetHdc();
                            Win32Api.BitBlt(hDestDC, 0, 0, img.Width, img.Height, zero, location.X, location.Y, CopyPixelOperation.);//0x40cc0020
                        }
                        catch (Exception exception)
                        {
                            throw exception;
                        }
                        return flag;
                    }
                    finally
                    {
                        if (hDestDC != IntPtr.Zero)
                        {
                            graphics.ReleaseHdc(hDestDC);
                        }
                    }
                    return flag;
                }
            }
            catch (Exception exception2)
            {
                Console.WriteLine(exception2.Message);
                flag = false;
            }
            finally
            {
                if (zero != IntPtr.Zero)
                {
                    DeleteDC(zero);
                }
            }
            return flag;
        }
        */

        public static BitmapSource ScreenSnapshot()
        {
            GetScreenResolution(out double dwidth, out double dheight);
            int width = (int)dwidth;
            int height = (int)dheight;
            using (var tempBitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppRgb))
            using (var g = Graphics.FromImage(tempBitmap))
            {
                g.CopyFromScreen(0, 0, 0, 0, new Size(width, height));

                var data = tempBitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
                try
                {
                    return BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgr32, null, data.Scan0, data.Stride * height, data.Stride);
                }
                finally
                {
                    tempBitmap.UnlockBits(data);
                }
            }
        }

        public static BitmapSource CutImage(BitmapSource bitmap, Int32Rect rec)
        {
            //计算Stride
            var stride = bitmap.Format.BitsPerPixel * rec.Width / 8;
            //声明字节数组
            byte[] data = new byte[rec.Height * stride];
            //调用CopyPixels
            bitmap.CopyPixels(rec, data, stride, 0);
            return BitmapSource.Create(rec.Width, rec.Height, 0, 0, PixelFormats.Bgr32, null, data, stride);
        }

        public static Bitmap ImageSourceToBitmap(ImageSource imageSource)
        {
            return BitmapSourceToBitmap((BitmapSource)imageSource);
        }

        public static Bitmap BitmapSourceToBitmap(BitmapSource bitmapSource)
        {
            Bitmap bmp = new Bitmap(bitmapSource.PixelWidth, bitmapSource.PixelHeight, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            BitmapData data = bmp.LockBits(
            new Rectangle(Point.Empty, bmp.Size), ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            bitmapSource.CopyPixels(Int32Rect.Empty, data.Scan0, data.Height * data.Stride, data.Stride); bmp.UnlockBits(data);
            return bmp;
        }

        public static BitmapImage BitmapToBitmapImage(Bitmap bitmap)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                bitmap.Save(stream, ImageFormat.Bmp);

                stream.Position = 0;
                BitmapImage result = new BitmapImage();
                result.BeginInit();
                // According to MSDN, "The default OnDemand cache option retains access to the stream until the image is needed."
                // Force the bitmap to load right now so we can dispose the stream.
                result.CacheOption = BitmapCacheOption.OnLoad;
                result.StreamSource = stream;
                result.EndInit();
                result.Freeze();

                return result;
            }
        }

    }
}
