using System.Numerics;

namespace Texture
{
    public partial class Form1 : Form
    {
        float thetaY = 0f;                                          // Y����]�p�x
        float thetaZ = 0f;                                          // Z����]�p�x
        float scale = 500f;                                         // �g��W��
        Vector3 offset = new(300f, 450f, 0);                        // ���s�ړ��̗�
        Vector3 light = new(0, 1 / MathF.Sqrt(2), 1 / MathF.Sqrt(2)); // ���̕����x�N�g��
        Model model = new("../../../1.csv");
        Image texture = Bitmap.FromFile("../../../1.bmp");

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            var screen = new Bitmap(e.ClipRectangle.Width, e.ClipRectangle.Height);     // ��ʃT�C�Y�̉摜�f�[�^�����
            var zBuffers = new float[e.ClipRectangle.Width, e.ClipRectangle.Height];    // �s�N�Z���̉��s���̒l
            for (int i = 0; i < zBuffers.GetLength(0); i++) for (int j = 0; j < zBuffers.GetLength(1); j++) zBuffers[i, j] = float.MaxValue; // �����l
            var transformedVertices = TransformVertices(model.vertices, offset);        // ���_�̕��s�ړ��Ɖ�]
            DrawPolygons(transformedVertices, screen, zBuffers, true);
            e.Graphics.DrawImage(screen, 0, 0);                                         //�摜�f�[�^����ʂɕ\������
        }

        private void DrawPolygons(Vertex[] vertices, Bitmap screen, float[,] zBuffer, bool gourauShading)
        {
            for (int m = 0; m < model.faces.GetLength(0); ++m)
            {
                var length = model.faces.GetLength(1);                                      // �|���S������Ƃ̒��_�̐��BMMD�̏ꍇ�͏��3�B
                var vs = new Vertex[length];
                for (int i = 0; i < length; ++i)
                {
                    var v = vertices[model.faces[m, i]];
                    vs[i] = new Vertex(v.pos, v.pseudoNormal, v.uv);
                }
                vs = vs.OrderBy(v => v.pos.Y).ToArray();
                if (MathF.Abs(vs[0].pos.Y - vs[2].pos.Y) < 0.1f) continue;                  // �O�p�`����Ȃ�
                var normal = GetNormalOfTrigngle(vs.Select(v => v.pos).ToArray());
                float brightness = MathF.Abs(light.X * normal.X + light.Y * normal.Y + light.Z * normal.Z);
                for (int y = (int)vs[0].pos.Y; y < vs[2].pos.Y; ++y)                        // �O�p�`�𕢂��S�Ẳ����ɂ��čs��
                {
                    int p = (MathF.Abs(vs[0].pos.Y - vs[1].pos.Y) < 0.1f || y >= vs[1].pos.Y) ? 1 : 0;
                    var x1 = vs[p].pos.X + (y - vs[p].pos.Y) * (vs[p + 1].pos.X - vs[p].pos.X) / (vs[p + 1].pos.Y - vs[p].pos.Y);
                    var z1 = vs[p].pos.Z + (y - vs[p].pos.Y) * (vs[p + 1].pos.Z - vs[p].pos.Z) / (vs[p + 1].pos.Y - vs[p].pos.Y);
                    var x2 = vs[0].pos.X + (y - vs[0].pos.Y) * (vs[2].pos.X - vs[0].pos.X) / (vs[2].pos.Y - vs[0].pos.Y);
                    var z2 = vs[0].pos.Z + (y - vs[0].pos.Y) * (vs[2].pos.Z - vs[0].pos.Z) / (vs[2].pos.Y - vs[0].pos.Y);
                    x1 = MathF.Min(MathF.Max(vs[p].pos.X, vs[p + 1].pos.X), MathF.Max(MathF.Min(vs[p].pos.X, vs[p + 1].pos.X), x1)); // �v�Z�덷�΍�
                    x2 = MathF.Min(MathF.Max(vs[0].pos.X, vs[2].pos.X), MathF.Max(MathF.Min(vs[0].pos.X, vs[2].pos.X), x2));
                    Vector3 n1 = new(), n2 = new();
                    n1 = vs[p].pseudoNormal + (y - vs[p].pos.Y) * (vs[p + 1].pseudoNormal - vs[p].pseudoNormal) / (vs[p + 1].pos.Y - vs[p].pos.Y);
                    n2 = vs[0].pseudoNormal + (y - vs[0].pos.Y) * (vs[2].pseudoNormal - vs[0].pseudoNormal) / (vs[2].pos.Y - vs[0].pos.Y);
                    var base_color = Color.SkyBlue;                                         // TODO �e�N�X�`���摜�ɒu��������
                    var color = Color.FromArgb(255, (int)(base_color.R * brightness), (int)(base_color.G * brightness), (int)(base_color.B * brightness));
                    for (int x = (int)Math.Min(x1, x2); x <= (int)Math.Max(x1, x2); ++x)
                    {
                        var z = x2 == x1 ? z1 : z1 + (x - x1) * (z2 - z1) / (x2 - x1);      // Z���W���v�Z
                        if (z > zBuffer[x, y]) continue;                                    // ����̂��̂����ɂ���Ή������Ȃ�
                        zBuffer[x, y] = z;                                                  // ��O�ɂ���Ή��s�̒l���X�V���ăs�N�Z����h��
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
            var vs = new Vertex[vertices.Length];  // �ړ���̒��_
            for (int i = 0; i < vs.Length; ++i)
            {
                Vector3 pos = new(vertices[i].pos.X, -vertices[i].pos.Y, vertices[i].pos.Z);   // MMD���f����Y�������]���Ă���
                pos = new(pos.Z * MathF.Sin(thetaY) + pos.X * MathF.Cos(thetaY), pos.Y, pos.Z * MathF.Cos(thetaY) - pos.X * MathF.Sin(thetaY));// Y����]
                pos = new(pos.X * MathF.Cos(thetaZ) - pos.Y * MathF.Sin(thetaZ), pos.X * MathF.Sin(thetaZ) + pos.Y * MathF.Cos(thetaZ), pos.Z);// Z����]
                pos = new(scale * pos.X, scale * pos.Y, scale * pos.Z);                    // �g��
                pos = new(pos.X + offset.X, pos.Y + offset.Y, pos.Z + offset.Z);           // ���s�ړ�
                Vector3 pn = new(vertices[i].pseudoNormal.X, -vertices[i].pseudoNormal.Y, vertices[i].pseudoNormal.Z);     // MMD���f����Y�������]���Ă���
                pn = new(pn.Z * MathF.Sin(thetaY) + pn.X * MathF.Cos(thetaY), pn.Y, pn.Z * MathF.Cos(thetaY) - pn.X * MathF.Sin(thetaY));// Y����]
                pn = new(pn.X * MathF.Cos(thetaZ) - pn.Y * MathF.Sin(thetaZ), pn.X * MathF.Sin(thetaZ) + pn.Y * MathF.Cos(thetaZ), pn.Z);// Z����]
                vs[i] = new Vertex(pos, pn, null);
            }
            return vs;
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