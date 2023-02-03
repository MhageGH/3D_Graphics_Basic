namespace Triangle
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        Point[] vertices = new Point[] { new(100, 100), new(150, 400), new(300, 150) };

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            var vs = vertices.OrderBy(v => v.Y).ToArray();
            if (vs[0] == vs[2]) return;
            var bitmap = new Bitmap(e.ClipRectangle.Width, e.ClipRectangle.Height);
            for (var y = vs[0].Y; y < vs[2].Y; y++)
            {
                var p = (y >= vs[1].Y && (vs[1] != vs[2])) ? 1 : 0;
                var x1 = vs[p].X+(y- vs[p].Y) * (vs[p + 1].X - vs[p].X) / (vs[p + 1].Y - vs[p].Y);
                var x2 = vs[0].X + (y - vs[0].Y) * (vs[2].X - vs[0].X) / (vs[2].Y - vs[0].Y);
                for (int i = Math.Min(x1, x2); i < Math.Max(x1, x2); ++i) bitmap.SetPixel(i, y, Color.SkyBlue);
            }
            e.Graphics.DrawImage(bitmap, 0, 0);
        }
    }
}