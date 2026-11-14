using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MergeGame.View.Board
{
    /// <summary>
    /// Turns the EventSystem's pointer events on the board into gestures (a tap, or a drag with begin, moves and end)
    /// for one pointer at a time; other fingers are ignored until it lifts. Works for touch and mouse alike. Positions
    /// are screen points. Whether a press became a drag is decided by the EventSystem's drag threshold.
    /// </summary>
    public sealed class BoardInputSurface : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private const int NoPointer = int.MinValue;

        private int activePointerId = NoPointer;
        private bool isDragging;

        /// <summary>Press and release without dragging; carries the press position.</summary>
        public event Action<Vector2> Tapped;

        /// <summary>The press turned into a drag; carries the press position, where the drag really began.</summary>
        public event Action<Vector2> DragBegan;

        public event Action<Vector2> Dragged;
        public event Action<Vector2> DragEnded;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (activePointerId != NoPointer)
                return;

            activePointerId = eventData.pointerId;
            isDragging = false;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != activePointerId || isDragging)
                return;

            isDragging = true;
            DragBegan?.Invoke(eventData.pressPosition);
            Dragged?.Invoke(eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (IsActiveDrag(eventData))
                Dragged?.Invoke(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!IsActiveDrag(eventData))
                return;

            Release();
            DragEnded?.Invoke(eventData.position);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            // A drag is finished by OnEndDrag, which the EventSystem sends after this.
            if (eventData.pointerId != activePointerId || isDragging)
                return;

            Release();
            Tapped?.Invoke(eventData.pressPosition);
        }

        private void OnDisable() => Release();

        private bool IsActiveDrag(PointerEventData eventData) => isDragging && eventData.pointerId == activePointerId;

        private void Release()
        {
            activePointerId = NoPointer;
            isDragging = false;
        }
    }
}
