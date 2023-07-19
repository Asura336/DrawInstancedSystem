using System;
using Com.Core;
using UnityEngine;

#nullable enable
namespace Com.Input
{
    /// <summary>
    /// 保存相机引用，切换相机
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CameraManager : MonoBehaviour
    {
        public Camera[] m_cameras = null!;
        int currentActive;

        public static CameraManager Instance { get; private set; } = null!;
        private void Awake()
        {
            this.SingletonCheck();

            if (m_cameras == null || m_cameras.Length == 0)
            {
                Debug.LogError("需要为相机管理器配置相机引用");
                return;
            }
            for (int i = 0; i < m_cameras.Length; i++) { m_cameras[i].enabled = true; }
            _ = CurrentActive;  // 初始化 currentActive 标记

            Instance = this;
        }

        public Camera? Main
        {
            get
            {
                if (currentActive == -1) { return null; }

                return m_cameras[currentActive].isActiveAndEnabled ?
                    m_cameras[currentActive] : CurrentActive;
            }
        }

        public Camera? CurrentActive
        {
            get
            {
                for (int i = 0; i < m_cameras.Length; i++)
                {
                    if (m_cameras[i].isActiveAndEnabled)
                    {
                        currentActive = i;
                        return m_cameras[i];
                    }
                }
                currentActive = -1;
                return null;
            }
        }

        /// <summary>
        /// 管理器的主相机变更时触发事件，发送方是 <see cref="CameraManager"/>，
        /// 传递参数给 <see cref="GetCamera(int)"/> 获取相机引用
        /// </summary>
        public event EventHandler<int>? OnSetMain;
        public void SetMain(int index)
        {
            index %= m_cameras.Length;
            for (int i = 0; i < m_cameras.Length; i++)
            {
                m_cameras[i].enabled = i == index;
            }
            currentActive = index;
            OnSetMain?.Invoke(this, index);
        }

        public Camera GetCamera(int index)
        {
            var i = index < 0 ? m_cameras.Length - index : index;
            return m_cameras[i];
        }

        public Camera this[int index] => GetCamera(index);
    }
}