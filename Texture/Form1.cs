using System.Numerics;

namespace Texture
{
    public partial class Form1 : Form
    {
        float thetaY = 0f;                                          // Y����]�p�x
        float thetaZ = 0f;                                          // Z����]�p�x
        float scale = 500f;                                         // �g��W��
        Vector3 offset1 = new(300f, 450f, 0);                        // ���s�ړ��̗�
        Vector3 offset2 = new(800f, 450f, 0);                        // ���s�ړ��̗�
        Vector3 light = new(0, 1 / MathF.Sqrt(2), 1 / MathF.Sqrt(2)); // ���̕����x�N�g��
        Model model = new();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            var bitmap = new Bitmap(e.ClipRectangle.Width, e.ClipRectangle.Height); // ��ʃT�C�Y�̉摜�f�[�^�����
            var zBuffers = new float[e.ClipRectangle.Width, e.ClipRectangle.Height];      // �s�N�Z���̉��s���̒l
            for (int i = 0; i < zBuffers.GetLength(0); i++) for (int j = 0; j < zBuffers.GetLength(1); j++) zBuffers[i, j] = float.MaxValue; // �����l
            var transformedVertices = TransformVertices(model.vertices, offset1);                  // ���_�̕��s�ړ��Ɖ�]
            DrawPolygons(transformedVertices, bitmap, zBuffers);
            transformedVertices = TransformVertices(model.vertices, offset2);
            var transformedPseudoNormals = TransformPsudoNormals(model.pseudoNormals);
            DrawPolygons(transformedVertices, bitmap, zBuffers, transformedPseudoNormals);
            e.Graphics.DrawImage(bitmap, 0, 0);                                     //�摜�f�[�^����ʂɕ\������
        }

        private void DrawPolygons(Vector3[] vertices, Bitmap bitmap, float[,] zBuffer, Vector3[]? pseudoNormals = null)
        {
            for (int m = 0; m < model.faces.GetLength(0); ++m)
            {
                var vs_pns = new (Vector3, Vector3)[model.faces.GetLength(1)];              // �^���@���x�N�g�����ꏏ�ɕ��ёւ���
                for (int i = 0; i < vs_pns.Length; ++i) vs_pns[i] = (vertices[model.faces[m, i]], pseudoNormals == null ? new Vector3() : pseudoNormals[model.faces[m, i]]);
                vs_pns = vs_pns.OrderBy(v => v.Item1.Y).ToArray();
                Vector3[] vs = new Vector3[model.faces.GetLength(1)], pns = new Vector3[model.faces.GetLength(1)];
                for (int i = 0; i < vs_pns.Length; ++i) (vs[i], pns[i]) = vs_pns[i];
                if (MathF.Abs(vs[0].Y - vs[2].Y) < 0.1f) continue;                      // �O�p�`����Ȃ�
                var normal = GetNormalOfTrigngle(vs);
                float brightness = MathF.Abs(light.X * normal.X + light.Y * normal.Y + light.Z * normal.Z);
                for (int y = (int)vs[0].Y; y < vs[2].Y; ++y)                            // �O�p�`�𕢂��S�Ẳ����ɂ��čs��
                {
                    int p = (MathF.Abs(vs[0].Y - vs[1].Y) < 0.1f || y >= vs[1].Y) ? 1 : 0;
                    var x1 = vs[p].X + (y - vs[p].Y) * (vs[p + 1].X - vs[p].X) / (vs[p + 1].Y - vs[p].Y);
                    var z1 = vs[p].Z + (y - vs[p].Y) * (vs[p + 1].Z - vs[p].Z) / (vs[p + 1].Y - vs[p].Y);
                    var x2 = vs[0].X + (y - vs[0].Y) * (vs[2].X - vs[0].X) / (vs[2].Y - vs[0].Y);
                    var z2 = vs[0].Z + (y - vs[0].Y) * (vs[2].Z - vs[0].Z) / (vs[2].Y - vs[0].Y);
                    x1 = MathF.Min(MathF.Max(vs[p].X, vs[p + 1].X), MathF.Max(MathF.Min(vs[p].X, vs[p + 1].X), x1)); // �v�Z�덷�΍�
                    x2 = MathF.Min(MathF.Max(vs[0].X, vs[2].X), MathF.Max(MathF.Min(vs[0].X, vs[2].X), x2));
                    Vector3 n1 = new(), n2 = new();
                    if (pseudoNormals != null)
                    {
                        n1 = pns[p] + (y - vs[p].Y) * (pns[p + 1] - pns[p]) / (vs[p + 1].Y - vs[p].Y);
                        n2 = pns[0] + (y - vs[0].Y) * (pns[2] - pns[0]) / (vs[2].Y - vs[0].Y);
                    }
                    var base_color = Color.SkyBlue;
                    var color = Color.FromArgb(255, (int)(base_color.R * brightness), (int)(base_color.G * brightness), (int)(base_color.B * brightness));
                    for (int x = (int)Math.Min(x1, x2); x <= (int)Math.Max(x1, x2); ++x)
                    {
                        var z = x2 == x1 ? z1 : z1 + (x - x1) * (z2 - z1) / (x2 - x1);       // Z���W���v�Z
                        if (z > zBuffer[x, y]) continue;                                     // ����̂��̂����ɂ���Ή������Ȃ�
                        zBuffer[x, y] = z;                                                   // ��O�ɂ���Ή��s�̒l���X�V���ăs�N�Z����h��
                        if (pseudoNormals != null)
                        {
                            var n = x2 == x1 ? n1 : n1 + (x - x1) * (n2 - n1) / (x2 - x1);
                            brightness = MathF.Min(1, MathF.Abs(light.X * n.X + light.Y * n.Y + light.Z * n.Z));
                            color = Color.FromArgb(255, (int)(base_color.R * brightness), (int)(base_color.G * brightness), (int)(base_color.B * brightness));
                        }
                        bitmap.SetPixel(x, y, color);
                    }
                }
            }
        }

        Vector3[] TransformVertices(Vector3[] vertices, Vector3 offset)
        {
            var vs = new Vector3[vertices.Length];  // �ړ���̒��_
            for (int i = 0; i < vs.Length; ++i)
            {
                Vector3 v = new(vertices[i].X, -vertices[i].Y, vertices[i].Z);   // MMD���f����Y�������]���Ă���
                v = new(v.Z * MathF.Sin(thetaY) + v.X * MathF.Cos(thetaY), v.Y, v.Z * MathF.Cos(thetaY) - v.X * MathF.Sin(thetaY));// Y����]
                v = new(v.X * MathF.Cos(thetaZ) - v.Y * MathF.Sin(thetaZ), v.X * MathF.Sin(thetaZ) + v.Y * MathF.Cos(thetaZ), v.Z);// Z����]
                v = new(scale * v.X, scale * v.Y, scale * v.Z);                    // �g��
                v = new(v.X + offset.X, v.Y + offset.Y, v.Z + offset.Z);           // ���s�ړ�
                vs[i] = new(v.X, v.Y, v.Z);
            }
            return vs;
        }

        Vector3[] TransformPsudoNormals(Vector3[] normals)
        {
            var ns = new Vector3[normals.Length];  // �ړ���̖@���x�N�g��
            for (int i = 0; i < ns.Length; ++i)
            {
                Vector3 n = new(normals[i].X, -normals[i].Y, normals[i].Z);     // MMD���f����Y�������]���Ă���
                n = new(n.Z * MathF.Sin(thetaY) + n.X * MathF.Cos(thetaY), n.Y, n.Z * MathF.Cos(thetaY) - n.X * MathF.Sin(thetaY));// Y����]
                n = new(n.X * MathF.Cos(thetaZ) - n.Y * MathF.Sin(thetaZ), n.X * MathF.Sin(thetaZ) + n.Y * MathF.Cos(thetaZ), n.Z);// Z����]
                ns[i] = new(n.X, n.Y, n.Z);
            }
            return ns;
        }

        Vector3 GetNormalOfTrigngle(Vector3[] vertices)
        {
            Vector3 a = new(vertices[1].X - vertices[0].X, vertices[1].Y - vertices[0].Y, vertices[1].Z - vertices[0].Z);
            Vector3 b = new(vertices[2].X - vertices[0].X, vertices[2].Y - vertices[0].Y, vertices[2].Z - vertices[0].Z);
            float n = MathF.Sqrt(MathF.Pow(a.Y * b.Z - a.Z * b.Y, 2) + MathF.Pow(a.Z * b.X - a.X * b.Z, 2) + MathF.Pow(a.X * b.Y - a.Y * b.X, 2));
            return new((a.Y * b.Z - a.Z * b.Y) / n, (a.Z * b.X - a.X * b.Z) / n, (a.X * b.Y - a.Y * b.X) / n); // �@���x�N�g��
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            thetaY += 0.05f; // ��莞�Ԃ��Ƃ�Y����]�p�x�𑝂₷
            Invalidate();
        }
    }
}