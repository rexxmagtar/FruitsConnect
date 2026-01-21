using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;
using DataRepository;
using TMPro;

namespace JigsawSystem
{
    public class PuzzleSolveUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private PuzzleSlot[] gridSlots = new PuzzleSlot[9];
        [SerializeField] private Image[] placeholderImages = new Image[9];
        [SerializeField] private RectTransform storageParent;
        [SerializeField] private RectTransform dragLayer;
        [SerializeField] private Button backButton;
        [SerializeField] private Canvas mainCanvas;
        [SerializeField] private TextMeshProUGUI puzzleNameText;
        
        [Header("Prefabs")]
        [SerializeField] private PuzzlePieceItem piecePrefab;

        [Header("Solve Popup")]
        [SerializeField] private PuzzleSolveUiPopup solvePopup;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip dragStartSound;
        [SerializeField] private AudioClip placeInSlotSound;
        [SerializeField] private AudioClip placeInStorageSound;
        [SerializeField] private AudioClip buttonClickSound;

        private JigsawPuzzleData currentPuzzle;
        private List<PuzzlePieceItem> activePieces = new List<PuzzlePieceItem>();
        private int[] slotPieceIndices = new int[9]; // Stores piece index at each slot
        
        public RectTransform DragLayer => dragLayer;
        public RectTransform StorageParent => storageParent;
        public float CanvasScale => mainCanvas.scaleFactor;

        private void Awake()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            if (backButton != null)
            {
                backButton.onClick.AddListener(Close);
            }

            for (int i = 0; i < gridSlots.Length; i++)
            {
                gridSlots[i].Initialize(i, this);
                slotPieceIndices[i] = -1;
            }
        }

        public void Open(JigsawPuzzleData data)
        {
            currentPuzzle = data;
            gameObject.SetActive(true);
            
            // Set puzzle display name
            if (puzzleNameText != null && !string.IsNullOrEmpty(data.displayName))
            {
                puzzleNameText.text = data.displayName;
            }
            
            Refresh();
        }

        public void Close()
        {
            if (audioSource != null && buttonClickSound != null)
            {
                audioSource.PlayOneShot(buttonClickSound);
            }
            SaveProgress();
            gameObject.SetActive(false);
        }

        public bool IsSlotOccupied(int slotIndex) => slotPieceIndices[slotIndex] != -1;
        
        public bool IsPuzzleSolved()
        {
            if (currentPuzzle == null) return false;
            return JigsawPuzzleManager.Instance.IsPuzzleSolved(currentPuzzle.puzzleId);
        }

        private void Refresh()
        {
            // 1. Clear everything
            foreach (var p in activePieces) if (p != null) Destroy(p.gameObject);
            activePieces.Clear();
            
            for (int i = 0; i < 9; i++)
            {
                slotPieceIndices[i] = -1;
                if (placeholderImages[i] != null) placeholderImages[i].enabled = false;
            }

            var saveData = ProgressSaveManager<SaveData>.Instance.GetGameData();
            var progress = JigsawPuzzleManager.Instance.GetPuzzleProgress(currentPuzzle.puzzleId);
            
            // 2. Identify all collected pieces for this puzzle
            var collectedIndices = saveData.CollectedPieces
                .Where(p => p.StartsWith(currentPuzzle.puzzleId + "_"))
                .Select(p => int.Parse(p.Split('_')[1]))
                .ToList();

            // 3. Create exactly one permanent storage item for every collected piece
            foreach (int pieceIndex in collectedIndices)
            {
                var storageItem = Instantiate(piecePrefab, storageParent);
                storageItem.Initialize(currentPuzzle.puzzleId, pieceIndex, currentPuzzle.pieces[pieceIndex], this);
                activePieces.Add(storageItem);
                
                // 4. If this piece is already placed in a slot, hide it in storage and show in slot
                bool isPlaced = false;
                for (int i = 0; i < 9; i++)
                {
                    if (progress.PlacedPieceIndices[i] == pieceIndex)
                    {
                        SetSlotPiece(i, pieceIndex);
                        storageItem.SetVisible(false);
                        isPlaced = true;
                        break;
                    }
                }
                
                if (!isPlaced) storageItem.SetVisible(true);
            }
        }

        private void CreatePieceInStorage(int index)
        {
            // Not used anymore in favor of one-time creation in Refresh
        }

        public void OnPieceDragStart(PuzzlePieceItem piece)
        {
            // Dragging handled via OnStartDraggingFromStorage / OnStartDraggingFromSlot
        }

        public void OnPieceDragEnd(PuzzlePieceItem dragPiece)
        {
            // If it didn't land in a slot, restore its "home" in storage
            if (!IsPieceInAnySlot(dragPiece))
            {
                var home = activePieces.FirstOrDefault(p => p.PieceIndex == dragPiece.PieceIndex);
                if (home != null) home.SetVisible(true);

                if (audioSource != null && placeInStorageSound != null)
                {
                    audioSource.PlayOneShot(placeInStorageSound);
                }
            }
            
            // Drag piece is temporary and always destroyed
            Destroy(dragPiece.gameObject);
            
            SaveProgress();
            CheckWin();
        }

        private bool IsPieceInAnySlot(PuzzlePieceItem piece)
        {
            for (int i = 0; i < 9; i++)
            {
                if (slotPieceIndices[i] == piece.PieceIndex) return true;
            }
            return false;
        }

        public void OnPieceDroppedOnSlot(PuzzlePieceItem dragPiece, PuzzleSlot slot)
        {
            // Prevent dropping if puzzle is already solved
            if (IsPuzzleSolved())
            {
                return;
            }
            
            int slotIndex = slot.SlotIndex;
            int newPieceIndex = dragPiece.PieceIndex;

            if (audioSource != null && placeInSlotSound != null)
            {
                audioSource.PlayOneShot(placeInSlotSound);
            }

            // Handle Swap: If slot was occupied, find that piece's storage home and show it
            if (slotPieceIndices[slotIndex] != -1)
            {
                int oldPieceIndex = slotPieceIndices[slotIndex];
                var oldHome = activePieces.FirstOrDefault(p => p.PieceIndex == oldPieceIndex);
                if (oldHome != null) oldHome.SetVisible(true);
            }

            // Set new piece in slot
            SetSlotPiece(slotIndex, newPieceIndex);
            
            // Find this piece's storage home and hide it
            var newHome = activePieces.FirstOrDefault(p => p.PieceIndex == newPieceIndex);
            if (newHome != null) newHome.SetVisible(false);

            // Note: dragPiece will be destroyed in OnPieceDragEnd which is called right after this
        }

        public void OnStartDraggingFromStorage(PuzzlePieceItem storagePiece, PointerEventData eventData)
        {
            // Prevent dragging if puzzle is already solved
            if (JigsawPuzzleManager.Instance.IsPuzzleSolved(currentPuzzle.puzzleId))
            {
                return;
            }
            
            if (audioSource != null && dragStartSound != null)
            {
                audioSource.PlayOneShot(dragStartSound);
            }

            // Hide the permanent piece in the grid
            storagePiece.SetVisible(false);
            
            // Spawn a temporary visual piece for dragging
            var dragPiece = Instantiate(piecePrefab, DragLayer);
            dragPiece.Initialize(currentPuzzle.puzzleId, storagePiece.PieceIndex, currentPuzzle.pieces[storagePiece.PieceIndex], this);
            dragPiece.LinkedStoragePiece = storagePiece;
            
            // Sync position to cursor
            dragPiece.transform.position = eventData.position;
            
            // Transfer drag control to the temporary piece
            eventData.pointerDrag = dragPiece.gameObject;
            dragPiece.OnBeginDrag(eventData);
        }

        public void OnStartDraggingFromSlot(PuzzleSlot slot, PointerEventData eventData)
        {
            // Prevent dragging if puzzle is already solved
            if (JigsawPuzzleManager.Instance.IsPuzzleSolved(currentPuzzle.puzzleId))
            {
                return;
            }
            
            if (audioSource != null && dragStartSound != null)
            {
                audioSource.PlayOneShot(dragStartSound);
            }

            int slotIndex = slot.SlotIndex;
            int pieceIndex = slotPieceIndices[slotIndex];
            
            // Deactivate placeholder
            placeholderImages[slotIndex].enabled = false;
            slotPieceIndices[slotIndex] = -1;
            
            // Spawn a temporary visual piece for dragging
            var dragPiece = Instantiate(piecePrefab, DragLayer);
            dragPiece.Initialize(currentPuzzle.puzzleId, pieceIndex, currentPuzzle.pieces[pieceIndex], this);
            
            // Sync position to cursor
            dragPiece.transform.position = eventData.position;
            
            // Transfer drag control
            eventData.pointerDrag = dragPiece.gameObject;
            dragPiece.OnBeginDrag(eventData);
        }

        private void SetSlotPiece(int slotIndex, int pieceIndex)
        {
            slotPieceIndices[slotIndex] = pieceIndex;
            if (placeholderImages[slotIndex] != null)
            {
                placeholderImages[slotIndex].sprite = currentPuzzle.pieces[pieceIndex];
                placeholderImages[slotIndex].enabled = true;
                placeholderImages[slotIndex].raycastTarget = false;
            }
        }

        private void SaveProgress()
        {
            JigsawPuzzleManager.Instance.SavePuzzleProgress(currentPuzzle.puzzleId, slotPieceIndices);
        }

        private void CheckWin()
        {
            if (JigsawPuzzleManager.Instance.IsPuzzleSolved(currentPuzzle.puzzleId)) return;

            bool allCorrect = true;
            for (int i = 0; i < 9; i++)
            {
                if (slotPieceIndices[i] != i)
                {
                    allCorrect = false;
                    break;
                }
            }

            if (allCorrect)
            {
                JigsawPuzzleManager.Instance.MarkPuzzleSolved(currentPuzzle.puzzleId);
                if (solvePopup != null)
                {
                    solvePopup.Show(currentPuzzle);
                }
            }
        }
    }
}
