using System.Numerics;

namespace SkinMeshAnimation
{
    internal class Matrix
    {
        public float[,] elements = new float[4, 4];

        public Matrix() { }

        static public Matrix Identity
        {
            get
            {
                var matrix = new Matrix();
                for (int i = 0; i < 4; ++i) matrix.elements[i, i] = 1;
                return matrix;
            }
        }

        static public Matrix CreateRotationZ(float theta)
        {
            var matrix = Matrix.Identity;
            matrix.elements[0, 0] = MathF.Cos(theta);
            matrix.elements[0, 1] = -MathF.Sin(theta);
            matrix.elements[1, 0] = MathF.Sin(theta);
            matrix.elements[1, 1] = MathF.Cos(theta);
            return matrix;
        }

        static public Matrix CreateRotationX(float theta)
        {
            var matrix = Matrix.Identity;
            matrix.elements[1, 1] = MathF.Cos(theta);
            matrix.elements[1, 2] = -MathF.Sin(theta);
            matrix.elements[2, 1] = MathF.Sin(theta);
            matrix.elements[2, 2] = MathF.Cos(theta);
            return matrix;
        }

        static public Matrix CreateRotationY(float theta)
        {
            var matrix = Matrix.Identity;
            matrix.elements[0, 0] = MathF.Cos(theta);
            matrix.elements[0, 2] = MathF.Sin(theta);
            matrix.elements[2, 0] = -MathF.Sin(theta);
            matrix.elements[2, 2] = MathF.Cos(theta);
            return matrix;
        }

        static public Matrix CreateScale(float k)
        {
            var matrix = Matrix.Identity;
            for (int i = 0; i < 3; ++i) matrix.elements[i, i] = k;
            return matrix;
        }

        static public Matrix CreateScale(float k_x, float k_y, float k_z)
        {
            var matrix = Matrix.Identity;
            matrix.elements[0, 0] = k_x;
            matrix.elements[1, 1] = k_y;
            matrix.elements[2, 2] = k_z;
            return matrix;
        }

        static public Matrix CreateTranslation(Vector3 v)
        {
            var matrix = Matrix.Identity;
            matrix.elements[0, 3] = v.X;
            matrix.elements[1, 3] = v.Y;
            matrix.elements[2, 3] = v.Z;
            return matrix;
        }

        public static Matrix operator *(Matrix matrix1, Matrix matrix2)
        {
            var matrix = new Matrix();
            for (int i = 0; i < 4; ++i) for (int j = 0; j < 4; ++j) for (int k = 0; k < 4; ++k)
                        matrix.elements[i, j] += matrix1.elements[i, k] * matrix2.elements[k, j];
            return matrix;
        }

        public static Vector3 operator *(Matrix matrix, Vector3 v)
        {
            var _v = new Vector4(v, 1);
            var _v_out = new Vector4();
            for (int i = 0; i < 4; ++i) for (int j = 0; j < 4; ++j) _v_out[i] += matrix.elements[i, j] * _v[j];
            return new Vector3(_v_out.X, _v_out.Y, _v_out.Z);
        }
    }
}
