using System;
using System.Collections.Generic;
using Com.Collections;
using UnityEngine;

namespace Com.Input.EventSystem
{
    using EventData = MouseTouchEventData;

    public static class ExecuteEvents
    {
        public static T AssertValidateEventData<T>(EventData e) where T : class
        {
            if (!(e is T o))
            {
                throw new ArgumentException($"{e.GetType()} is not {typeof(T)}");
            }
            return o;
        }

        #region cached delegates...
        static readonly Action<IMouseEnterHandler, EventData> _mouseEnter =
            (h, e) => h.OnMouseTouchEnter(e);
        public static Action<IMouseEnterHandler, EventData> MouseEnterHandler => _mouseEnter;

        static readonly Action<IMouseHoldHandler, EventData> _mouseHold =
            (h, e) => h.OnMouseTouchHold(e);
        public static Action<IMouseHoldHandler, EventData> MouseHoldHandler => _mouseHold;

        static readonly Action<IMouseExitHandler, EventData> _mouseExit =
            (h, e) => h.OnMouseTouchExit(e);
        public static Action<IMouseExitHandler, EventData> MouseExitHandler => _mouseExit;


        static readonly Action<IMouseDownHandler, EventData> _mouseDown =
            (h, e) => h.OnMouseTouchDown(e);
        public static Action<IMouseDownHandler, EventData> MouseDownHandler => _mouseDown;

        static readonly Action<IMouseUpHandler, EventData> _mouseUp =
            (h, e) => h.OnMouseTouchUp(e);
        public static Action<IMouseUpHandler, EventData> MouseUpHandler => _mouseUp;

        static readonly Action<IMouseClickHandler, EventData> _mouseClick =
            (h, e) => h.OnMouseClick(e);
        public static Action<IMouseClickHandler, EventData> MouseClickHandler => _mouseClick;


        static readonly Action<IMouseBeginDragHandler, EventData> _mouseBeginDrag =
            (h, e) => h.OnMouseBeginDrag(e);
        public static Action<IMouseBeginDragHandler, EventData> MouseBeginDragHandler => _mouseBeginDrag;

        static readonly Action<IMouseDragHandler, EventData> _mouseDrag =
            (h, e) => h.OnMouseDragging(e);
        public static Action<IMouseDragHandler, EventData> MouseDragHandler => _mouseDrag;

        static readonly Action<IMouseEndDragHandler, EventData> _mouseEndDrag =
            (h, e) => h.OnMouseEndDrag(e);
        public static Action<IMouseEndDragHandler, EventData> MouseEndDragHandler => _mouseEndDrag;
        #endregion

        public static bool Execute<THandler>(GameObject target, EventData eventData, Action<THandler, EventData> functor)
            where THandler : class, IMouseTouchEventHandler
        {
            using var handlers = SharedCollection<IMouseTouchEventHandler>.ListCell.Create();
            GetEventList<THandler>(target, handlers);

            var count = handlers.Count;
            for (int i = 0; i < count; i++)
            {
                if (handlers[i] is THandler handler)
                {
                    try
                    {
                        functor(handler, eventData);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
                else if (UnityEngine.Application.isEditor)
                {
                    Debug.LogError($"Type {typeof(THandler)} expectrd {handlers[i].GetType()}");
                    continue;
                }
            }

            return handlers.Count > 0;
        }

        /// <summary>
        /// 指定<see cref="GameObject">游戏物体</see>能处理<see cref="IMouseTouchEventHandler">事件</see>
        /// </summary>
        /// <returns></returns>
        public static bool CanHandleEvent<THandler>(GameObject obj)
            where THandler : class, IMouseTouchEventHandler
        {
            using var handlers = SharedCollection<IMouseTouchEventHandler>.ListCell.Create();
            GetEventList<THandler>(obj, handlers);
            return handlers.Count > 0;
        }


        /// <summary>
        /// 从节点冒泡，沿节点树检索实际处理事件的物件
        /// </summary>
        /// <typeparam name="THandler"></typeparam>
        /// <param name="node"></param>
        /// <returns></returns>
        public static GameObject BubbleEventHandler<THandler>(GameObject node)
            where THandler : class, IMouseTouchEventHandler
        {
            if (!node) { return null; }

            for (var t = node.transform; t; t = t.parent)
            {
                if (CanHandleEvent<THandler>(t.gameObject))
                {
                    return t.gameObject;
                }
            }
            return null;
        }

        static void GetEventList<THandler>(GameObject obj, IList<IMouseTouchEventHandler> desk)
            where THandler : class, IMouseTouchEventHandler
        {
            if (!obj || !obj.activeInHierarchy) { return; }

            using var components = SharedCollection<Component>.ListCell.Create();
            obj.GetComponents(components.Body);

            int count = components.Count;
            for (int i = 0; i < count; i++)
            {
                if (ShouldSendToComponent<THandler>(components[i], out var h) && h != null)
                {
                    desk.Add(h);
                }
            }
        }

        static bool ShouldSendToComponent<THandler>(Component c, out THandler handler)
             where THandler : class, IMouseTouchEventHandler
        {
            if (c is THandler h)
            {
                handler = h;
                return c is Behaviour b && b.isActiveAndEnabled;
            }
            else
            {
                handler = null;
                return false;
            }
        }
    }
}