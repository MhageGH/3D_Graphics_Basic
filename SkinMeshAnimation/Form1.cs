using System.Numerics;

namespace SkinMeshAnimation
{
    public partial class Form1 : Form
    {
        readonly float viewScale = 45;
        Vector3 lightVector = new(0, -0.5f, 1f);
        Vector3 viewTranslationVector = new(600f, 900f, 0);
        Vector3 lookAtPoint = new(0, 0, 0);
        Vector3 eyePoint = new(0.2f, 0.2f, 1);
        readonly Model model = new("../../../../Data/初音ミク/初音ミク.pmx", true);
        readonly BoneData boneData = new();
        int frameNumber = 0;

        public Form1()
        {
            InitializeComponent();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            Invalidate();
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            var lookDirection = Vector3.Normalize(lookAtPoint - eyePoint);
            var thetaY = -MathF.Atan2(lookDirection.X, -lookDirection.Z);
            var thetaX = -MathF.Atan2(lookDirection.Y, -lookDirection.Z);
            var screen = new Bitmap(e.ClipRectangle.Width, e.ClipRectangle.Height);
            var renderer = new Renderer(screen, lightVector, true, true);
            var positionMatrix = Matrix.CreateTranslation(new Vector3(0, 0, 0));
            var viewMatrix = Matrix.CreateTranslation(viewTranslationVector) * Matrix.CreateScale(viewScale) * Matrix.CreateRotationX(thetaX) * Matrix.CreateRotationY(thetaY);
            var vpMatrix = viewMatrix * positionMatrix;
            renderer.DrawModel(model, vpMatrix, boneData.matrices[frameNumber]);
            renderer.Render();
            e.Graphics.DrawImage(screen, 0, 0);
            //frameNumber = (frameNumber + 1) % boneData.matrices.GetLength(0);
        }
    }
}
