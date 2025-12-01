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
        // ensure wrapper exists (child 0 is expected to be wrapper)
        if (transform.childCount == 0)
        {
            Debug.LogWarning("Tile has no wrapper child (expected child 0). OnDrop ignored.");
            return;
        }

        Transform wrapper = transform.GetChild(0);

        GameObject dropped = eventData.pointerDrag;
        if (dropped == null)
        {
            // nothing dropped
            return;
        }

        var moveScript = dropped.GetComponent<MovePieceScript>();
        if (moveScript == null) return;

        moveScript.newParent = transform;

        // if tile already has a piece(s) handle capture via pooling
        if (wrapper.childCount > 0)
        {
            if (ChessManager.Instance.moves.Contains(gameObject))
            {
                foreach (Transform child in wrapper)
                {
                    ChessManager.Instance.PoolPiece(child.gameObject);
                }
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Move piece if selected tile is in list of moveable tiles
        if (ChessManager.Instance.moves.Contains(gameObject))
        {
            // Clear any object in the tile's wrapper (Capture logic for touch/click moves)
            if (transform.childCount > 0)
            {
                Transform wrapper = transform.GetChild(0);
                if (wrapper != null && wrapper.childCount > 0)
                {
                    foreach (Transform child in wrapper)
                    {
                        ChessManager.Instance.PoolPiece(child.gameObject);
                    }
                }
            }

            GameObject movingPiece = ChessManager.Instance.selectedPiece;

            ChessManager.Instance.MovePiece(ChessManager.Instance.selectedPiece.transform.parent.transform.parent.gameObject, this.gameObject);

            if (ChessManager.Instance.isPromotionPending)
            {
                ChessManager.Instance.selectedPiece = movingPiece;
            }
        }

        // Remove tile highlights if piece loses focus
        if (ChessManager.Instance.focus != this.gameObject)
        {
            // Promotion cancellation must handle its own cleanup to preserve selectedPiece.
            if (!ChessManager.Instance.isPromotionPending)
            {
                ChessManager.Instance.ResetObjects();
            }

            ChessManager.Instance.focus = this.gameObject;
        }
    }
}
