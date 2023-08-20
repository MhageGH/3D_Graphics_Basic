using System.Numerics;

namespace Texture
{
    public partial class Form1 : Form
    {
        float thetaY = MathF.PI / 10;                                                // Y軸回転角度
        float thetaZ = 0;                                                          // Z軸回転角度
        float scale = 50f;                                                          // 拡大係数
        const float light_thetaX = 1 * MathF.PI / 3;
        Vector3 light = new(0, MathF.Cos(light_thetaX), MathF.Sin(light_thetaX));   // 光の方向ベクトル
        Vector3 offset = new(300f, 450f, 0);                                        // 平行移動の量
        //Model model = new("../../../Model/1.csv");                                  // 1.csvは球形だが同じ位置で法線ベクトルが異なる別の頂点が存在し、法線が不連続に切り替わる(例:頂点1と頂点396)
        Model model = new("../../../Model/Shanghai.csv");

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
            DrawPolygons(transformedVertices, screen, zBuffers, true, true);            // テクスチャマッピングと、グーローシェーディングは引数でON/OFF切り替え可能
            e.Graphics.DrawImage(screen, 0, 0);                                         // 画像データを画面に表示する
        }

        private void DrawPolygons(Vertex[] vertices, Bitmap screen, float[,] zBuffer, bool textureMapping, bool gourauShading)
        {
            for (int m = 0; m < model.faces.Length; ++m)
            {
                var color = model.faces[m].material.color;
                var texture = model.faces[m].material.texture;
                var length = model.faces[m].vertexNumbers.Length;                          // ポリゴン一つごとの頂点の数。MMDの場合は常に3。
                var vs = new Vertex[length];
                for (int i = 0; i < length; ++i)
                {
                    var v = vertices[model.faces[m].vertexNumbers[i]];
                    vs[i] = new Vertex(v.pos, v.pseudoNormal, v.uv);
                }
                vs = vs.OrderBy(v => v.pos.Y).ToArray();
                if (MathF.Abs(vs[0].pos.Y - vs[2].pos.Y) < 0.1f) continue;                  // 三角形じゃない
                var normal = MakePositiveNormal(GetNormalOfTrigngle(vs.Select(v => v.pos).ToArray()));
                float brightness = Clip(0, 1, light.X * normal.X + light.Y * normal.Y + light.Z * normal.Z);
                for (int y = (int)vs[0].pos.Y; y < vs[2].pos.Y; ++y)                        // 三角形を覆う全ての横線について行う
                {
                    int j = (MathF.Abs(vs[0].pos.Y - vs[1].pos.Y) < 0.1f || y >= vs[1].pos.Y) ? 1 : 0;
                    var x1 = Clip(vs[j].pos.X, vs[j + 1].pos.X, vs[j].pos.X + (y - vs[j].pos.Y) * (vs[j + 1].pos.X - vs[j].pos.X) / (vs[j + 1].pos.Y - vs[j].pos.Y));  // クリッピングは計算誤差対策
                    var x2 = Clip(vs[0].pos.X, vs[2].pos.X, vs[0].pos.X + (y - vs[0].pos.Y) * (vs[2].pos.X - vs[0].pos.X) / (vs[2].pos.Y - vs[0].pos.Y));
                    var z1 = vs[j].pos.Z + (y - vs[j].pos.Y) * (vs[j + 1].pos.Z - vs[j].pos.Z) / (vs[j + 1].pos.Y - vs[j].pos.Y);
                    var z2 = vs[0].pos.Z + (y - vs[0].pos.Y) * (vs[2].pos.Z - vs[0].pos.Z) / (vs[2].pos.Y - vs[0].pos.Y);
                    var pn1 = vs[j].pseudoNormal + (y - vs[j].pos.Y) * (vs[j + 1].pseudoNormal - vs[j].pseudoNormal) / (vs[j + 1].pos.Y - vs[j].pos.Y);
                    var pn2 = vs[0].pseudoNormal + (y - vs[0].pos.Y) * (vs[2].pseudoNormal - vs[0].pseudoNormal) / (vs[2].pos.Y - vs[0].pos.Y);
                    var uv1 = vs[j].uv + (y - vs[j].pos.Y) * (vs[j + 1].uv - vs[j].uv) / (vs[j + 1].pos.Y - vs[j].pos.Y);
                    var uv2 = vs[0].uv + (y - vs[0].pos.Y) * (vs[2].uv - vs[0].uv) / (vs[2].pos.Y - vs[0].pos.Y);
                    for (int x = (int)Math.Min(x1, x2); x <= (int)Math.Max(x1, x2); ++x)
                    {
                        var z = x2 == x1 ? z1 : z1 + (x - x1) * (z2 - z1) / (x2 - x1);      // Z座標を計算
                        if (z > zBuffer[x, y]) continue;                                    // 今回のものが奥にあれば何もしない
                        if (textureMapping)
                        {
                            var uv = x2 == x1 ? uv1 : uv1 + (x - x1) * (uv2 - uv1) / (x2 - x1);
                            uv.X = Clip(0, 1, uv.X);                                        // クリッピングは計算誤差対策
                            uv.Y = Clip(0, 1, uv.Y);
                            if (texture != null) color = texture.GetPixel((int)((texture.Width - 1) * uv.X), (int)((texture.Height - 1) * uv.Y));
                        }
                        var brightenedColor = Color.FromArgb(color.A, (int)(color.R * brightness), (int)(color.G * brightness), (int)(color.B * brightness));
                        if (gourauShading)
                        {
                            var n = x2 == x1 ? pn1 : pn1 + (x - x1) * (pn2 - pn1) / (x2 - x1);
                            n = MakePositiveNormal(n);
                            brightness = Clip(0, 1, light.X * n.X + light.Y * n.Y + light.Z * n.Z);
                            brightenedColor = Color.FromArgb(color.A, (int)(color.R * brightness), (int)(color.G * brightness), (int)(color.B * brightness));
                        }
                        if (brightenedColor.A == 0) continue;                                 // 透明な色は塗らず、Zバッファを更新しない
                        screen.SetPixel(x, y, brightenedColor);
                        zBuffer[x, y] = z;                                                    // 奥行の値を更新
                    }
                }
            }
        }

        float Clip(float min, float max, float value)
        {
            return MathF.Min(MathF.Max(min, max), MathF.Max(MathF.Min(min, max), value));
        }

        /// <summary>
        /// 法線を視点(Z方向)から見て正の方向になるようにする
        /// </summary>
        Vector3 MakePositiveNormal(Vector3 normal)
        {
            return normal.Z > 0 ? normal : -normal;
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
                vs[i] = new Vertex(pos, pn, vertices[i].uv);
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
            //thetaY += 0.05f; // 一定時間ごとにY軸回転角度を増やす
            Invalidate();
        }
    }
}