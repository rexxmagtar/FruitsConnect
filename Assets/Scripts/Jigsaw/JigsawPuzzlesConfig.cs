using UnityEngine;
using System.Collections.Generic;

namespace JigsawSystem
{
    [CreateAssetMenu(fileName = "JigsawPuzzlesConfig", menuName = "Fruit Connect/Jigsaw/Puzzles Config")]
    public class JigsawPuzzlesConfig : ScriptableObject
    {
        [Header("All Puzzles")]
        [SerializeField] private List<JigsawPuzzleData> puzzles = new List<JigsawPuzzleData>();

        public List<JigsawPuzzleData> Puzzles => puzzles;

        /// <summary>
        /// Gets a puzzle by its ID
        /// </summary>
        public JigsawPuzzleData GetPuzzle(string id)
        {
            return puzzles.Find(p => p.puzzleId == id);
        }
    }
}
