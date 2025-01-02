namespace AntiAliasing
{
	internal class BoneData
	{
		public Matrix[][] matrices;

        public BoneData(string filename)
        {
            var mss = new List<Matrix[]>();
            var ms = new List<Matrix>();
            var lines = File.ReadAllLines(filename);
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("[")) ms.Clear();
                else if (lines[i].Contains("]")) mss.Add(ms.ToArray());
                else if (lines[i].Contains("("))
                {
                    lines[i] = lines[i].Replace("(", "").Replace(")", "");
                    var words = lines[i].Split(',');
                    var f = new float[16];
                    for (int j = 0; j < f.Length; ++j) f[j] = Convert.ToSingle(words[j]);
                    ms.Add(new Matrix(f[0], f[1], f[2], f[3], f[4], f[5], f[6], f[7], f[8], f[9], f[10], f[11], f[12], f[13], f[14], f[15]));
                }
            }
            matrices = mss.ToArray();
        }
    }
}