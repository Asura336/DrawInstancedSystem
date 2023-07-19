using System;
using System.Collections.Generic;
using Com.Core;
using UnityEngine;

namespace Com.Input
{
    /// <summary>
    /// <see cref="CameraManager">相机管理器</see>的“外壳”，描述鼠标和三维空间的交互方式。
    /// 同其他组件交互时透过该组件，继承此类以自定义函数接口的行为
    /// </summary>
    [RequireComponent(typeof(CameraManager))]
    [DisallowMultipleComponent]
    public class CameraManagerInterface : MonoBehaviour
    {
        CameraManager m_manager = null!;
        public CameraManager Manager => m_manager ? m_manager :
            (m_manager = GetComponent<CameraManager>());

        public virtual Vector3 MainViewportPosition =>
            CameraManager.Instance.Main!.ScreenToViewportPoint(UnityEngine.Input.mousePosition);

        public virtual Ray MainCameraPoint =>
            CameraManager.Instance.Main!.ViewportPointToRay(MainViewportPosition);

        /// <summary>
        /// 鼠标没有被 UI 阻挡
        /// </summary>
        public virtual bool PointerOnSpace
        {
            get
            {
                var _eventSysyem = UnityEngine.EventSystems.EventSystem.current;
                return !_eventSysyem || !_eventSysyem.IsPointerOverGameObject();
            }
        }

        public bool InitCompleted { get; protected set; } = false;

        static CameraManagerInterface insance;
        public static T GetInstance<T>() where T : CameraManagerInterface
        {
            return insance as T;
        }

        static readonly List<Action<CameraManagerInterface>> __afterInstanceInitialActions = new List<Action<CameraManagerInterface>>(16);

        private void Awake()
        {
            this.SingletonCheck();
            insance = this;

            InitCompleted = false;
            Initial();
            InitCompleted = true;
            InvokeAfterInitial();
        }

        /// <summary>
        /// 重写这个方法填写其他初始化过程
        /// </summary>
        protected virtual void Initial()
        {

        }

        public static void InvokeAfterInitial(Action<CameraManagerInterface> action)
        {
            if (GetInstance<CameraManagerInterface>().Assign(out var _ins)
                && _ins.InitCompleted)
            {
                action(_ins);
            }
            else
            {
                __afterInstanceInitialActions.Add(action);
            }
        }

        void InvokeAfterInitial()
        {
            foreach (var act in __afterInstanceInitialActions)
            {
                act(this);
            }
        }
    }
}