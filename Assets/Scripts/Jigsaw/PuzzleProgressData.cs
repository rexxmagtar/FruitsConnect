namespace JigsawSystem
{
    [System.Serializable]
    public class PuzzleProgressData
    {
        public string PuzzleId;
        public int[] PlacedPieceIndices = new int[9]; // -1 means empty, otherwise 0-8

        public PuzzleProgressData() { }
        public PuzzleProgressData(string id)
        {
            PuzzleId = id;
            for (int i = 0; i < 9; i++) PlacedPieceIndices[i] = -1;
        }
    }
}
