using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MovePieceScript : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [HideInInspector] public Transform newParent;
    private Transform originalParent;
    private GameObject originalTile;
    public Image image;

    // Flag to track if the drag was legal and actually started
    private bool isDragLegal = false;

    // CanvasGroup to allow underlying drops while dragging
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 1. Get the piece's color
        int pieceValue = int.Parse(this.gameObject.name);
        int pieceColor = ChessManager.Instance.GetPieceColor(pieceValue);

        if (!ChessManager.Instance.IsMyPiece(pieceColor))
        {
            isDragLegal = false;
            return;
        }

        // 2. CHECK TURN: If it's not this piece's turn, abort the drag immediately.
        if (pieceColor != ChessManager.Instance.currentTurnColor)
        {
            isDragLegal = false;
            return;
        }

        // --- If the check passes, proceed with drag setup ---
        isDragLegal = true;

        // Set values to track and use (store original wrapper parent)
        newParent = null; // we'll rely on OnDrop or pointer raycast to set this
        originalParent = transform.parent; // wrapper (usually the tile's child)
        originalTile = transform.parent != null && transform.parent.parent != null
            ? transform.parent.parent.gameObject
            : null;

        // disable image blocking so underlying UI can receive drop events
        if (image != null) image.raycastTarget = false;

        // Use CanvasGroup to allow drop to work
        canvasGroup.blocksRaycasts = false;

        // Place piece outside of hierarchy to be above everything
        transform.SetParent(ChessManager.Instance.MainView);
        transform.SetAsLastSibling();

        // Highlight possible moves (This will also set selectedPiece)
        ChessManager.Instance.CheckMove(originalTile, this.gameObject);

        // move to have bottom of obj be on cursor using lerp
        // grow in size using lerp
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Only allow movement if the drag was legal
        if (isDragLegal)
        {
            // Piece follows cursor
            transform.position = Input.mousePosition;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragLegal) return;

        bool isMoveMade = false;

        // If OnDrop successfully set newParent, good. Otherwise try to use the raycast under pointer.
        if (newParent == null && eventData != null)
        {
            var raycastGO = eventData.pointerCurrentRaycast.gameObject;
            if (raycastGO != null)
            {
                // If user released over a child of the tile (e.g. wrapper), walk up to find TileScript or top tile
                Transform t = raycastGO.transform;
                while (t != null && t.GetComponent<TileScript>() == null)
                {
                    t = t.parent;
                }
                if (t != null && t.GetComponent<TileScript>() != null)
                {
                    newParent = t;
                }
            }
        }

        // 1. Check if selected tile is in list of moveable tiles
        if (newParent != null && ChessManager.Instance.moves.Contains(newParent.gameObject))
        {
            // Move piece to new tile
            ChessManager.Instance.MovePiece(originalTile, newParent.gameObject);
            isMoveMade = true;
        }

        // 2. Handle invalid drop (snap back)
        if (!isMoveMade)
        {
            // Back to wrapper (originalParent should be the wrapper transform)
            if (originalParent != null)
            {
                transform.SetParent(originalParent);
                transform.localPosition = Vector3.zero;
            }
            else
            {
                // as a last resort, parent back to board so it's not lost
                transform.SetParent(ChessManager.Instance.MainView);
            }

            ChessManager.Instance.ResetObjects();
        }

        // Restore blocking so piece is interactable again
        if (image != null) image.raycastTarget = true;
        canvasGroup.blocksRaycasts = true;
        isDragLegal = false;
        newParent = null;
        // return to original size
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        int pieceValue = int.Parse(this.gameObject.name);
        int pieceColor = ChessManager.Instance.GetPieceColor(pieceValue);

        if (!ChessManager.Instance.IsMyPiece(pieceColor))
        {
            isDragLegal = false;
            return;
        }

        // Highlight possible moves
        GameObject currenttile = transform.parent != null && transform.parent.parent != null
            ? transform.parent.parent.gameObject
            : null;
        ChessManager.Instance.CheckMove(currenttile, this.gameObject);
        //ChessManager.Instance.focus = this.gameObject;
    }
}
