namespace ZBuffer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        readonly (float X, float Y, float Z)[] vertices = new[] { (-50f, -150f, 0f), (0f, 150f, 0f), (150f, -100f, 0f),
                                                                  (-50f, -120f, 150f), (0f, 120f, 150f), (150f, -100f, 50f)};// 三角形の頂点
        readonly int[,] faces = new[,] { { 0, 1, 2 }, { 3, 4, 5 } };                // 頂点番号の組み合わせ
        float thetaY = 0;

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            var transformedVertices = TransformVertices(vertices);                  // 頂点の平行移動と回転
            var bitmap = new Bitmap(e.ClipRectangle.Width, e.ClipRectangle.Height); // 画面サイズの画像データを作る
            var zs = new float[e.ClipRectangle.Width, e.ClipRectangle.Height];      // ピクセルの奥行きの値
            for (int i = 0; i < zs.GetLength(0); i++) for (int j = 0; j < zs.GetLength(1); j++) zs[i, j] = float.MaxValue; // 初期値
            for (int m = 0; m < faces.GetLength(0); ++m)
            {
                var vs = new (float X, float Y, float Z)[faces.GetLength(1)];
                for (int i = 0; i < vs.Length; ++i) vs[i] = transformedVertices[faces[m, i]];
                vs = vs.OrderBy(v => v.Y).ToArray();                                    // Y0≦Y1≦Y2となるように並び替え
                if (MathF.Abs(vs[0].Y - vs[2].Y) < 0.1f) return;                        // 三角形じゃない
                float k = GetLightDirectness(vs);                                       // 光が三角形に真っすぐに当たっている割合を取得
                for (int y = (int)vs[0].Y; y < vs[2].Y; y++)                            // 三角形を覆う全ての横線について行う
                {
                    int p = (MathF.Abs(vs[0].Y - vs[1].Y) < 0.1f || y >= vs[1].Y) ? 1 : 0;
                    var x1 = vs[p].X + (y - vs[p].Y) * (vs[p + 1].X - vs[p].X) / (vs[p + 1].Y - vs[p].Y);
                    var z1 = vs[p].Z + (y - vs[p].Y) * (vs[p + 1].Z - vs[p].Z) / (vs[p + 1].Y - vs[p].Y);
                    var x2 = vs[0].X + (y - vs[0].Y) * (vs[2].X - vs[0].X) / (vs[2].Y - vs[0].Y);
                    var z2 = vs[0].Z + (y - vs[0].Y) * (vs[2].Z - vs[0].Z) / (vs[2].Y - vs[0].Y);
                    var color = Color.SkyBlue;
                    color = Color.FromArgb(255, (int)(color.R * k), (int)(color.G * k), (int)(color.B * k));
                    for (int x = (int)Math.Min(x1, x2); x <= (int)Math.Max(x1, x2); ++x)
                    {
                        var z = x2 == x1 ? z1 : z1 + (x - x1) * (z2 - z1) / (x2 - x1);  // Z座標を計算
                        if (z > zs[x, y]) continue;                                     // 今回のものが奥にあれば何もしない
                        zs[x, y] = z;                                                   // 手前にあれば奥行の値を更新してピクセルを塗る
                        bitmap.SetPixel(x, y, color);
                    }
                }
            }
            e.Graphics.DrawImage(bitmap, 0, 0);                                     //画像データを画面に表示する
        }

        float Clip(float min, float max, float value)
        {
            return MathF.Min(MathF.Max(min, max), MathF.Max(MathF.Min(min, max), value));
        }

        (float X, float Y, float Z)[] TransformVertices((float X, float Y, float Z)[] vertices)
        {
            (float X, float Y, float Z) offset = new(250f, 250f, 0);                    // 平行移動の量
            var vs = new (float X, float Y, float Z)[vertices.Length];              // 移動後の頂点
            for (int i = 0; i < vs.Length; ++i)
            {
                (float X, float Y, float Z) v = new(vertices[i].X, vertices[i].Y, vertices[i].Z);
                v = new(v.Z * MathF.Sin(thetaY) + v.X * MathF.Cos(thetaY), v.Y, v.Z * MathF.Cos(thetaY) - v.X * MathF.Sin(thetaY));// Y軸回転
                v = new(v.X + offset.X, v.Y + offset.Y, v.Z + offset.Z);            // 平行移動
                vs[i] = new(v.X, v.Y, v.Z);
            }
            return vs;
        }

        float GetLightDirectness((float X, float Y, float Z)[] vs)
        {
            (float X, float Y, float Z) light = new(0, 0, 1);                           // 光の方向ベクトル
            (float X, float Y, float Z) a = new(vs[1].X - vs[0].X, vs[1].Y - vs[0].Y, vs[1].Z - vs[0].Z);
            (float X, float Y, float Z) b = new(vs[2].X - vs[0].X, vs[2].Y - vs[0].Y, vs[2].Z - vs[0].Z);
            float n = MathF.Sqrt(MathF.Pow(a.Y * b.Z - a.Z * b.Y, 2) + MathF.Pow(a.Z * b.X - a.X * b.Z, 2) + MathF.Pow(a.X * b.Y - a.Y * b.X, 2));
            (float X, float Y, float Z) normal = new((a.Y * b.Z - a.Z * b.Y) / n, (a.Z * b.X - a.X * b.Z) / n, (a.X * b.Y - a.Y * b.X) / n); // 法線ベクトル
            normal = normal.Z > 0 ? normal : new(-normal.X, -normal.Y, -normal.Z);              // 法線ベクトルを視点(Z方向)から見て正の方向になるようにする
            return Clip(0, 1, light.X * normal.X + light.Y * normal.Y + light.Z * normal.Z);    // 光が三角形に真っすぐに当たっている割合
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            thetaY += 0.05f; // 一定時間ごとにY軸回転角度を増やす
            Invalidate();
        }
    }
}