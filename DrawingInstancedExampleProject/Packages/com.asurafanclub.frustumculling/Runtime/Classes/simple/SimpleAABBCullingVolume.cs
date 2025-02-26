﻿using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Events;

namespace Com.Culling
{
    public interface IAABBCullingVolume
    {
        int Index { get; set; }
        bool Valid { get; }
        Bounds Volume { get; }
        internal bool VolumeUpdated { get; }
        Matrix4x4 LocalToWorld { get; }

        void DoBecameInvisible();
        void DoBecameVisible();
        void DoLodChanged(int level);
        void UpdateVolume();
    }

    /// <summary>
    /// 继承此类实现项目指定的被剔除物体。继承类需要指定寻找 <see cref="AABBCullingGroupKeeperTemplate{TGroup, TVolume}">剔除组</see>
    ///的过程。
    ///简单激活和休眠此组件即可自动注册和注销，调用方只需要更新包围盒与监听事件。
    /// </summary>
    /// <typeparam name="TGroupKeeper"></typeparam>
    public abstract class AABBCullingVolumeTemplate<TGroupKeeper> : MonoBehaviour, IAABBCullingVolume
        where TGroupKeeper : AbsAABBCullingGroupKeeper
    {
        [SerializeField] Bounds localBounds;
        [SerializeField] protected TGroupKeeper groupKeeper;
        [SerializeField] int index = -1;

        public UnityEvent onBecameVisible;
        public UnityEvent onBecameInvisible;
        public UnityEvent onVolumeDisabled;
        public UnityEvent<int> lodChanged;

        bool volumeUpdated;
        Transform cachedTransform;
        bool destroyed = false;

        protected abstract TGroupKeeper FindGroupKeeper();

        protected virtual void Awake()
        {
            cachedTransform = transform;
            onBecameVisible ??= new UnityEvent();
            onBecameInvisible ??= new UnityEvent();
            onVolumeDisabled ??= new UnityEvent();
            lodChanged ??= new UnityEvent<int>();

            index = -1;
        }

        protected virtual void OnEnable()
        {
            if (groupKeeper) { groupKeeper.Add(this); }
            else
            {
                groupKeeper = FindGroupKeeper();
                StartCoroutine(AddToKeeperNextFrame());
            }
        }

        IEnumerator AddToKeeperNextFrame()
        {
            yield return null;
            groupKeeper.Add(this);
        }

        protected virtual void OnDisable()
        {
            if (groupKeeper) { groupKeeper.Remove(this); }
            onVolumeDisabled?.Invoke();
        }

        protected virtual void OnDestroy()
        {
            destroyed = true;
            onBecameVisible.RemoveAllListeners();
            onBecameInvisible.RemoveAllListeners();
            lodChanged.RemoveAllListeners();
            onVolumeDisabled.RemoveAllListeners();
        }

        public override string ToString()
        {
            return gameObject ? gameObject.name : base.ToString();
        }

        public int Index
        {
            get => index;
            set => index = value;
        }

        public bool Valid => !destroyed && index != -1;


        bool IAABBCullingVolume.VolumeUpdated
        {
            get
            {
                if (destroyed) { return false; }
                if (volumeUpdated)
                {
                    volumeUpdated = false;
                    return true;
                }
                return false;
            }
        }

        Matrix4x4 IAABBCullingVolume.LocalToWorld
        {
            get
            {
                var t = cachedTransform ? cachedTransform : (cachedTransform = transform);
                return t ? t.localToWorldMatrix : Matrix4x4.identity;
            }
        }

        public void GetHeightAndVisible(out float height, out bool visible)
        {
            height = default;
            visible = default;
            if (groupKeeper is null) { return; }
            var group = groupKeeper.CullingGroup;
            var ctx = group.GetInternalVisibleContextAt(index);
            height = ctx.height;
            visible = ctx.visible;
        }

        [ContextMenu("update bounds")]
        public void UpdateVolume()
        {
            volumeUpdated = true;
        }

        public void DoBecameVisible()
        {
            onBecameVisible?.Invoke();
        }

        public void DoBecameInvisible()
        {
            onBecameInvisible?.Invoke();
        }

        public void DoLodChanged(int level)
        {
            lodChanged?.Invoke(level);
        }

        /// <summary>
        /// 当前世界空间下的轴对齐包围盒
        /// </summary>
        public Bounds Volume
        {
            get
            {
                var b = default(Bounds);
                localBounds.Mul(cachedTransform.localToWorldMatrix, ref b);
                return b;
            }
        }

        /// <summary>
        /// 本地空间下的轴对齐包围盒
        /// </summary>
        public Bounds LocalBounds
        {
            get => localBounds;
            set
            {
                var prevB = localBounds;
                unsafe
                {
                    if (!EqualsBounds(&prevB, &value))
                    {
                        localBounds = value;
                        volumeUpdated = true;
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static unsafe bool EqualsBounds(Bounds* a, Bounds* b)
        {
            ulong* pa = (ulong*)a, pb = (ulong*)b;
            // Bounds 相当于 6 个 float
            for (int i = 0; i < 3; i++)
            {
                if (pa[i] != pb[i]) { return false; }
            }
            return true;
        }
    }

    /// <summary>
    /// 对应 <see cref="SimpleAABBCullingGroup"/> 类型的剔除组，
    ///使用 <see cref="UnityEngine.Object.FindObjectOfType(System.Type)"/> 查询剔除组。
    ///作为算法原型，简单挂在物体上就可以用。需要保证场景里已经存在剔除组。
    /// </summary>
    public class SimpleAABBCullingVolume : AABBCullingVolumeTemplate<SimpleAABBCullingGroupKeeper>
    {
        protected override SimpleAABBCullingGroupKeeper FindGroupKeeper() => FindObjectOfType<SimpleAABBCullingGroupKeeper>();
    }
}