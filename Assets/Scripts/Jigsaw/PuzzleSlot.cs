using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace JigsawSystem
{
public class PuzzleSlot : MonoBehaviour, IDropHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public int SlotIndex; // 0-8
    public bool HasPiece => solveUI.IsSlotOccupied(SlotIndex);
    
    private PuzzleSolveUI solveUI;

    public void Initialize(int index, PuzzleSolveUI ui)
    {
        SlotIndex = index;
        solveUI = ui;
    }

    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log("OnDrop: " + HasPiece);
        // Prevent dropping if puzzle is already solved
        if (solveUI.IsPuzzleSolved())
        {
            return;
        }
        
        if (eventData.pointerDrag != null)
        {
            PuzzlePieceItem piece = eventData.pointerDrag.GetComponent<PuzzlePieceItem>();
            if (piece != null)
            {
                solveUI.OnPieceDroppedOnSlot(piece, this);
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log("OnBeginDrag: " + HasPiece);
        if (HasPiece)
        {
            solveUI.OnStartDraggingFromSlot(this, eventData);
        }
    }

    public void OnDrag(PointerEventData eventData) { }
    public void OnEndDrag(PointerEventData eventData) { }
}
}
