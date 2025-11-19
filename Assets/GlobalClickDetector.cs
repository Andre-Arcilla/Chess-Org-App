using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GlobalClickDetector : MonoBehaviour, IPointerDownHandler
{
    public void OnPointerDown(PointerEventData eventData)
    {
        CheckFocusedElement();
    }

    public void CheckFocusedElement()
    {
        if (ChessManager.Instance.focus != this.gameObject)
        {
            ChessManager.Instance.ResetObjects();
            ChessManager.Instance.focus = this.gameObject;
        }
    }
}
