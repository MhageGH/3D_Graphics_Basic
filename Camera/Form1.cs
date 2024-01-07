using System.Numerics;

namespace Camera
{
    public partial class Form1 : Form
    {
        float thetaX = 0.2f, thetaY = -0.4f, thetaZ = 0, viewScale = 5, localScale1 = 5;
        Vector3 lightVector = new(0, 0.707f, 0.997f);
        Vector3 translationVector = new(300f, 450f, 0);
        List<Model> models = new()
        {
            new("D:\\OneDrive\\ドキュメント\\MyProgram\\MMD\\MikuMikuDance_v909x64\\UserFile\\Model\\NuKasa_博麗神社mk2\\博麗神社(可動部省略).pmx", true),
            new("D:\\OneDrive\\ドキュメント\\MyProgram\\MMD\\MikuMikuDance_v909x64\\UserFile\\Model\\改造後再配布可\\kirby\\カービィ.pmx", true)
        };

        public Form1()
        {
            InitializeComponent();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            thetaY += 0.01f;
            Invalidate();
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            var screen = new Bitmap(e.ClipRectangle.Width, e.ClipRectangle.Height);
            var renderer = new Renderer(screen, lightVector, true, true);
            var localTransMatrices = new List<Matrix>() { Matrix.Identity, Matrix.CreateScale(localScale1) };
            var worldTransMatrix = new List<Matrix>() { Matrix.Identity, Matrix.Identity };
            var viewTransMatrix = Matrix.CreateTranslation(translationVector) * Matrix.CreateScale(viewScale) * Matrix.CreateRotationX(thetaX) * Matrix.CreateRotationZ(thetaZ) * Matrix.CreateRotationY(thetaY);
            for (int i = 0; i < models.Count; ++i) renderer.DrawModel(models[i], viewTransMatrix * worldTransMatrix[i] * localTransMatrices[i]);
            renderer.Render();
            e.Graphics.DrawImage(screen, 0, 0);
        }
    }
}
