using System.Drawing.Imaging;

namespace AntiAliasing
{
    internal class AntiAliasing
    {
        public Bitmap screen2;

        public AntiAliasing(Bitmap screen)
        {
            this.screen2 = new Bitmap(screen.Width * 2, screen.Height * 2);
        }

        public Bitmap GetDownSamplingImage()
        {
            var screenBmpData2 = screen2.LockBits(new Rectangle(0, 0, screen2.Width, screen2.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            var stride2 = screenBmpData2.Stride;
            var screenImgData2 = new byte[stride2 * screen2.Height];
            System.Runtime.InteropServices.Marshal.Copy(screenBmpData2.Scan0, screenImgData2, 0, screenImgData2.Length);
            screen2.UnlockBits(screenBmpData2);

            var stride = stride2 / 2;
            var screenImgData = new byte[stride * screen2.Height / 2];
            for (int i = 0; i < screen2.Height / 2; ++i)
            {
                for (int j = 0; j < screen2.Width / 2; ++j)
                {
                    for (int k = 0; k < 4; ++k)
                    {
                        UInt16 c = screenImgData2[stride2 * i * 2 + 4 * j * 2 + k];
                        c += screenImgData2[stride2 * i * 2 + 4 * j * 2 + 4 + k];
                        c += screenImgData2[stride2 * (i * 2 + 1) + 4 * j * 2 + k];
                        c += screenImgData2[stride2 * (i * 2 + 1) + 4 * j * 2 + 4 + k];
                        screenImgData[stride * i + 4 * j + k] = (byte)(c / 4);
                    }
                }
            }

            var s = new Bitmap(screen2.Width / 2, screen2.Height / 2);
            var screenBmpData = s.LockBits(new Rectangle(0, 0, s.Width, s.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            System.Runtime.InteropServices.Marshal.Copy(screenImgData, 0, screenBmpData.Scan0, screenImgData.Length);
            s.UnlockBits(screenBmpData);
            return s;
        }
    }
}
