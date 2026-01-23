using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace JigsawSystem
{
    public class PuzzlePieceItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
    [SerializeField] private Image image;
    [SerializeField] private float dragScale = 1.2f;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Transform originalParent;
    private Vector2 originalPosition;
    
    public int PieceIndex { get; private set; }
    public string PuzzleId { get; private set; }
    
    // Reference to the slot it's currently in, if any
    public PuzzleSlot CurrentSlot { get; set; }
    
    // Reference to the piece staying in storage while this one is dragged
    public PuzzlePieceItem LinkedStoragePiece { get; set; }
    
    private PuzzleSolveUI solveUI;

    public void Initialize(string puzzleId, int pieceIndex, Sprite sprite, PuzzleSolveUI ui)
    {
        PuzzleId = puzzleId;
        PieceIndex = pieceIndex;
        image.sprite = sprite;
        solveUI = ui;
        
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        
        rectTransform = GetComponent<RectTransform>();

        // Ensure we have a LayoutElement and set default state
        SetIgnoreLayout(false);
    }

    public void SetIgnoreLayout(bool ignore)
    {
        LayoutElement layout = GetComponent<LayoutElement>();
        if (layout == null) layout = gameObject.AddComponent<LayoutElement>();
        layout.ignoreLayout = ignore;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Prevent dragging if puzzle is already solved
        if (JigsawPuzzleManager.Instance.IsPuzzleSolved(PuzzleId))
        {
            return;
        }
        
        // If we are in storage, we don't drag ourselves, we spawn a copy
        if (transform.parent == solveUI.StorageParent && LinkedStoragePiece == null)
        {
            solveUI.OnStartDraggingFromStorage(this, eventData);
            return;
        }

        originalParent = transform.parent;
        originalPosition = rectTransform.anchoredPosition;
        
        // Move to drag layer
        transform.SetParent(solveUI.DragLayer);
        SetIgnoreLayout(true);
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.6f;
        
        // Scale up
        transform.localScale = Vector3.one * dragScale;
        
        solveUI.OnPieceDragStart(this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / solveUI.CanvasScale;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;
        
        // Reset scale (UI layout or slot placement will handle final scale)
        transform.localScale = Vector3.one;
        
        solveUI.OnPieceDragEnd(this);
    }

    public void SetVisible(bool visible)
    {
        if (canvasGroup != null) canvasGroup.alpha = visible ? 1f : 0f;
        image.enabled = visible;
    }

    public void ResetToOriginal()
    {
        transform.SetParent(originalParent);
        rectTransform.anchoredPosition = originalPosition;
        SetIgnoreLayout(false);
    }
    }
}
