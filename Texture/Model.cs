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
    /// 面。頂点番号の組み合わせと材質を持つ。MMDではFaceと呼ぶ。
    /// </summary>
    internal class Face
    {
        public int[] vertexNumbers;
        public Material material;

        public Face(int[] vertexNumbers, Material material)
        {
            this.vertexNumbers = vertexNumbers;
            this.material = material;
        }
    }

    /// <summary>
    /// 材質。色とテクスチャを持つ。
    /// </summary>
    internal class Material
    {
        public String name;
        public Color color;
        public Bitmap texture;

        public Material(String name, Color color, Bitmap texture)
        {
            this.name = name;
            this.color = color;
            this.texture = texture;
        }
    }

    internal class Model
    {
        public Vertex[] vertices;
        public Material[] materials;
        public Face[] faces;

        /// <param name="filename">MMDモデル用のPMXファイルをPMXエディターで開いてCSV出力したファイルの名前</param>
        public Model(String filename)
        {
            var lines = File.ReadAllLines(filename);
            var data = new String[lines.Length][];
            for (int i = 0; i < lines.Length; i++) data[i] = lines[i].Split(',');
            var vertexList = new List<Vertex>();
            var faceList = new List<Face>();
            var materialList = new List<Material>();
            for (int i = 0; i < lines.Length; i++)
            {
                if (data[i][0] == "Vertex")
                {
                    var pos = new float[3];
                    for (int j = 0; j < pos.Length; ++j) pos[j] = Convert.ToSingle(data[i][2 + j]);
                    var pseudoNormal = new float[3];
                    for (int j = 0; j < pseudoNormal.Length; ++j) pseudoNormal[j] = Convert.ToSingle(data[i][5 + j]);
                    var uv = new float[2];
                    for (int j = 0; j < uv.Length; ++j) uv[j] = Convert.ToSingle(data[i][9 + j]);
                    vertexList.Add(new(new Vector3(pos), new Vector3(pseudoNormal), new Vector2(uv)));
                }
                if (data[i][0] == "Material")
                {
                    var rgb = new int[3];
                    for (int j = 0; j < rgb.Length; ++j) rgb[j] = (int)(255 * Convert.ToSingle(data[i][3 + j]));
                    var textureFilename = data[i][26];
                    var texture = new Bitmap(Path.GetDirectoryName(filename) + "/" + textureFilename);  // テクスチャファイルはbmp,  png, jpg とし、モデルのファイルと同じフォルダにあるとする
                    materialList.Add(new(data[i][1], Color.FromArgb(rgb[0], rgb[1], rgb[2]), texture));
                }
            }
            vertices = vertexList.ToArray();
            materials = materialList.ToArray();
            for (int i = 0; i < lines.Length; i++)
            {
                if (data[i][0] == "Face")
                {
                    var vertexNumbers = new int[3];
                    for (int j = 0; j < vertexNumbers.Length; ++j) vertexNumbers[j] = Convert.ToInt32(data[i][3 + j]);
                    var material = materials.First(x => x.name == data[i][1]);
                    faceList.Add(new Face(vertexNumbers, material));
                }
            }
            faces = faceList.ToArray();
        }
    }
}
