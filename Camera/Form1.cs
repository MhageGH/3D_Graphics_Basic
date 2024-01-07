using System.Numerics;

namespace Camera
{
    public partial class Form1 : Form
    {
        float thetaX = 0.2f, thetaY = -0.4f, thetaZ = 0, scale = 5;
        Vector3 lightVector = new(0, 0.707f, 0.997f);
        Vector3 translationVector = new(300f, 450f, 0);
        Model model = new("D:\\OneDrive\\hLg\\MyProgram\\MMD\\MikuMikuDance_v909x64\\UserFile\\Model\\NuKasa_í_Ğmk2\\í_Ğ(Â®Èª).pmx", true);

        public Form1()
        {
            InitializeComponent();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            thetaY += 0.1f;
            Invalidate();
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            var screen = new Bitmap(e.ClipRectangle.Width, e.ClipRectangle.Height);
            var render = new Render(screen, lightVector, true, true);
            var matrix = Matrix.CreateTranslation(translationVector) * Matrix.CreateScale(scale) * Matrix.CreateRotationX(thetaX) * Matrix.CreateRotationZ(thetaZ) * Matrix.CreateRotationY(thetaY);
            render.DrawModel(model, matrix);
            e.Graphics.DrawImage(screen, 0, 0);
        }
    }
}
