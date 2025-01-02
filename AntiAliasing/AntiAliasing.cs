namespace AntiAliasing
{
    internal class AntiAliasing
    {
        public Bitmap screen;

        public AntiAliasing(Bitmap screen)
        {
            this.screen = screen;
        }

        public void UpSampling()
        {
            screen = new Bitmap(screen.Width * 2, screen.Height * 2);
        }

        public void DownSampling()
        {
            var s = new Bitmap(screen.Width / 2, screen.Height / 2);
            for (int i = 0; i < s.Width; i++)
            {
                for (int j = 0; j < s.Height; j++)
                {
                    var c1 = screen.GetPixel(i * 2, j * 2);
                    var c2 = screen.GetPixel(i * 2 + 1, j * 2);
                    var c3 = screen.GetPixel(i * 2, j * 2 + 1);
                    var c4 = screen.GetPixel(i * 2 + 1, j * 2 + 1);
                    var a = (c1.A + c2.A + c3.A + c4.A) / 4;
                    var r = (c1.R + c2.R + c3.R + c4.R) / 4;
                    var g = (c1.G + c2.G + c3.G + c4.G) / 4;
                    var b = (c1.B + c2.B + c3.B + c4.B) / 4;
                    s.SetPixel(i, j, Color.FromArgb(a, r, g, b));
                }
            }
            screen = s;
        }
    }
}
