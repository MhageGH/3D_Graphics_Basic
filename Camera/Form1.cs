using System.Numerics;

namespace Camera
{
    public partial class Form1 : Form
    {
        float t = 0;
        float viewScale = 5, localScale1X = 3, localScale1Y = 3, localScale1Z = 3, localHeight = 0;
        Vector3 lightVector = new(0, 0.707f, 0.997f);
        Vector3 viewTranslationVector = new(300f, 450f, 0);
        Vector3 lookAtPoint = new(0, 0, 0);
        Vector3 eyePoint = new(0.2f, 0.2f, 1);
        List<Model> models = new()
        {
            new("../../../../Data/NuKasa_����_��mk2/����_��(�����ȗ�).pmx", true),
            new("../../../../Data/kirby/�J�[�r�B.pmx", true)
        };

        public Form1()
        {
            InitializeComponent();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            float T = 20f;
            t = t < 2 * T ? t + 1f : 0;
            var t1 = t % T;
            localScale1X = 3 - 1.5f * MathF.Sin(2 * MathF.PI * t / T);
            localScale1Y = 3 + 1.5f * MathF.Sin(2 * MathF.PI * t / T);
            localScale1Z = 3 - 1.5f * MathF.Sin(2 * MathF.PI * t / T);
            localHeight = t1 < T / 2 ? 0.5f * t1 * (t1 - T / 2) : 0;
            eyePoint.X = 0.2f - 0.001f * t;
            Invalidate();
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            var lookDirection = Vector3.Normalize(lookAtPoint - eyePoint);
            var thetaY = -MathF.Atan2(lookDirection.X, -lookDirection.Z);
            var thetaX = -MathF.Atan2(lookDirection.Y, -lookDirection.Z);
            var screen = new Bitmap(e.ClipRectangle.Width, e.ClipRectangle.Height);
            var renderer = new Renderer(screen, lightVector, true, true);
            var localTransMatrices = new List<Matrix>() { Matrix.Identity, Matrix.CreateScale(localScale1X, localScale1Y, localScale1Z) };
            var worldTransMatrix = new List<Matrix>() { Matrix.Identity, Matrix.CreateTranslation(new Vector3(0, localHeight, 0)) };
            var viewTransMatrix = Matrix.CreateTranslation(viewTranslationVector) * Matrix.CreateScale(viewScale) * Matrix.CreateRotationX(thetaX) * Matrix.CreateRotationY(thetaY);
            for (int i = 0; i < models.Count; ++i) renderer.DrawModel(models[i], viewTransMatrix * worldTransMatrix[i] * localTransMatrices[i]);
            renderer.Render();
            e.Graphics.DrawImage(screen, 0, 0);
        }
    }
}
