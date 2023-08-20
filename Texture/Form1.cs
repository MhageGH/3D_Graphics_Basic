using System.Numerics;

namespace Texture
{
    public partial class Form1 : Form
    {
        float thetaX = 0.3f;                                               // X����]�p�x
        float thetaY = 0.5f;                                               // Y����]�p�x
        float thetaZ = 0;                                                           // Z����]�p�x
        float scale = 50f;                                                          // �g��W��
        const float light_thetaX = 1 * MathF.PI / 3;
        Vector3 light = new(0, MathF.Cos(light_thetaX), MathF.Sin(light_thetaX));   // ���̕����x�N�g��
        Vector3 offset = new(300f, 450f, 0);                                        // ���s�ړ��̗�
        //Model model = new("../../../Model/1.csv");                                  // 1.csv�͋��`���������ʒu�Ŗ@���x�N�g�����قȂ�ʂ̒��_�����݂��A�@�����s�A���ɐ؂�ւ��(��:���_1�ƒ��_396)
        Model model = new("../../../Model/Shanghai.csv");

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
            DrawPolygons(transformedVertices, screen, zBuffers, true, true);            // �e�N�X�`���}�b�s���O�ƁA�O�[���[�V�F�[�f�B���O�͈�����ON/OFF�؂�ւ��\
            e.Graphics.DrawImage(screen, 0, 0);                                         // �摜�f�[�^����ʂɕ\������
        }

        private void DrawPolygons(Vertex[] vertices, Bitmap screen, float[,] zBuffer, bool textureMapping, bool gourauShading)
        {
            for (int m = 0; m < model.faces.Length; ++m)
            {
                var color = model.faces[m].material.color;
                var texture = model.faces[m].material.texture;
                var length = model.faces[m].vertexNumbers.Length;                          // �|���S������Ƃ̒��_�̐��BMMD�̏ꍇ�͏��3�B
                var vs = new Vertex[length];
                for (int i = 0; i < length; ++i)
                {
                    var v = vertices[model.faces[m].vertexNumbers[i]];
                    vs[i] = new Vertex(v.pos, v.pseudoNormal, v.uv);
                }
                vs = vs.OrderBy(v => v.pos.Y).ToArray();
                if (MathF.Abs(vs[0].pos.Y - vs[2].pos.Y) < 0.1f) continue;                  // �O�p�`����Ȃ�
                var normal = MakePositiveNormal(GetNormalOfTrigngle(vs.Select(v => v.pos).ToArray()));
                float brightness = Clip(0, 1, light.X * normal.X + light.Y * normal.Y + light.Z * normal.Z);
                for (int y = (int)vs[0].pos.Y; y < vs[2].pos.Y; ++y)                        // �O�p�`�𕢂��S�Ẳ����ɂ��čs��
                {
                    if (y < 0 || y >= screen.Height) continue;                              // ��ʃT�C�Y�ŃN���b�s���O
                    int j = (MathF.Abs(vs[0].pos.Y - vs[1].pos.Y) < 0.1f || y >= vs[1].pos.Y) ? 1 : 0;
                    var x1 = Clip(vs[j].pos.X, vs[j + 1].pos.X, vs[j].pos.X + (y - vs[j].pos.Y) * (vs[j + 1].pos.X - vs[j].pos.X) / (vs[j + 1].pos.Y - vs[j].pos.Y));  // �v�Z�덷�΍�
                    var x2 = Clip(vs[0].pos.X, vs[2].pos.X, vs[0].pos.X + (y - vs[0].pos.Y) * (vs[2].pos.X - vs[0].pos.X) / (vs[2].pos.Y - vs[0].pos.Y));
                    var z1 = vs[j].pos.Z + (y - vs[j].pos.Y) * (vs[j + 1].pos.Z - vs[j].pos.Z) / (vs[j + 1].pos.Y - vs[j].pos.Y);
                    var z2 = vs[0].pos.Z + (y - vs[0].pos.Y) * (vs[2].pos.Z - vs[0].pos.Z) / (vs[2].pos.Y - vs[0].pos.Y);
                    var pn1 = vs[j].pseudoNormal + (y - vs[j].pos.Y) * (vs[j + 1].pseudoNormal - vs[j].pseudoNormal) / (vs[j + 1].pos.Y - vs[j].pos.Y);
                    var pn2 = vs[0].pseudoNormal + (y - vs[0].pos.Y) * (vs[2].pseudoNormal - vs[0].pseudoNormal) / (vs[2].pos.Y - vs[0].pos.Y);
                    var uv1 = vs[j].uv + (y - vs[j].pos.Y) * (vs[j + 1].uv - vs[j].uv) / (vs[j + 1].pos.Y - vs[j].pos.Y);
                    var uv2 = vs[0].uv + (y - vs[0].pos.Y) * (vs[2].uv - vs[0].uv) / (vs[2].pos.Y - vs[0].pos.Y);
                    for (int x = (int)Math.Min(x1, x2); x <= (int)Math.Max(x1, x2); ++x)
                    {
                        if (x < 0 || x >= screen.Width) continue;                           // ��ʃT�C�Y�ŃN���b�s���O
                        var z = x2 == x1 ? z1 : z1 + (x - x1) * (z2 - z1) / (x2 - x1);      // Z���W���v�Z
                        if (z > zBuffer[x, y]) continue;                                    // ����̂��̂����ɂ���Ή������Ȃ�
                        if (textureMapping)
                        {
                            var uv = x2 == x1 ? uv1 : uv1 + (x - x1) * (uv2 - uv1) / (x2 - x1);
                            uv.X = Clip(0, 1, uv.X);                                        // �v�Z�덷�΍�
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
                        if (brightenedColor.A == 0) continue;                                 // �����ȐF�͓h�炸�AZ�o�b�t�@���X�V���Ȃ�
                        screen.SetPixel(x, y, brightenedColor);
                        zBuffer[x, y] = z;                                                    // ���s�̒l���X�V
                    }
                }
            }
        }

        float Clip(float min, float max, float value)
        {
            return MathF.Min(MathF.Max(min, max), MathF.Max(MathF.Min(min, max), value));
        }

        /// <summary>
        /// �@�������_(Z����)���猩�Đ��̕����ɂȂ�悤�ɂ���
        /// </summary>
        Vector3 MakePositiveNormal(Vector3 normal)
        {
            return normal.Z > 0 ? normal : -normal;
        }

        Vertex[] TransformVertices(Vertex[] vertices, Vector3 offset)
        {
            var vs = new Vertex[vertices.Length];  // �ړ���̒��_
            for (int i = 0; i < vs.Length; ++i)
            {
                Vector3 pos = new(vertices[i].pos.X, -vertices[i].pos.Y, vertices[i].pos.Z);   // MMD���f����Y�������]���Ă���
                pos = new(pos.Z * MathF.Sin(thetaY) + pos.X * MathF.Cos(thetaY), pos.Y, pos.Z * MathF.Cos(thetaY) - pos.X * MathF.Sin(thetaY));// Y����]
                pos = new(pos.X * MathF.Cos(thetaZ) - pos.Y * MathF.Sin(thetaZ), pos.X * MathF.Sin(thetaZ) + pos.Y * MathF.Cos(thetaZ), pos.Z);// Z����]
                pos = new(pos.X, pos.Y * MathF.Cos(thetaX) - pos.Z * MathF.Sin(thetaX), pos.Y * MathF.Sin(thetaX) + pos.Z * MathF.Cos(thetaX));// X����]
                pos = new(scale * pos.X, scale * pos.Y, scale * pos.Z);                    // �g��
                pos = new(pos.X + offset.X, pos.Y + offset.Y, pos.Z + offset.Z);           // ���s�ړ�
                Vector3 pn = new(vertices[i].pseudoNormal.X, -vertices[i].pseudoNormal.Y, vertices[i].pseudoNormal.Z);     // MMD���f����Y�������]���Ă���
                pn = new(pn.Z * MathF.Sin(thetaY) + pn.X * MathF.Cos(thetaY), pn.Y, pn.Z * MathF.Cos(thetaY) - pn.X * MathF.Sin(thetaY));// Y����]
                pn = new(pn.X * MathF.Cos(thetaZ) - pn.Y * MathF.Sin(thetaZ), pn.X * MathF.Sin(thetaZ) + pn.Y * MathF.Cos(thetaZ), pn.Z);// Z����]
                pn = new(pn.X, pn.Y * MathF.Cos(thetaX) - pn.Z * MathF.Sin(thetaX), pn.Y * MathF.Sin(thetaX) + pn.Z * MathF.Cos(thetaX));// X����]
                vs[i] = new Vertex(pos, pn, vertices[i].uv);
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
            //thetaY += 0.05f; // ��莞�Ԃ��Ƃ�Y����]�p�x�𑝂₷
            Invalidate();
        }
    }
}