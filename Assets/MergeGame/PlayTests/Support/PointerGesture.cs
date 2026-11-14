using UnityEngine;
using UnityEngine.EventSystems;

namespace MergeGame.PlayTests.Support
{
    /// <summary>
    /// Sends the pointer events the EventSystem sends for a real finger (down, begin drag, drags, up, end drag) to a
    /// target, so gestures can be driven step by step and paused mid-drag.
    /// </summary>
    internal sealed class PointerGesture
    {
        private const int MousePointerId = -1;

        private readonly GameObject target;
        private readonly PointerEventData eventData;

        private PointerGesture(GameObject target, Vector2 screenPoint)
        {
            this.target = target;
            eventData = new PointerEventData(EventSystem.current)
            {
                pointerId = MousePointerId,
                button = PointerEventData.InputButton.Left,
                position = screenPoint,
                pressPosition = screenPoint
            };
        }

        public static PointerGesture Press(GameObject target, Vector2 screenPoint)
        {
            var gesture = new PointerGesture(target, screenPoint);
            ExecuteEvents.Execute(target, gesture.eventData, ExecuteEvents.pointerDownHandler);
            return gesture;
        }

        public static void Tap(GameObject target, Vector2 screenPoint) => Press(target, screenPoint).Release();

        public void BeginDrag()
        {
            eventData.dragging = true;
            ExecuteEvents.Execute(target, eventData, ExecuteEvents.beginDragHandler);
        }

        public void MoveTo(Vector2 screenPoint)
        {
            eventData.delta = screenPoint - eventData.position;
            eventData.position = screenPoint;
            ExecuteEvents.Execute(target, eventData, ExecuteEvents.dragHandler);
        }

        public void Release()
        {
            ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerUpHandler);
            if (!eventData.dragging)
                return;

            ExecuteEvents.Execute(target, eventData, ExecuteEvents.endDragHandler);
            eventData.dragging = false;
        }
    }
}
