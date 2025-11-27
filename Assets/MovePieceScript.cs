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

        // Set values to track and use
        newParent = transform.parent.transform.parent;
        originalParent = transform.parent.transform.parent;
        originalTile = transform.parent.transform.parent.gameObject;

        image.raycastTarget = false;

        // Place piece outside of heirarchy to be above everything
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

        // 1. Check if selected tile is in list of moveable tiles
        if (ChessManager.Instance.moves.Contains(newParent.gameObject))
        {
            // Move piece to new tile
            ChessManager.Instance.MovePiece(originalTile, newParent.gameObject);

            isMoveMade = true;
        }

        // 2. Handle invalid drop (snap back)
        if (!isMoveMade)
        {
            transform.SetParent(originalParent.GetChild(0));
            transform.localPosition = Vector3.zero;
            ChessManager.Instance.ResetObjects();
        }

        image.raycastTarget = true;
        isDragLegal = false;
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
        GameObject currenttile = transform.parent.transform.parent.gameObject;
        ChessManager.Instance.CheckMove(currenttile, this.gameObject);
        //ChessManager.Instance.focus = this.gameObject;
    }
}