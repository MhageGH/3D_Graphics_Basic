using System.Diagnostics;
using System.Drawing.Imaging;
using System.Numerics;
using System.Text;

namespace SkinMeshAnimation
{
    /// <summary>
    /// 各種情報を含む頂点
    /// </summary>
    internal class Vertex
    {
        public Vector3 pos;             // 位置
        public Vector3 pseudoNormal;    // 疑似法線ベクトル
        public Vector2 uv;              // UV座標

        public Vertex() { }

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

        public Face() { }

        public Face(int[] vertexNumbers, Material material)
        {
            this.vertexNumbers = vertexNumbers;
            this.material = material;
        }
    }

    internal class Texture
    {
        public byte[] bytes;
        public int width, height, stride;
        
        public Texture() { }

        public Texture(byte[] bytes, int width, int height, int stride)
        {
            this.bytes = bytes;
            this.width = width;
            this.height = height;
            this.stride = stride;
        }
    }

    /// <summary>
    /// 材質。色とテクスチャを持つ。
    /// </summary>
    internal class Material
    {
        public String name;
        public Color color;
        public Texture? texture = null;

        public Material() { }

        public Material(String name, Color color, Texture? texture)
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
            // C#はデフォルトでShift_JISに非対応。NuGetパッケージでSystem.Text.Encoding.CodePagesをインストールし、以下のメソッド実行でShift_JISを使用可能にする。
            // 日本のWindows環境のCSVはShift_JISがデフォルト。PMX EditorのCSV出力もShift_JISが使用されている。
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var lines = File.ReadAllLines(filename, System.Text.Encoding.GetEncoding("Shift_JIS"));
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
                    pos[1] *= -1; pseudoNormal[1] *= -1;    // MMDモデルはY軸が反転しているので反転する
                    vertexList.Add(new(new Vector3(pos), new Vector3(pseudoNormal), new Vector2(uv)));
                }
                if (data[i][0] == "Material")
                {
                    var rgba = new int[4];
                    for (int j = 0; j < rgba.Length; ++j) rgba[j] = (int)(255 * Convert.ToSingle(data[i][3 + j]));
                    var textureFilename = data[i][26].Trim('\"');   // PMX EditorのCSV出力は文字列がダブルクォーテーションで囲われている。Excelなどで編集して保存するとダブルクォーテーションがなくなる。
                    var bitmap = textureFilename == "" ? null : new Bitmap(Path.GetDirectoryName(filename) + "/" + textureFilename);  // テクスチャファイルはbmp, png, jpgに対応。tgaは非対応。モデルのファイルと同じフォルダにあるとする
                    var material = new Material(data[i][1], Color.FromArgb(rgba[3], rgba[0], rgba[1], rgba[2]), null);
                    if (bitmap != null)
                    {
                        int width = bitmap.Width, height = bitmap.Height;
                        var bmpData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                        var stride = bmpData.Stride;
                        var bytes = new byte[stride * height];
                        System.Runtime.InteropServices.Marshal.Copy(bytes, 0, bmpData.Scan0, bytes.Length);
                        bitmap.UnlockBits(bmpData);
                        material.texture = new Texture(bytes, width, height, stride);
                    }
                    materialList.Add(material);
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

        public Model(String filename, bool pmx)
        {
            if (!pmx) return;
            using (var reader = new BinaryReader(new FileStream(filename, FileMode.Open))) // BinaryReaderクラスとPMXデータフォーマットは共にリトルエンディアン
            {
                // ヘッダ
                var headerStr = Encoding.ASCII.GetString(reader.ReadBytes(4));
                Debug.WriteLine("headerStr : " + headerStr);
                var ver = reader.ReadSingle();
                Debug.WriteLine("ver : " + ver);        // 2.0 または 2.1 でなければならない
                var headerInfoSize = reader.ReadByte();
                Debug.WriteLine("headerInfoSize : " + headerInfoSize); // 8でなければならない
                var encodeTypeNumber = reader.ReadByte();
                var encoding = encodeTypeNumber == 0 ? Encoding.Unicode : Encoding.UTF8;
                Debug.WriteLine("encoding : " + encoding);
                var numberOfAdditionalUV = reader.ReadByte();
                Debug.WriteLine("numberOfAdditionalUV : " + numberOfAdditionalUV);
                var vertexIndexSize = reader.ReadByte();
                Debug.WriteLine("vertexIndexSize : " + vertexIndexSize);
                var textureIndexSize = reader.ReadByte();
                Debug.WriteLine("textureIndexSize : " + textureIndexSize);
                var materialIndexSize = reader.ReadByte();
                Debug.WriteLine("materialIndexSize : " + materialIndexSize);
                var boneIndexSize = reader.ReadByte();
                Debug.WriteLine("boneIndexSize : " + boneIndexSize);
                var morphIndexSize = reader.ReadByte();
                Debug.WriteLine("morphIndexSize : " + morphIndexSize);
                var rigidBodyIndexSize = reader.ReadByte();
                Debug.WriteLine("rigidBodyIndexSize : " + rigidBodyIndexSize);
                var sizeOfModelName = reader.ReadInt32();
                var modelName = encoding.GetString(reader.ReadBytes(sizeOfModelName));
                Debug.WriteLine("modelName : " + modelName);
                var sizeOfEnglishModelName = reader.ReadInt32();
                var EnglishModelName = encoding.GetString(reader.ReadBytes(sizeOfEnglishModelName));
                Debug.WriteLine("EnglishModelName : " + EnglishModelName);
                var sizeOfComment = reader.ReadInt32();
                var comment = encoding.GetString(reader.ReadBytes(sizeOfComment));
                Debug.WriteLine("comment : " + comment);
                var sizeOfEnglishComment = reader.ReadInt32();
                var EnglishComment = encoding.GetString(reader.ReadBytes(sizeOfEnglishComment));
                Debug.WriteLine("EnglishComment : " + EnglishComment);

                // 頂点
                var numberOfVertex = reader.ReadInt32();
                Debug.WriteLine("sizeOfVertex : " + numberOfVertex);
                vertices = new Vertex[numberOfVertex];
                for (int i = 0; i < numberOfVertex; i++)
                {
                    vertices[i] = new Vertex();
                    vertices[i].pos.X = reader.ReadSingle();
                    vertices[i].pos.Y = -reader.ReadSingle();           // MMDモデルはY軸が反転しているので反転する
                    vertices[i].pos.Z = reader.ReadSingle();
                    vertices[i].pseudoNormal.X = reader.ReadSingle();
                    vertices[i].pseudoNormal.Y = -reader.ReadSingle();  // MMDモデルはY軸が反転しているので反転する
                    vertices[i].pseudoNormal.Z = reader.ReadSingle();
                    vertices[i].uv.X = reader.ReadSingle();
                    vertices[i].uv.Y = reader.ReadSingle();
                    reader.ReadBytes(16 * numberOfAdditionalUV);    // 使用しない
                    var weightTransformType = reader.ReadByte();
                    switch (weightTransformType)
                    {
                        // 全て使用しない
                        case 0: // BDEF1
                            reader.ReadBytes(boneIndexSize);
                            break;
                        case 1: // BDEF2
                            reader.ReadBytes(2 * boneIndexSize + 4);
                            break;
                        case 2: // BDEF4
                            reader.ReadBytes(4 * boneIndexSize + 4 * 4);
                            break;
                        case 3: // SDEF
                            reader.ReadBytes(2 * boneIndexSize + 4 + 3 * 12);
                            break;
                    }
                    reader.ReadSingle();    // 使用しない
                }
                Debug.WriteLine("Last Vertex pos X: " + vertices[numberOfVertex - 1].pos.X);

                // 面
                var numberOfFace = reader.ReadInt32() / 3;  // 注意：PMX仕様.txtの「面数」は面の数ではなく面を構成している頂点の数を表す。そのため3で割る必要がある。
                Debug.WriteLine("numberOfFace : " + numberOfFace);
                faces = new Face[numberOfFace];
                for (int i = 0; i < numberOfFace; i++)
                {
                    faces[i] = new Face();
                    faces[i].vertexNumbers = new int[3];
                    switch (vertexIndexSize)
                    {
                        case 1:
                            for (int j = 0; j < 3; ++j) faces[i].vertexNumbers[j] = reader.ReadByte();  // 符号無し 8bit
                            break;
                        case 2:
                            for (int j = 0; j < 3; ++j) faces[i].vertexNumbers[j] = reader.ReadUInt16();// 符号無し 16bit
                            break;
                        case 4:
                            for (int j = 0; j < 3; ++j) faces[i].vertexNumbers[j] = reader.ReadInt32(); // 符号あり 32bit
                            break;
                    }
                }

                // テクスチャ
                var numberOfTexture = reader.ReadInt32();
                Debug.WriteLine("numberOfTexture : " + numberOfTexture);
                var texturePaths = new String[numberOfTexture];
                for (int i = 0; i < numberOfTexture; i++)
                {
                    var sizeOfTexturePath = reader.ReadInt32();
                    texturePaths[i] = encoding.GetString(reader.ReadBytes(sizeOfTexturePath));
                }

                // 材質
                var numberOfMaterial = reader.ReadInt32();
                Debug.WriteLine("numberOfMaterial : " + numberOfMaterial);
                materials = new Material[numberOfMaterial];
                int faceIndex = 0;
                for (int i = 0; i < numberOfMaterial; i++)
                {
                    materials[i] = new Material();
                    var sizeOfMaterialName = reader.ReadInt32();
                    materials[i].name = encoding.GetString(reader.ReadBytes(sizeOfMaterialName));
                    var sizeOfEnglishMaterialName = reader.ReadInt32();
                    encoding.GetString(reader.ReadBytes(sizeOfEnglishMaterialName));    // 使用しない
                    var rgba = new int[4];
                    for (int j = 0; j < rgba.Length; ++j) rgba[j] = (int)(255 * reader.ReadSingle());
                    materials[i].color = Color.FromArgb(rgba[3], rgba[0], rgba[1], rgba[2]);
                    reader.ReadBytes(12 + 4 + 12 + 1 + 16 + 4);  // 使用しない
                    int textureIndex = 0;
                    switch (textureIndexSize)
                    {
                        case 1:
                            textureIndex = reader.ReadSByte(); // 符号あり 8bit
                            break;
                        case 2:
                            textureIndex = reader.ReadInt16(); // 符号あり 16bit
                            break;
                        case 4:
                            textureIndex = reader.ReadInt32(); // 符号あり 32bit
                            break;
                    }
                    var bitmap = textureIndex == -1 ? null : new Bitmap(Path.GetDirectoryName(filename) + "/" + texturePaths[textureIndex]);
                    if (bitmap != null)
                    {
                        int width = bitmap.Width, height = bitmap.Height;
                        var bmpData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                        var stride = bmpData.Stride;
                        var bytes = new byte[stride * height];
                        System.Runtime.InteropServices.Marshal.Copy(bmpData.Scan0, bytes, 0, bytes.Length);
                        bitmap.UnlockBits(bmpData);
                        materials[i].texture = new Texture(bytes, width, height, stride);
                    }
                    reader.ReadBytes(textureIndexSize + 1); // 使用しない
                    var commonToonFlag = reader.ReadByte();
                    if (commonToonFlag == 0) reader.ReadBytes(textureIndexSize);    // どちらも使用しない
                    else reader.ReadByte();
                    var sizeOfMemo = reader.ReadInt32();
                    reader.ReadBytes(sizeOfMemo);       // 使用しない
                    var n = reader.ReadInt32() / 3;     // 材質に対応する面数
                    for (int j = 0; j < n; j++)
                    {
                        faces[faceIndex].material = materials[i];
                        faceIndex++;
                    }
                }
            }
        }
    }
}
