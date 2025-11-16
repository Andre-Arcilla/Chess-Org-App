using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MovePieceScript : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [HideInInspector] public Transform originalParent;
    private GameObject tile;
    public Image image;

    public void OnBeginDrag(PointerEventData eventData)
    {
        tile = transform.parent.transform.parent.gameObject;

        image.raycastTarget = false;
        originalParent = transform.parent.transform.parent;
        transform.SetParent(ChessManager.Instance.MainView);
        transform.SetAsLastSibling();

        // move to have bottom of obj be on cursor using lerp
        // grow in size using lerp
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        ChessManager.Instance.MovePiece(tile, originalParent.gameObject);
        transform.SetParent(originalParent.transform.GetChild(0).transform);
        image.raycastTarget = true;

        // return to original size
    }

    public void OnPointerClick(PointerEventData eventData)
    {

    }
}