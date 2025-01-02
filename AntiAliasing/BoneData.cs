namespace AntiAliasing
{
	internal class BoneData
	{
		public Matrix[][] matrices;
		public BoneData()     // SkinMeshAnimationプロジェクトから参照する
        {
			var boneData = new SkinMeshAnimation.BoneData();
            matrices = new Matrix[boneData.matrices.GetLength(0)][];
            for (int i = 0; i < boneData.matrices.GetLength(0); ++i)
			{
                matrices[i] = new Matrix[boneData.matrices[i].GetLength(0)];
                for (int j = 0; j < boneData.matrices[i].GetLength(0); ++j)
				{
					matrices[i][j] = new Matrix(
                        boneData.matrices[i][j].elements[0, 0], boneData.matrices[i][j].elements[0, 1], boneData.matrices[i][j].elements[0, 2], boneData.matrices[i][j].elements[0, 3],
                        boneData.matrices[i][j].elements[1, 0], boneData.matrices[i][j].elements[1, 1], boneData.matrices[i][j].elements[1, 2], boneData.matrices[i][j].elements[1, 3],
                        boneData.matrices[i][j].elements[2, 0], boneData.matrices[i][j].elements[2, 1], boneData.matrices[i][j].elements[2, 2], boneData.matrices[i][j].elements[2, 3],
                        boneData.matrices[i][j].elements[3, 0], boneData.matrices[i][j].elements[3, 1], boneData.matrices[i][j].elements[3, 2], boneData.matrices[i][j].elements[3, 3]
                    );
                }
			}
		}
	}
}