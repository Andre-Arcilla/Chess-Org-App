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

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Set values to track and use
        newParent = transform.parent.transform.parent;
        originalParent = transform.parent.transform.parent;
        originalTile = transform.parent.transform.parent.gameObject;

        image.raycastTarget = false;

        // Place piece outside of heirarchy to be above everything
        transform.SetParent(ChessManager.Instance.MainView);
        transform.SetAsLastSibling();

        // Highlight possible moves
        ChessManager.Instance.CheckMove(originalTile, this.gameObject);

        // move to have bottom of obj be on cursor using lerp
        // grow in size using lerp
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Piece follows cursor
        transform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Check if selected tile is in list of moveable tiles
        if (ChessManager.Instance.Moves.Contains(newParent.gameObject))
        {
            // Move piece to new tile
            ChessManager.Instance.MovePiece(originalTile, newParent.gameObject);
        }
        else
        {
            // Move piece back to original tile
            ChessManager.Instance.MovePiece(originalTile, originalParent.gameObject);
        }
        
        image.raycastTarget = true;

        // return to original size
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Highlight possible moves
        GameObject currenttile = transform.parent.transform.parent.gameObject;
        ChessManager.Instance.CheckMove(currenttile, this.gameObject);
        //ChessManager.Instance.focus = this.gameObject;
    }
}