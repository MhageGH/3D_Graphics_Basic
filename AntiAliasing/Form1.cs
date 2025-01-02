using System.Numerics;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace AntiAliasing
{
    public partial class Form1 : Form
    {
        readonly float viewScale = 45 * 2;
        Vector3 lightVector = new(0, -0.5f, 1f);
        Vector3 viewTranslationVector = new(600f * 2, 900f * 2, 0);
        Vector3 lookAtPoint = new(0, 0, 0);
        Vector3 eyePoint = new(0.2f, 0.2f, 1);
        readonly Model model = new("../../../../Data/初音ミク/初音ミク.pmx", true);
        readonly BoneData boneData = new("../../../../Data/Bone.dat");
        int frameNumber = 0;
        VideoWriter vw = new("movie.mp4", FourCC.H264, 30, new OpenCvSharp.Size(1000, 900));

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
            var antiAliasing = new AntiAliasing(new Bitmap(e.ClipRectangle.Width, e.ClipRectangle.Height));
            var screen2 = antiAliasing.screen2;
            var renderer = new Renderer(screen2, lightVector, true, true);
            var positionMatrix = Matrix.CreateTranslation(new Vector3(0, 0, 0));
            var viewMatrix = Matrix.CreateTranslation(viewTranslationVector) * Matrix.CreateScale(viewScale) * Matrix.CreateRotationX(thetaX) * Matrix.CreateRotationY(thetaY);
            var vpMatrix = viewMatrix * positionMatrix;
            renderer.DrawModel(model, vpMatrix, boneData.matrices[frameNumber]);
            renderer.Render();
            var screen = antiAliasing.GetDownSamplingImage();
            e.Graphics.DrawImage(screen, 0, 0);
            
            var mat = screen.Clone(new Rectangle(0, 0, vw.FrameSize.Width, vw.FrameSize.Height), screen.PixelFormat).ToMat();
            vw.Write(mat);
            mat.Dispose();

            frameNumber++;
            int endFrameNumber = 1000;
            if (frameNumber == boneData.matrices.GetLength(0) || frameNumber == endFrameNumber) Close();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            vw.Release();
            vw.Dispose();
        }
    }
}
