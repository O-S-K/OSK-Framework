using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OSK
{
    public class ScrollDragState : MonoBehaviour, IBeginDragHandler, IEndDragHandler
    {
        public bool IsDragging { get; private set; }

        public void OnBeginDrag(PointerEventData eventData) => IsDragging = true;
        public void OnEndDrag(PointerEventData eventData) => IsDragging = false;
    }
}
