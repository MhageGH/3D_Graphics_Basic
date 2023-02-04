namespace Triangle
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        
        readonly (float X, float Y, float Z)[] vertices = new[] { (-50f,-150f, 0f), (0f, 150f, 0f), (150f, -100f, 0f) };// 三角形の頂点
        float thetaY = 0;
        (float X, float Y, float Z) offset = new(200f, 250f, 0);                    // 平行移動の量
        (float X, float Y, float Z) light = new(0, 0, 1);                           // 光の方向ベクトル

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            var vs = new (float X, float Y, float Z)[vertices.Length];              // 移動後の頂点
            for (int i = 0; i < vs.Length; ++i)
            {
                (float X, float Y, float Z) v = new(vertices[i].X, vertices[i].Y, vertices[i].Z);
                v = new(v.Z * MathF.Sin(thetaY) + v.X * MathF.Cos(thetaY), v.Y, v.Z * MathF.Cos(thetaY) - v.X * MathF.Sin(thetaY));// Y軸回転
                v = new(v.X + offset.X, v.Y + offset.Y, v.Z + offset.Z);            // 平行移動
                vs[i] = new(v.X, v.Y, v.Z);
            }
            vs = vs.OrderBy(v => v.Y).ToArray();                                    // Y0≦Y1≦Y2となるように並び替え
            if (vs[0] == vs[2]) return;                                             // 三角形じゃない
            (float X, float Y, float Z) a = new(vs[1].X - vs[0].X, vs[1].Y - vs[0].Y, vs[1].Z - vs[0].Z);
            (float X, float Y, float Z) b = new(vs[2].X - vs[0].X, vs[2].Y - vs[0].Y, vs[2].Z - vs[0].Z);
            float n = MathF.Sqrt(MathF.Pow(a.Y * b.Z - a.Z * b.Y, 2) + MathF.Pow(a.Z * b.X - a.X * b.Z, 2) + MathF.Pow(a.X * b.Y - a.Y * b.X, 2));
            (float X, float Y, float Z) normal = new((a.Y * b.Z - a.Z * b.Y) / n, (a.Z * b.X - a.X * b.Z) / n, (a.X * b.Y - a.Y * b.X) / n); // 法線ベクトル
            float k = MathF.Abs(light.X * normal.X + light.Y * normal.Y + light.Z * normal.Z);                     // 光が三角形に真っすぐに当たっている割合
            var bitmap = new Bitmap(e.ClipRectangle.Width, e.ClipRectangle.Height); // 画面サイズの画像データを作る
            for (var y = (int)vs[0].Y; y < vs[2].Y; y++)                            // 三角形を覆う全ての横線について行う
            {
                var p = (y >= vs[1].Y && (vs[1] != vs[2])) ? 1 : 0;
                var x1 = vs[p].X+(y- vs[p].Y) * (vs[p + 1].X - vs[p].X) / (vs[p + 1].Y - vs[p].Y);
                var x2 = vs[0].X + (y - vs[0].Y) * (vs[2].X - vs[0].X) / (vs[2].Y - vs[0].Y);
                var color = Color.SkyBlue;
                color = Color.FromArgb(255, (int)(color.R * k), (int)(color.G * k), (int)(color.B * k));
                for (int i = (int)Math.Min(x1, x2); i <= (int)Math.Max(x1, x2); ++i) bitmap.SetPixel(i, y, color);
            }
            e.Graphics.DrawImage(bitmap, 0, 0);                                     //画像データを画面に表示する
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            thetaY += 0.1f; // 一定時間ごとにY軸回転角度を増やす
            Invalidate();
        }
    }
}