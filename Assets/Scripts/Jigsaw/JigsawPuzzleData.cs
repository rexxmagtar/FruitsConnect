using UnityEngine;

namespace JigsawSystem
{
    [CreateAssetMenu(fileName = "JigsawPuzzleData", menuName = "Fruit Connect/Jigsaw/Puzzle Data")]
    public class JigsawPuzzleData : ScriptableObject
    {
        [Header("Basic Info")]
        public string puzzleId;
        public Sprite fullImage;
        public int completionReward = 100;

        [Header("Pieces (3x3 - 9 pieces total)")]
        [Tooltip("Order: Row 0 (top) [0,1,2], Row 1 [3,4,5], Row 2 [6,7,8]")]
        public Sprite[] pieces = new Sprite[9];

        /// <summary>
        /// Validates the puzzle data
        /// </summary>
        private void OnValidate()
        {
            if (pieces.Length != 9)
            {
                System.Array.Resize(ref pieces, 9);
            }
        }
    }
}
