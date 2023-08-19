using System.Numerics;

namespace Texture
{
    /// <summary>
    /// 各種情報を含む頂点
    /// </summary>
    internal class Vertex
    {
        public Vector3 pos;             // 位置
        public Vector3 pseudoNormal;    // 疑似法線ベクトル
        public Vector2 uv;              // UV座標

        public Vertex(Vector3 pos, Vector3? pseudoNormal, Vector2? uv)
        {
            this.pos = pos;
            if (pseudoNormal != null) this.pseudoNormal = (Vector3)pseudoNormal;
            if (uv != null) this.uv = (Vector2)uv;
        }
    }

    /// <summary>
    /// 面。頂点番号の組み合わせ。MMDではFaceと呼ぶ。
    /// </summary>
    internal class Face
    {
        public int[] vertexNumbers;

        public Face(int[] vertexNumbers)
        {
            this.vertexNumbers = vertexNumbers;
        }
    }

    internal class Model
    {
        public Vertex[] vertices;
        public Face[] faces;

        /// <param name="filename">MMDモデル用のPMXファイルをPMXエディターで開いてCSV出力したファイルの名前</param>
        public Model(String filename)
        {
            var lines = File.ReadAllLines(filename);
            var data = new String[lines.Length][];
            for (int i = 0; i < lines.Length; i++) data[i] = lines[i].Split(',');
            var posList = new List<Vector3>();
            var pseudoNormalList = new List<Vector3>();
            var uvList = new List<Vector2>();
            var vertexNumbersList = new List<int[]>();
            for (int i = 0; i < lines.Length; i++)
            {
                if (data[i][0] == "Vertex")
                {
                    posList.Add(new(Convert.ToSingle(data[i][2]), Convert.ToSingle(data[i][3]), Convert.ToSingle(data[i][4])));
                    pseudoNormalList.Add(new(Convert.ToSingle(data[i][5]), Convert.ToSingle(data[i][6]), Convert.ToSingle(data[i][7])));
                    uvList.Add(new(Convert.ToSingle(data[i][9]), Convert.ToSingle(data[i][10])));
                }
                if (data[i][0] == "Face")
                {
                    vertexNumbersList.Add(new[] { Convert.ToInt32(data[i][3]), Convert.ToInt32(data[i][4]), Convert.ToInt32(data[i][5]) });
                }
            }
            vertices = new Vertex[posList.Count];
            for (int i = 0; i < vertices.Length; ++i) vertices[i] = new Vertex(posList[i], pseudoNormalList[i], uvList[i]);
            faces = new Face[vertexNumbersList.Count];
            for (int i = 0; i < vertexNumbersList.Count; ++i) faces[i] = new Face(vertexNumbersList[i]);
        }
    }
}
