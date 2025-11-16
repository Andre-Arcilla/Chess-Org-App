using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TileScript : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        if (transform.GetChild(0).transform.childCount == 0)
        {
            GameObject dropped = eventData.pointerDrag;
            MovePieceScript movedPiece = dropped.GetComponent<MovePieceScript>();
            movedPiece.originalParent = transform;
        }
        // Temporary, add logic to allow capture of pieces
        else
        {
            GameObject dropped = eventData.pointerDrag;
            MovePieceScript movedPiece = dropped.GetComponent<MovePieceScript>();
            movedPiece.originalParent = transform;
            Debug.Log(transform.GetChild(0).transform);
            foreach (Transform child in transform.GetChild(0))
            {
                Destroy(child.gameObject);
            }
        }
    }
}
