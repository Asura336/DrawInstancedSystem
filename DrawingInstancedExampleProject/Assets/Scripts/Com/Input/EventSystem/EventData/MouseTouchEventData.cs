using UnityEngine;

#nullable enable
namespace Com.Input.EventSystem
{
    /// <summary>
    /// see <see cref="MouseTouchInputModule"/>
    /// </summary>
    public sealed class MouseTouchEventData
    {
        public GameObject? CurrentTouch { get; set; }
        public GameObject? PrevTouch { get; set; }

        /// <summary>
        /// cached dragging object, 0 with left, 1 with right and 2 with middle
        /// </summary>
        public readonly GameObject?[] Dragging;
        public ref GameObject? DraggingObject(MouseInputButton mouseButtonFlag)
        {
            return ref Dragging[ConvertIndex(mouseButtonFlag)];
        }

        public Camera TargetCamera { get; set; }

        public Vector2 ScreenCoord { get; set; }
        public Vector2 ScreenCoordDelta { get; set; }

        public Vector2 ViewportCoord { get; set; }
        public Vector2 ViewportCoordDelta { get; set; }

        public Ray PointRay => TargetCamera.ViewportPointToRay(ViewportCoord);

        public RaycastResult MouseRaycastResult { get; set; }

        public MouseInputButton MouseButton { get; set; }
        public MouseInputButton PrevMouseButton { get; set; }

        public readonly int[] clickCount;
        public ref int ClickCount(MouseInputButton mouseButtonFlag)
        {
            return ref clickCount[ConvertIndex(mouseButtonFlag)];
        }

        public MouseTouchEventData()
        {
            Dragging = new GameObject[3];
            clickCount = new int[3];
            TargetCamera = null!;
        }

        static int ConvertIndex(MouseInputButton mouseButtonFlag)
        {
            switch (mouseButtonFlag)
            {
                case MouseInputButton.Left: return 0;
                case MouseInputButton.Right: return 1;
                case MouseInputButton.Middle: return 2;
                default: return ConvertIndexOr(mouseButtonFlag);
            }
        }

        static int ConvertIndexOr(MouseInputButton mouseButtonFlag)
        {
            if (mouseButtonFlag.HasFlag(MouseInputButton.Left))
            {
                return 0;
            }
            if (mouseButtonFlag.HasFlag(MouseInputButton.Right))
            {
                return 1;
            }
            if (mouseButtonFlag.HasFlag(MouseInputButton.Middle))
            {
                return 2;
            }
            return 0;
        }
    }
}