using System;
using Com.Core;
using UnityEngine;

#nullable enable
namespace Com.Input.EventSystem
{
    /// <summary>
    /// 简易版本的输入模块，用于鼠标在空间内的点击和拖拽行为。
    /// 因为解释鼠标的行动依赖于相机显示的画面，
    /// 输入模块的实现依赖 <see cref="CameraManagerInterface">相机管理器接口</see>，
    /// 如果接入新的UI组件或使用多个视口的布局，
    /// 重写<see cref="CameraManagerInterface">相机管理器接口</see>的方法
    /// 实现和相机有关的功能。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MouseTouchInputModule : MonoBehaviour
    {
        CameraManagerInterface cmInterface = null!;

        public Camera MainCamera
        {
            get
            {
                var _main = cmInterface.Manager.Main;
                if (_main == null)
                {
                    _main = cmInterface.Manager.CurrentActive;
                }
                if (_main == null)
                {
                    throw new MissingReferenceException("相机管理器未配置相机");
                }
                return _main;
            }
        }

        public Vector3 ViewportPosition => cmInterface.MainViewportPosition;

        /// <summary>
        /// 从 <see cref="CameraManagerInterface"/> 或继承类获取鼠标未被 UI 阻挡的状态。
        /// 鼠标未被 UI 阻挡时输入消息会通过 <see cref="ExecuteEvents.Execute"/> 传递给物体；
        /// 在任何时机都可以从全局事件获取输入信号，如果不希望在 UI 上响应全局事件，在回调中过滤。
        /// </summary>
        public bool PointerOnSpace => cmInterface.PointerOnSpace;

        public Ray PointRay => MainCamera.ViewportPointToRay(ViewportPosition);
        public float raycastDistance = 1000;
        public LayerMask raycastLayer = ~0;

        /// <summary>
        /// 鼠标按下和抬起的间隔（秒）小于此值时识别为点击动作
        /// </summary>
        [Header("鼠标按下和抬起的间隔（秒）小于此值时识别为点击动作")]
        [Range(0, 1f)]
        [SerializeField] float mouseupTime = 0.1667f;
        /// <summary>
        /// 鼠标两次抬起间隔小于此值时点击计数增加一次
        /// </summary>
        [Header("鼠标两次抬起间隔小于此值时点击计数增加一次")]
        [Range(0, 1f)]
        [SerializeField] float clickTime = 0.2f;

        public MouseTouchEventData MouseTouchData { get; private set; } = null!;

        public GameObject? CurrentRaycastTarget { get; private set; }

        /// <summary>
        /// 全局的鼠标点击事件，参数 { 按键序号, 点击目标, 点击次数 }
        /// </summary>
        public event Action<int, GameObject?, int>? OnMouseClick;
        /// <summary>
        /// 全局的鼠标按下事件，参数 { 按键序号, 点击目标, 点击次数 }
        /// </summary>
        public event Action<int, GameObject?, int>? OnMouseDown;

        /// <summary>
        /// 鼠标离开或者进入UI时发送消息，传入<see cref="PointerOnSpace"/>
        /// </summary>
        public event Action<bool>? OnPointerFocus;

        public static MouseTouchInputModule Instance { get; private set; } = null!;
        private void Awake()
        {
            this.SingletonCheck();
            Instance = this;

            CameraManagerInterface.InvokeAfterInitial(OnCMInterfaceInit);

            MouseTouchData = new MouseTouchEventData();

            _clickCounters = new ClickCounterGroup(mouseupTime, clickTime);
            InitClickCounters();
        }
        void OnCMInterfaceInit(CameraManagerInterface c)
        {
            cmInterface = c;
            _prevPointerOnSpace = PointerOnSpace;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ApplyClickResponseTime();
        }
#endif

        private void OnEnable()
        {
            _prevScreenCoord = UnityEngine.Input.mousePosition;
        }

        private void Update()
        {
            // 处理输入，收集当前帧信息
            CheckPointerOnSpace();
            TickModule();
            TickClickCount();
        }

        private void LateUpdate()
        {
            // 延迟发送消息
            ExecuteCommands();

            WriteToPrevFrameStates();
        }


        /// <summary>
        /// 重新构造点击响应组件
        /// </summary>
        public void ApplyClickResponseTime()
        {
            _clickCounters = new ClickCounterGroup(mouseupTime, clickTime);
            InitClickCounters();
        }


        #region cached states...
        Vector3 _prevScreenCoord;
        Vector3 _prevViewportCoord;
        bool _prevPointerOnSpace = true;

        RaycastResult _prevFrameRR;

        ClickCounterGroup _clickCounters = null!;
        #endregion

        #region inline calls...
        void CheckPointerOnSpace()
        {
            if (PointerOnSpace != _prevPointerOnSpace)
            {
                OnPointerFocus?.Invoke(PointerOnSpace);
                _prevPointerOnSpace = PointerOnSpace;
            }
        }
        void TickModule()
        {
            RaycastOnSpace();
            CheckPointUpOrDown();
        }
        void RaycastOnSpace()
        {
            MouseTouchData.TargetCamera = MainCamera;
            var _screenCoord = UnityEngine.Input.mousePosition;
            MouseTouchData.ScreenCoord = _screenCoord;
            MouseTouchData.ScreenCoordDelta = _screenCoord - _prevScreenCoord;
            MouseTouchData.ViewportCoord = ViewportPosition;
            MouseTouchData.ViewportCoordDelta = ViewportPosition - _prevViewportCoord;

            var rayCast = Physics.Raycast(ray: PointRay,
                maxDistance: raycastDistance,
                layerMask: raycastLayer,
                hitInfo: out var hit);
            CurrentRaycastTarget = rayCast ? hit.collider.gameObject : null;
            RaycastResult _raycastResult = rayCast ? hit : default;
            _raycastResult.screenCoord = _screenCoord;
            MouseTouchData.MouseRaycastResult = _raycastResult;
        }
        void CheckPointUpOrDown()
        {
            var _mouseButton = MouseInputButton.None;
            if (getMouse(0))
            {
                _mouseButton |= MouseInputButton.Left;
            }
            if (getMouse(1))
            {
                _mouseButton |= MouseInputButton.Right;
            }
            if (getMouse(2))
            {
                _mouseButton |= MouseInputButton.Middle;
            }
            MouseTouchData.MouseButton = _mouseButton;

            static bool getMouse(int mouseButton) =>
                UnityEngine.Input.GetMouseButtonDown(mouseButton) ||
                UnityEngine.Input.GetMouseButton(mouseButton);
        }

        void TickClickCount()
        {
            //...
            var deltaTime = Time.deltaTime;
            for (int i = 0; i < ClickCounterGroup.count; i++)
            {
                _clickCounters[i].Tick(deltaTime);
            }
        }
        void InitClickCounters()
        {
            var _ccLeft = _clickCounters.GetCounter(MouseInputButton.Left);
            _ccLeft.onMouseDown = (obj, count) =>
            {
                // 鼠标刚刚按下时传递的点击计数还未增加，少1次
                Instance.MouseTouchData.ClickCount(MouseInputButton.Left) = count + 1;
                Instance.OnMouseDown?.Invoke(0, obj, count);
                if (obj != null)
                {
                    Instance.Execute(obj, ExecuteEvents.MouseDownHandler);
                }
                // 这里 MouseTouchData 的点击计数不清空
            };
            _ccLeft.onTickEnd = (obj, count) =>
            {
                // click left
                //Debug.Log($"click left [{obj?.name ?? "null"}]: {count}");
                Instance.MouseTouchData.ClickCount(MouseInputButton.Left) = count;
                Instance.OnMouseClick?.Invoke(0, obj, count);
                if (obj != null)
                {
                    Instance.Execute(obj, ExecuteEvents.MouseClickHandler);
                }
                Instance.MouseTouchData.ClickCount(MouseInputButton.Left) = 0;
            };

            var _ccRight = _clickCounters.GetCounter(MouseInputButton.Right);
            _ccRight.onMouseDown = (obj, count) =>
            {
                // 鼠标刚刚按下时传递的点击计数还未增加，少1次
                Instance.MouseTouchData.ClickCount(MouseInputButton.Right) = count + 1;
                Instance.OnMouseDown?.Invoke(1, obj, count);
                if (obj != null)
                {
                    Instance.Execute(obj, ExecuteEvents.MouseDownHandler);
                }
                // 这里 MouseTouchData 的点击计数不清空
            };
            _ccRight.onTickEnd = (obj, count) =>
            {
                // click right
                //Debug.Log($"click right [{obj?.name ?? "null"}]: {count}");
                Instance.MouseTouchData.ClickCount(MouseInputButton.Right) = count;
                Instance.OnMouseClick?.Invoke(1, obj, count);
                if (obj != null)
                {
                    Instance.Execute(obj, ExecuteEvents.MouseClickHandler);
                }
                Instance.MouseTouchData.ClickCount(MouseInputButton.Right) = 0;
            };

            var _ccMiddle = _clickCounters.GetCounter(MouseInputButton.Middle);
            _ccMiddle.onMouseDown = (obj, count) =>
            {
                // 鼠标刚刚按下时传递的点击计数还未增加，少1次
                Instance.MouseTouchData.ClickCount(MouseInputButton.Middle) = count + 1;
                Instance.OnMouseDown?.Invoke(2, obj, count);
                if (obj != null)
                {
                    Instance.Execute(obj, ExecuteEvents.MouseDownHandler);
                }
                // 这里 MouseTouchData 的点击计数不清空
            };
            _ccMiddle.onTickEnd = (obj, count) =>
            {
                // click middle
                //Debug.Log($"click middle [{obj?.name ?? "null"}]: {count}");
                Instance.MouseTouchData.ClickCount(MouseInputButton.Middle) = count;
                Instance.OnMouseClick?.Invoke(2, obj, count);
                if (obj != null)
                {
                    Instance.Execute(obj, ExecuteEvents.MouseClickHandler);
                }
                Instance.MouseTouchData.ClickCount(MouseInputButton.Middle) = 0;
            };
        }


        void WriteToPrevFrameStates()
        {
            _prevScreenCoord = UnityEngine.Input.mousePosition;
            _prevViewportCoord = MouseTouchData.ViewportCoord;
            _prevFrameRR = MouseTouchData.MouseRaycastResult;

            MouseTouchData.PrevMouseButton = MouseTouchData.MouseButton;
        }

        /// <summary>
        /// 写入所有状态，实际发送消息的阶段
        /// </summary>
        void ExecuteCommands()
        {
            CheckPointerEnterAndExit(_prevFrameRR, MouseTouchData.MouseRaycastResult);

            CheckPointerButtonAct(_prevFrameRR, MouseTouchData.MouseRaycastResult,
                MouseTouchData.PrevMouseButton, MouseTouchData.MouseButton,
                 MouseInputButton.Left);
            CheckPointerButtonAct(_prevFrameRR, MouseTouchData.MouseRaycastResult,
                MouseTouchData.PrevMouseButton, MouseTouchData.MouseButton,
                 MouseInputButton.Right);
            CheckPointerButtonAct(_prevFrameRR, MouseTouchData.MouseRaycastResult,
                MouseTouchData.PrevMouseButton, MouseTouchData.MouseButton,
                 MouseInputButton.Middle);
        }
        #endregion

        void CheckPointerEnterAndExit(in RaycastResult prev, in RaycastResult current)
        {
            if (current.hitTarget != prev.hitTarget)
            {
                MouseTouchData.PrevTouch = MouseTouchData.CurrentTouch;
                MouseTouchData.CurrentTouch = current.hitTarget;
                if (prev.hitTarget)
                {
                    Execute(prev.hitTarget!, ExecuteEvents.MouseExitHandler);
                }
                if (current.hitTarget)
                {
                    Execute(current.hitTarget!, ExecuteEvents.MouseEnterHandler);
                }
            }
            else if (current.hitTarget)
            {
                Execute(current.hitTarget!, ExecuteEvents.MouseHoldHandler);
            }
        }

        /* 点击事件
         * 在同一个物体上按下和抬起
         * 按下和抬起在时间间隔内
         * 每次符合要求的抬起增加计数
         * 每次计数增长后刷新倒计时
         * 倒计时结束时发送点击消息
         * 点击新物体会中断倒计时
         */

        void CheckPointerButtonAct(in RaycastResult prev, in RaycastResult current,
            MouseInputButton prevMouseButton, MouseInputButton currMouseButton,
            MouseInputButton flag)
        {
            if (currMouseButton.HasFlag(flag))
            {
                // press button...
                if (prevMouseButton.HasFlag(flag))
                {
                    // dragging...
                    if (MouseTouchData.DraggingObject(flag))
                    {
                        Execute(MouseTouchData.DraggingObject(flag)!, ExecuteEvents.MouseDragHandler);
                    }
                    else
                    {
                        // hold down at space, pass
                    }
                }
                else
                {
                    // already down
                    if (current.hitTarget)
                    {
                        MouseTouchData.DraggingObject(flag) = current.hitTarget;
                        // [2022-01-25] 不在这里结算按下事件，见：this.InitTickCounters()
                        //Execute(current.hitTarget!, ExecuteEvents.MouseDownHandler);
                        Execute(current.hitTarget!, ExecuteEvents.MouseBeginDragHandler);
                    }
                    else
                    {
                        // down at space...
                    }
                    _clickCounters.GetCounter(flag).MouseDown(current.hitTarget);
                }
            }
            else
            {
                // release button...
                if (prevMouseButton.HasFlag(flag))
                {
                    // drag end
                    var _endDragObj = MouseTouchData.DraggingObject(flag);
                    if (_endDragObj != null)
                    {
                        if (current.hitTarget == _endDragObj)
                        {
                            Execute(current.hitTarget, ExecuteEvents.MouseUpHandler);
                        }
                        else
                        {
                            //...
                        }
                        Execute(_endDragObj, ExecuteEvents.MouseEndDragHandler);
                        MouseTouchData.DraggingObject(flag) = null;
                    }
                    else
                    {
                        // release on space...
                    }
                    _clickCounters.GetCounter(flag).MouseUp(current.hitTarget);
                }
                else
                {
                    // no action, pass
                }
            }
        }

        void Execute<T>(GameObject target, Action<T, MouseTouchEventData> functor)
            where T : class, IMouseTouchEventHandler
        {
            if (PointerOnSpace && !ExecuteEvents.Execute(target, MouseTouchData, functor))
            {
                var handler = ExecuteEvents.BubbleEventHandler<T>(target);
                if (handler)
                {
                    ExecuteEvents.Execute(handler!, MouseTouchData, functor);
                }
            }
        }


        class ClickCounterGroup
        {
            public const int count = 3;
            readonly ClickCounter[] counters;

            public ClickCounterGroup()
            {
                this.counters = new ClickCounter[count];
                for (int i = 0; i < count; i++)
                {
                    counters[i] = new ClickCounter();
                }
            }
            public ClickCounterGroup(float mouseupTime, float clickTime)
            {
                this.counters = new ClickCounter[count];
                for (int i = 0; i < count; i++)
                {
                    counters[i] = new ClickCounter(mouseupTime, clickTime);
                }
            }

            public ClickCounter this[int index] => counters[index];

            public ClickCounter GetCounter(MouseInputButton flag)
            {
                return flag switch
                {
                    MouseInputButton.Left => counters[0],
                    MouseInputButton.Right => counters[1],
                    MouseInputButton.Middle => counters[2],
                    _ => counters[0]
                };
            }
        }

        /// <summary>
        /// 点击计时器，自行缓存点击目标
        /// </summary>
        class ClickCounter
        {
            /// <summary>
            /// 鼠标按下和抬起的间隔（秒）小于此值时识别为点击动作
            /// </summary>
            public readonly float MOUSE_UP_TIME = 0.1667f;
            /// <summary>
            /// 鼠标两次抬起间隔小于此值时点击计数增加一次
            /// </summary>
            public readonly float CLICK_TIME = 0.2f;
            float _mouseDownTimer = 0;
            float _timer = 0;

            /// <summary>
            /// 鼠标按下和抬起的位置最小偏移（曼哈顿距离）小于此值时识别为点击动作
            /// </summary>
            public const float MOUSE_COORD_DELTA = 8;
            Vector3 _mouseCoord;

            public GameObject? Target { get; set; }
            public int Count { get; set; } = 0;

            public Action<GameObject?, int>? onMouseDown;
            public Action<GameObject?, int>? onTickEnd;

            public ClickCounter() { }
            public ClickCounter(float mouseupTime, float clickTime)
            {
                MOUSE_UP_TIME = mouseupTime;
                CLICK_TIME = clickTime;
            }

            public void MouseDown(GameObject? target)
            {
                _mouseDownTimer = MOUSE_UP_TIME;
                _mouseCoord = UnityEngine.Input.mousePosition;
                if (Target != target)
                {
                    Target = target;
                    Count = 0;
                }
                onMouseDown?.Invoke(target, Count);
            }

            public void MouseUp(GameObject? target)
            {
                if (_mouseDownTimer > 0)
                {
                    Count = target == Target ? Count + 1 : 1;
                    Target = target;
                    _timer = CLICK_TIME;
                    _mouseDownTimer = MOUSE_UP_TIME;
                }
            }

            public void Tick(float deltaTime)
            {
                if ((UnityEngine.Input.mousePosition - _mouseCoord).Manhattan() > MOUSE_COORD_DELTA)
                {
                    _timer = 0;
                    _mouseDownTimer = 0;
                    Count = 0;
                }

                bool _callTickEnd = false;
                if (_timer > 0)
                {
                    _timer -= deltaTime;
                }
                else if (Count != 0)
                {
                    _callTickEnd = true;
                }
                if (_mouseDownTimer > 0)
                {
                    _mouseDownTimer -= deltaTime;
                }
                else if (Count != 0)
                {
                    _callTickEnd = true;
                }
                if (_callTickEnd)
                {
                    onTickEnd?.Invoke(Target, Count);
                    Count = 0;
                }
            }
        }
    }
}