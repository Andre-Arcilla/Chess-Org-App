using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using static UnityEngine.UI.Image;

public class TileScript : MonoBehaviour, IDropHandler, IPointerDownHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        if (transform.GetChild(0).transform.childCount == 0)
        {
            GameObject dropped = eventData.pointerDrag;
            dropped.GetComponent<MovePieceScript>().newParent = transform;
        }
        // Temporary, add logic to allow capture of pieces
        else
        {
            GameObject dropped = eventData.pointerDrag;
            dropped.GetComponent<MovePieceScript>().newParent = transform;

            if (ChessManager.Instance.Moves.Contains(gameObject))
            {
                foreach (Transform child in transform.GetChild(0))
                {
                    Destroy(child.gameObject);
                }
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Move piece if selected tile is in list of moveable tiles
        if (ChessManager.Instance.Moves.Contains(gameObject))
        {
            // Clear any object in the tile's wrapper
            if (transform.GetChild(0).transform.childCount > 0)
            {
                foreach (Transform child in transform.GetChild(0))
                {
                    Destroy(child.gameObject);
                }
            }
                        
            ChessManager.Instance.MovePiece(ChessManager.Instance.SelectedPiece.transform.parent.transform.parent.gameObject, this.gameObject);
        }

        // Remove tile highlights if piece loses focus
        if (ChessManager.Instance.focus != this.gameObject)
        {
            ChessManager.Instance.ResetObjects();
            ChessManager.Instance.focus = this.gameObject;
        }
    }
}