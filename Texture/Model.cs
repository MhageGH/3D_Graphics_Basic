using System.Numerics;

namespace Texture
{
    internal class Model
    {
        public Vector3[] vertices;              // 頂点の座標
        public Vector3[] pseudoNormals;         // 頂点の疑似法線ベクトル
        public Vector2[] UVs;                   // 頂点のUV座標
        public int[,] faces;                    // 頂点番号の組み合わせ

        public Model(String filename)
        {
            var lines = File.ReadAllLines(filename);
            var data = new String[lines.Length][];
            for (int i = 0; i < lines.Length; i++) data[i] = lines[i].Split(',');
            var vertexList = new List<Vector3>();
            var pseudoNormalList = new List<Vector3>();
            var uvList = new List<Vector2>();
            var faceList = new List<int[]>();
            for (int i = 0; i < lines.Length; i++)
            {
                if (data[i][0] == "Vertex")
                {
                    vertexList.Add(new(Convert.ToSingle(data[i][2]), Convert.ToSingle(data[i][3]), Convert.ToSingle(data[i][4])));
                    pseudoNormalList.Add(new(Convert.ToSingle(data[i][5]), Convert.ToSingle(data[i][6]), Convert.ToSingle(data[i][7])));
                    uvList.Add(new(Convert.ToSingle(data[i][9]), Convert.ToSingle(data[i][10])));
                }
                if (data[i][0] == "Face")
                {
                    faceList.Add(new[] { Convert.ToInt32(data[i][3]), Convert.ToInt32(data[i][4]), Convert.ToInt32(data[i][5]) });
                }
            }
            vertices = vertexList.ToArray();
            pseudoNormals = pseudoNormalList.ToArray();
            UVs = uvList.ToArray();
            faces = new int[faceList.Count, 3];
            for (int i = 0; i < faceList.Count; ++i) for (int j = 0; j < 3; ++j) faces[i, j] = faceList[i][j];
        }
    }
}
