using System.Numerics;

namespace Texture
{
    public partial class Form1 : Form
    {
        float thetaY = 0f;                                          // Y軸回転角度
        float thetaZ = 0f;                                          // Z軸回転角度
        float scale = 500f;                                         // 拡大係数
        Vector3 offset = new(300f, 450f, 0);                        // 平行移動の量
        Vector3 light = new(0, 1 / MathF.Sqrt(2), 1 / MathF.Sqrt(2)); // 光の方向ベクトル
        Model model = new("../../../1.csv");
        Image texture = Bitmap.FromFile("../../../1.bmp");

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            var screen = new Bitmap(e.ClipRectangle.Width, e.ClipRectangle.Height);     // 画面サイズの画像データを作る
            var zBuffers = new float[e.ClipRectangle.Width, e.ClipRectangle.Height];    // ピクセルの奥行きの値
            for (int i = 0; i < zBuffers.GetLength(0); i++) for (int j = 0; j < zBuffers.GetLength(1); j++) zBuffers[i, j] = float.MaxValue; // 初期値
            var transformedVertices = TransformVertices(model.vertices, offset);        // 頂点の平行移動と回転
            DrawPolygons(transformedVertices, screen, zBuffers, true);
            e.Graphics.DrawImage(screen, 0, 0);                                         //画像データを画面に表示する
        }

        private void DrawPolygons(Vertex[] vertices, Bitmap screen, float[,] zBuffer, bool gourauShading)
        {
            for (int m = 0; m < model.faces.Length; ++m)
            {
                var length = model.faces[m].vertexNumbers.Length;                          // ポリゴン一つごとの頂点の数。MMDの場合は常に3。
                var vs = new Vertex[length];
                for (int i = 0; i < length; ++i)
                {
                    var v = vertices[model.faces[m].vertexNumbers[i]];
                    vs[i] = new Vertex(v.pos, v.pseudoNormal, v.uv);
                }
                vs = vs.OrderBy(v => v.pos.Y).ToArray();
                if (MathF.Abs(vs[0].pos.Y - vs[2].pos.Y) < 0.1f) continue;                  // 三角形じゃない
                var normal = GetNormalOfTrigngle(vs.Select(v => v.pos).ToArray());
                float brightness = MathF.Abs(light.X * normal.X + light.Y * normal.Y + light.Z * normal.Z);
                for (int y = (int)vs[0].pos.Y; y < vs[2].pos.Y; ++y)                        // 三角形を覆う全ての横線について行う
                {
                    int p = (MathF.Abs(vs[0].pos.Y - vs[1].pos.Y) < 0.1f || y >= vs[1].pos.Y) ? 1 : 0;
                    var x1 = vs[p].pos.X + (y - vs[p].pos.Y) * (vs[p + 1].pos.X - vs[p].pos.X) / (vs[p + 1].pos.Y - vs[p].pos.Y);
                    var z1 = vs[p].pos.Z + (y - vs[p].pos.Y) * (vs[p + 1].pos.Z - vs[p].pos.Z) / (vs[p + 1].pos.Y - vs[p].pos.Y);
                    var x2 = vs[0].pos.X + (y - vs[0].pos.Y) * (vs[2].pos.X - vs[0].pos.X) / (vs[2].pos.Y - vs[0].pos.Y);
                    var z2 = vs[0].pos.Z + (y - vs[0].pos.Y) * (vs[2].pos.Z - vs[0].pos.Z) / (vs[2].pos.Y - vs[0].pos.Y);
                    x1 = MathF.Min(MathF.Max(vs[p].pos.X, vs[p + 1].pos.X), MathF.Max(MathF.Min(vs[p].pos.X, vs[p + 1].pos.X), x1)); // 計算誤差対策
                    x2 = MathF.Min(MathF.Max(vs[0].pos.X, vs[2].pos.X), MathF.Max(MathF.Min(vs[0].pos.X, vs[2].pos.X), x2));
                    Vector3 n1 = new(), n2 = new();
                    n1 = vs[p].pseudoNormal + (y - vs[p].pos.Y) * (vs[p + 1].pseudoNormal - vs[p].pseudoNormal) / (vs[p + 1].pos.Y - vs[p].pos.Y);
                    n2 = vs[0].pseudoNormal + (y - vs[0].pos.Y) * (vs[2].pseudoNormal - vs[0].pseudoNormal) / (vs[2].pos.Y - vs[0].pos.Y);
                    var base_color = Color.SkyBlue;                                         // TODO テクスチャ画像に置き換える
                    var color = Color.FromArgb(255, (int)(base_color.R * brightness), (int)(base_color.G * brightness), (int)(base_color.B * brightness));
                    for (int x = (int)Math.Min(x1, x2); x <= (int)Math.Max(x1, x2); ++x)
                    {
                        var z = x2 == x1 ? z1 : z1 + (x - x1) * (z2 - z1) / (x2 - x1);      // Z座標を計算
                        if (z > zBuffer[x, y]) continue;                                    // 今回のものが奥にあれば何もしない
                        zBuffer[x, y] = z;                                                  // 手前にあれば奥行の値を更新してピクセルを塗る
                        if (gourauShading)
                        {
                            var n = x2 == x1 ? n1 : n1 + (x - x1) * (n2 - n1) / (x2 - x1);
                            brightness = MathF.Min(1, MathF.Abs(light.X * n.X + light.Y * n.Y + light.Z * n.Z));
                            color = Color.FromArgb(255, (int)(base_color.R * brightness), (int)(base_color.G * brightness), (int)(base_color.B * brightness));
                        }
                        screen.SetPixel(x, y, color);
                    }
                }
            }
        }

        Vertex[] TransformVertices(Vertex[] vertices, Vector3 offset)
        {
            var vs = new Vertex[vertices.Length];  // 移動後の頂点
            for (int i = 0; i < vs.Length; ++i)
            {
                Vector3 pos = new(vertices[i].pos.X, -vertices[i].pos.Y, vertices[i].pos.Z);   // MMDモデルはY軸が反転している
                pos = new(pos.Z * MathF.Sin(thetaY) + pos.X * MathF.Cos(thetaY), pos.Y, pos.Z * MathF.Cos(thetaY) - pos.X * MathF.Sin(thetaY));// Y軸回転
                pos = new(pos.X * MathF.Cos(thetaZ) - pos.Y * MathF.Sin(thetaZ), pos.X * MathF.Sin(thetaZ) + pos.Y * MathF.Cos(thetaZ), pos.Z);// Z軸回転
                pos = new(scale * pos.X, scale * pos.Y, scale * pos.Z);                    // 拡大
                pos = new(pos.X + offset.X, pos.Y + offset.Y, pos.Z + offset.Z);           // 平行移動
                Vector3 pn = new(vertices[i].pseudoNormal.X, -vertices[i].pseudoNormal.Y, vertices[i].pseudoNormal.Z);     // MMDモデルはY軸が反転している
                pn = new(pn.Z * MathF.Sin(thetaY) + pn.X * MathF.Cos(thetaY), pn.Y, pn.Z * MathF.Cos(thetaY) - pn.X * MathF.Sin(thetaY));// Y軸回転
                pn = new(pn.X * MathF.Cos(thetaZ) - pn.Y * MathF.Sin(thetaZ), pn.X * MathF.Sin(thetaZ) + pn.Y * MathF.Cos(thetaZ), pn.Z);// Z軸回転
                vs[i] = new Vertex(pos, pn, null);
            }
            return vs;
        }

        Vector3 GetNormalOfTrigngle(Vector3[] vertices)
        {
            Vector3 a = new(vertices[1].X - vertices[0].X, vertices[1].Y - vertices[0].Y, vertices[1].Z - vertices[0].Z);
            Vector3 b = new(vertices[2].X - vertices[0].X, vertices[2].Y - vertices[0].Y, vertices[2].Z - vertices[0].Z);
            float n = MathF.Sqrt(MathF.Pow(a.Y * b.Z - a.Z * b.Y, 2) + MathF.Pow(a.Z * b.X - a.X * b.Z, 2) + MathF.Pow(a.X * b.Y - a.Y * b.X, 2));
            return new((a.Y * b.Z - a.Z * b.Y) / n, (a.Z * b.X - a.X * b.Z) / n, (a.X * b.Y - a.Y * b.X) / n); // 法線ベクトル
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            thetaY += 0.05f; // 一定時間ごとにY軸回転角度を増やす
            Invalidate();
        }
    }
}