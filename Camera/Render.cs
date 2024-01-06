using System.Diagnostics;
using System.Drawing.Imaging;
using System.Numerics;

namespace Camera
{
    internal class Render
    {
        Bitmap screen;
        float[,] zBuffers;
        Vector3 lightVector;
        bool useTextureMapping; 
        bool useGourauShading;

        public Render(Bitmap screen, Vector3 lightVector, bool useTextureMapping, bool useGourauShading)
        {
            this.screen = screen;
            this.lightVector = Vector3.Normalize(lightVector);
            this.useTextureMapping = useTextureMapping;
            this.useGourauShading = useGourauShading;
            zBuffers = new float[screen.Width, screen.Height];
            for (int i = 0; i < zBuffers.GetLength(0); i++) for (int j = 0; j < zBuffers.GetLength(1); j++) zBuffers[i, j] = float.MaxValue; // 初期値
        }

        public void DrawModel(Model model, Matrix matrix, Matrix matrixR)
        {
            var sw = new Stopwatch();
            sw.Start();
            var transformedVertices = TransformVertices(model.vertices, matrix, matrixR);
            Debug.WriteLine($"\tTarnsformVertices time : {sw.Elapsed}");
            sw.Restart();
            DrawPolygons(model, transformedVertices);
            Debug.WriteLine($"\tDrawPolygons time : {sw.Elapsed}");
            sw.Stop();
        }

        /// <summary>
        /// MatrixRは法線ベクトル用の線形変換のみの合成行列：線形変換と平行移動が複数組み合わさっていても法線ベクトルには線形変換だけが残る
        ///       ⇒　R'(R(v + d + n) + d') – R'(R(v + d) + d') = R'R(v + d + n) + R'd' – R'R(v + d) – R'd' = R'Rn
        /// </summary>
        private Vertex[] TransformVertices(Vertex[] vertices, Matrix matrix, Matrix matrixR)
        {
            var vertices_out = new Vertex[vertices.Length];
            for (int i = 0; i < vertices_out.Length; ++i)
            {
                var pos = matrix * vertices[i].pos;
                var pn = Vector3.Normalize(matrixR * vertices[i].pseudoNormal);
                vertices_out[i] = new Vertex(pos, pn, vertices[i].uv);
            }
            return vertices_out;
        }

        private void DrawPolygons(Model model, Vertex[] vertices)
        {
            int width = screen.Width, height = screen.Height;
            var screenBmpData = screen.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            try
            {
                var stride = screenBmpData.Stride;
                var screenImgData = new byte[stride * height];
                DrawPolygonsWithByteArray(model, vertices, width, height, stride, screenImgData);
                System.Runtime.InteropServices.Marshal.Copy(screenImgData, 0, screenBmpData.Scan0, screenImgData.Length);
            }
            finally
            {
                screen.UnlockBits(screenBmpData);
            }
        }

        private void DrawPolygonsWithByteArray(Model model, Vertex[] vertices, int width, int height, int stride, Byte[] screenImgData)
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
                if (Math.Max(Math.Max(vs[0].pos.X, vs[1].pos.X), vs[2].pos.X) - Math.Min(Math.Min(vs[0].pos.X, vs[1].pos.X), vs[2].pos.X) < 0.1f) continue; // 三角形じゃない
                var normal = MakePositiveNormal(GetNormalOfTrigngle(vs.Select(v => v.pos).ToArray()));
                float brightness = ClipValue(0, 1, lightVector.X * normal.X + lightVector.Y * normal.Y + lightVector.Z * normal.Z);
                for (int y = (int)vs[0].pos.Y; y < vs[2].pos.Y; ++y)                        // 三角形を覆う全ての横線について行う
                {
                    if (y < 0 || y >= height) continue;                              // 画面サイズでクリッピング
                    int j = (MathF.Abs(vs[0].pos.Y - vs[1].pos.Y) < 0.1f || y >= vs[1].pos.Y) ? 1 : 0;
                    var x1 = ClipValue(vs[j].pos.X, vs[j + 1].pos.X, vs[j].pos.X + (y - vs[j].pos.Y) * (vs[j + 1].pos.X - vs[j].pos.X) / (vs[j + 1].pos.Y - vs[j].pos.Y));  // 計算誤差対策
                    var x2 = ClipValue(vs[0].pos.X, vs[2].pos.X, vs[0].pos.X + (y - vs[0].pos.Y) * (vs[2].pos.X - vs[0].pos.X) / (vs[2].pos.Y - vs[0].pos.Y));
                    var z1 = vs[j].pos.Z + (y - vs[j].pos.Y) * (vs[j + 1].pos.Z - vs[j].pos.Z) / (vs[j + 1].pos.Y - vs[j].pos.Y);
                    var z2 = vs[0].pos.Z + (y - vs[0].pos.Y) * (vs[2].pos.Z - vs[0].pos.Z) / (vs[2].pos.Y - vs[0].pos.Y);
                    var pn1 = vs[j].pseudoNormal + (y - vs[j].pos.Y) * (vs[j + 1].pseudoNormal - vs[j].pseudoNormal) / (vs[j + 1].pos.Y - vs[j].pos.Y);
                    var pn2 = vs[0].pseudoNormal + (y - vs[0].pos.Y) * (vs[2].pseudoNormal - vs[0].pseudoNormal) / (vs[2].pos.Y - vs[0].pos.Y);
                    var uv1 = vs[j].uv + (y - vs[j].pos.Y) * (vs[j + 1].uv - vs[j].uv) / (vs[j + 1].pos.Y - vs[j].pos.Y);
                    var uv2 = vs[0].uv + (y - vs[0].pos.Y) * (vs[2].uv - vs[0].uv) / (vs[2].pos.Y - vs[0].pos.Y);
                    for (int x = (int)Math.Min(x1, x2); x <= (int)Math.Max(x1, x2); ++x)
                    {
                        if (x < 0 || x >= width) continue;                                  // 画面サイズでクリッピング
                        var z = x2 == x1 ? z1 : z1 + (x - x1) * (z2 - z1) / (x2 - x1);      // Z座標を計算
                        if (z > zBuffers[x, y]) continue;                                    // 今回のものが奥にあれば何もしない
                        if (useTextureMapping)
                        {
                            var uv = x2 == x1 ? uv1 : uv1 + (x - x1) * (uv2 - uv1) / (x2 - x1);
                            uv.X = ClipValue(0, 1, uv.X);                                        // 計算誤差対策
                            uv.Y = ClipValue(0, 1, uv.Y);
                            if (texture != null)
                            {
                                int X = (int)((texture.width - 1) * uv.X);
                                int Y = (int)((texture.height - 1) * uv.Y);
                                int argb = 0;
                                for (int i = 0; i < 4; ++i) argb |= (int)(texture.bytes[texture.stride * Y + 4 * X + i]) << (i * 8);
                                color = Color.FromArgb(argb);
                            }
                        }
                        var brightenedColor = Color.FromArgb(color.A, (int)(color.R * brightness), (int)(color.G * brightness), (int)(color.B * brightness));
                        if (useGourauShading)
                        {
                            var n = x2 == x1 ? pn1 : pn1 + (x - x1) * (pn2 - pn1) / (x2 - x1);
                            n = MakePositiveNormal(n);
                            brightness = ClipValue(0, 1, lightVector.X * n.X + lightVector.Y * n.Y + lightVector.Z * n.Z);
                            brightenedColor = Color.FromArgb(color.A, (int)(color.R * brightness), (int)(color.G * brightness), (int)(color.B * brightness));
                        }
                        if (brightenedColor.A == 0) continue;                                 // 透明な色は塗らず、Zバッファを更新しない
                        for (int i = 0; i < 4; ++i) screenImgData[stride * y + 4 * x + i] = (byte)((brightenedColor.ToArgb() >> (i * 8)) & 0xFF);
                        zBuffers[x, y] = z;                                                    // 奥行の値を更新
                    }
                }
            }
        }

        private Vector3 GetNormalOfTrigngle(Vector3[] vertices)
        {
            Vector3 a = new(vertices[1].X - vertices[0].X, vertices[1].Y - vertices[0].Y, vertices[1].Z - vertices[0].Z);
            Vector3 b = new(vertices[2].X - vertices[0].X, vertices[2].Y - vertices[0].Y, vertices[2].Z - vertices[0].Z);
            float n = MathF.Sqrt(MathF.Pow(a.Y * b.Z - a.Z * b.Y, 2) + MathF.Pow(a.Z * b.X - a.X * b.Z, 2) + MathF.Pow(a.X * b.Y - a.Y * b.X, 2));
            return new((a.Y * b.Z - a.Z * b.Y) / n, (a.Z * b.X - a.X * b.Z) / n, (a.X * b.Y - a.Y * b.X) / n); // 法線ベクトル
        }

        /// <summary>
        /// 法線を視点(Z方向)から見て正の方向になるようにする
        /// </summary>
        private Vector3 MakePositiveNormal(Vector3 normal)
        {
            return normal.Z > 0 ? normal : -normal;
        }

        /// <summary>
        /// 値を上下限値でクリップする
        /// </summary>
        private float ClipValue(float lowerLimit, float upperLimit, float value)
        {
            return MathF.Min(MathF.Max(lowerLimit, upperLimit), MathF.Max(MathF.Min(lowerLimit, upperLimit), value));
        }
    }
}
