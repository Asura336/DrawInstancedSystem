namespace Com.Input.EventSystem
{
    public interface IMouseTouchEventHandler { }


    public interface IMouseEnterHandler : IMouseTouchEventHandler
    {
        void OnMouseTouchEnter(MouseTouchEventData e);
    }

    public interface IMouseHoldHandler : IMouseTouchEventHandler
    {
        void OnMouseTouchHold(MouseTouchEventData e);
    }

    public interface IMouseExitHandler : IMouseTouchEventHandler
    {
        void OnMouseTouchExit(MouseTouchEventData e);
    }


    public interface IMouseDownHandler : IMouseTouchEventHandler
    {
        void OnMouseTouchDown(MouseTouchEventData e);
    }
    public interface IMouseUpHandler : IMouseTouchEventHandler
    {
        void OnMouseTouchUp(MouseTouchEventData e);
    }
    public interface IMouseClickHandler : IMouseTouchEventHandler
    {
        void OnMouseClick(MouseTouchEventData e);
    }


    public interface IMouseBeginDragHandler : IMouseTouchEventHandler
    {
        void OnMouseBeginDrag(MouseTouchEventData e);
    }
    public interface IMouseDragHandler : IMouseTouchEventHandler
    {
        void OnMouseDragging(MouseTouchEventData e);
    }
    public interface IMouseEndDragHandler : IMouseTouchEventHandler
    {
        void OnMouseEndDrag(MouseTouchEventData e);
    }
}