using UnityEngine;
using System.Collections.Generic;
using DataRepository;
using System.Linq;
using JigsawSystem; 


namespace JigsawSystem
{
    public class JigsawPuzzleManager : MonoBehaviour
    {
        private static JigsawPuzzleManager _instance;
        public static JigsawPuzzleManager Instance => _instance;

        [Header("Configuration")]
        [SerializeField] private JigsawPuzzlesConfig config;

        public JigsawPuzzlesConfig Config => config;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Awards a puzzle piece to the player.
        /// If pieceId is "random", awards a random piece that the player doesn't already have.
        /// If the specified pieceId is already owned by the player, awards a random missing piece instead.
        /// Returns the ID of the awarded piece (puzzleId_index)
        /// </summary>
        public string AwardPiece(string pieceId)
        {
            var saveData = ProgressSaveManager<SaveData>.Instance.GetGameData();
            
            string awardedId = pieceId;

            if (pieceId == "random")
            {
                awardedId = GetRandomMissingPiece();
            }
            else
            {
                // If user already has this specific piece, give them a random one instead
                if (saveData.CollectedPieces.Contains(pieceId))
                {
                    awardedId = GetRandomMissingPiece();
                    Debug.Log($"Player already has piece {pieceId}, awarding random piece instead: {awardedId}");
                }
            }

            if (string.IsNullOrEmpty(awardedId)) return null;

            if (!saveData.CollectedPieces.Contains(awardedId))
            {
                saveData.CollectedPieces.Add(awardedId);
                ProgressSaveManager<SaveData>.Instance.SaveGameData();
                Debug.Log($"Awarded piece: {awardedId}");
            }

            return awardedId;
        }

        private string GetRandomMissingPiece()
        {
            if (config == null) return null;

            var saveData = ProgressSaveManager<SaveData>.Instance.GetGameData();
            List<string> allPossiblePieces = new List<string>();

            foreach (var puzzle in config.Puzzles)
            {
                for (int i = 0; i < 9; i++)
                {
                    string id = $"{puzzle.puzzleId}_{i}";
                    if (!saveData.CollectedPieces.Contains(id))
                    {
                        allPossiblePieces.Add(id);
                    }
                }
            }

            if (allPossiblePieces.Count == 0) return null;

            return allPossiblePieces[Random.Range(0, allPossiblePieces.Count)];
        }

        public int GetCollectedPieceCount(string puzzleId)
        {
            var saveData = ProgressSaveManager<SaveData>.Instance.GetGameData();
            return saveData.CollectedPieces.Count(p => p.StartsWith(puzzleId + "_"));
        }

        public bool IsPuzzleSolved(string puzzleId)
        {
            var saveData = ProgressSaveManager<SaveData>.Instance.GetGameData();
            return saveData.SolvedPuzzles.Contains(puzzleId);
        }

        public void MarkPuzzleSolved(string puzzleId)
        {
            var saveData = ProgressSaveManager<SaveData>.Instance.GetGameData();
            if (!saveData.SolvedPuzzles.Contains(puzzleId))
            {
                saveData.SolvedPuzzles.Add(puzzleId);
                
                // Award money reward
                var puzzleData = config.GetPuzzle(puzzleId);
                if (puzzleData != null)
                {
                    ProgressSaveManager<SaveData>.Instance.AddCoins(puzzleData.completionReward);
                }
                
                ProgressSaveManager<SaveData>.Instance.SaveGameData();
            }
        }

        public PuzzleProgressData GetPuzzleProgress(string puzzleId)
        {
            var saveData = ProgressSaveManager<SaveData>.Instance.GetGameData();
            var progress = saveData.PuzzleProgress.FirstOrDefault(p => p.PuzzleId == puzzleId);
            
            if (progress == null)
            {
                progress = new PuzzleProgressData(puzzleId);
                saveData.PuzzleProgress.Add(progress);
            }
            
            return progress;
        }

        public void SavePuzzleProgress(string puzzleId, int[] placedPieces)
        {
            var progress = GetPuzzleProgress(puzzleId);
            System.Array.Copy(placedPieces, progress.PlacedPieceIndices, 9);
            ProgressSaveManager<SaveData>.Instance.SaveGameData();
        }
        
        public List<string> GetUnplacedCollectedPieces(string puzzleId)
        {
            var saveData = ProgressSaveManager<SaveData>.Instance.GetGameData();
            var progress = GetPuzzleProgress(puzzleId);
            
            List<string> unplaced = new List<string>();
            
            // Find all collected pieces for this puzzle
            var collectedForPuzzle = saveData.CollectedPieces
                .Where(p => p.StartsWith(puzzleId + "_"))
                .Select(p => int.Parse(p.Split('_')[1]))
                .ToList();
                
            // Filter out those already placed in the grid
            foreach (int pieceIndex in collectedForPuzzle)
            {
                bool isPlaced = false;
                for (int i = 0; i < 9; i++)
                {
                    if (progress.PlacedPieceIndices[i] == pieceIndex)
                    {
                        isPlaced = true;
                        break;
                    }
                }
                
                if (!isPlaced)
                {
                    unplaced.Add($"{puzzleId}_{pieceIndex}");
                }
            }
            
            return unplaced;
        }
    }
}
