using UnityEngine;
using UnityEngine.Assertions;
using static Com.Culling.AABBCullingHelper;

namespace Com.Culling
{
    /// <summary>
    /// 表示剔除组的公开行为，由持有组的服务等调用的方法和属性
    /// </summary>
    public interface IAABBCullingGroup
    {
        int Count { get; set; }
        Camera ReferenceCamera { get; set; }

        void EraseAt(int index);
        void SetLodLevels(float[] lodLevels);
        void Setup(Bounds[] array);
        void InitInternalBuffers(int count);
        void Update();
    }

    /// <summary>
    /// 用于模式匹配的抽象类，<see cref="AABBCullingGroupKeeperTemplate{TGroup, TVolume}"/>
    /// </summary>
    public abstract class AbsAABBCullingGroup : IAABBCullingGroup
    {
        public AABBCullingStateChanged onStateChanged;

        public abstract int Count { get; set; }
        public abstract Camera ReferenceCamera { get; set; }

        public abstract void EraseAt(int index);
        public abstract void SetLodLevels(float[] lodLevels);
        public abstract void Setup(Bounds[] array);
        public abstract void InitInternalBuffers(int count);
        public abstract void CheckEvent();
        public abstract AABBCullingContext GetInternalVisibleContextAt(int index);
        public abstract void Update();
    }

    /// <summary>
    /// 最基本的剔除组，单线程代码
    /// </summary>
    public class SimpleAABBCullingGroup : AbsAABBCullingGroup
    {
        public static readonly float[] defaultLodLevels = new float[] { 1, 0.5f, 0.25f, 0.025f };
        /* 相机
         *   世界空间到投影空间的变换
         * 
         * 每物体
         *   世界空间包围盒
         *   可见性
         *   视野内高度
         */

        static readonly float[] emptyLodLevels = new float[1] { 0 };

        protected float[] lodLevels = emptyLodLevels;
        protected int bufferCount = 0;

        protected Camera referenceCamera;
        protected Matrix4x4 vpMatrix;
        protected readonly Plane[] frustumPlanes = new Plane[6];

        protected int capacity;
        protected Bounds[] bounds;

        // 两个缓冲区在来回交换，作为“当前帧”和“上一帧”的缓冲区
        protected AABBCullingContext[] ctx0;
        protected AABBCullingContext[] ctx1;

        protected int revertCtxBufferFrame = 0;

        public override int Count
        {
            get => bufferCount;
            set
            {
                Assert.IsTrue(value < bounds.Length + 1, $"length <= {bounds.Length}");
                bufferCount = value;
            }
        }

        public override Camera ReferenceCamera
        {
            get => referenceCamera;
            set
            {
                referenceCamera = value;
                Update_VP_Matrix();
            }
        }

        public override void EraseAt(int index)
        {
            Assert.IsTrue(index > -1, "invalid index");
            Assert.IsTrue(index < bufferCount, $"remove ({index}) but count= {bufferCount}");

            --bufferCount;
            bounds[index] = bounds[bufferCount];
            ctx0[index] = ctx0[bufferCount];
            ctx1[index] = ctx1[bufferCount];
            // buffer[bufferCount]: index changed
            // bufferCount => index
        }

        /// <summary>
        /// 重置缓冲区状态，当前帧保存的内容全部设置为可见且视口中高度为 1
        /// </summary>
        /// <param name="count"></param>
        public override void InitInternalBuffers(int count)
        {
            bufferCount = count;
            //bool rev = revertCtxBufferFrame % 2 != 0;
            //AABBCullingContext[] before = rev ? ctx0 : ctx1,
            //    after = rev ? ctx1 : ctx0;
            // ctx0 => after
            // ctx1 => before
            revertCtxBufferFrame = 0;
            ctx0.Initialize();
            for (int i = 0; i < count; i++)
            {
                ctx1[i] = AABBCullingContext.Visible;
            }
        }

        public override void SetLodLevels(float[] lodLevels)
        {
            if (lodLevels != null)
            {
                Assert.IsTrue(lodLevels.Length < 128, "最多 127 段");
                Assert.IsTrue(IsDescending(lodLevels), "需要倒序");
            }
            this.lodLevels = lodLevels ?? emptyLodLevels;
        }

        public override void Setup(Bounds[] array)
        {
            Assert.IsTrue(array != null);
            bounds = array;
            //...
            capacity = bounds.Length;
            Realloc(ref ctx0, capacity);
            Realloc(ref ctx1, capacity);
        }

        /* 计算
         *   计算可见性
         * 检查
         *   当前结果比较上一次的结果
         *   交换引用
         *   
         * LOD level?
         *   1..n..0
         */

        public override void Update()
        {
            Update_VP_Matrix();

            bool rev = revertCtxBufferFrame % 2 != 0;
            AABBCullingContext[] before = rev ? ctx0 : ctx1,
                after = rev ? ctx1 : ctx0;

            Culling(after, bounds, bufferCount);
            CheckEvent(before, after, bufferCount);

            revertCtxBufferFrame++;
        }

        /// <summary>
        /// 按当前帧的内容立即检查并发送一次事件
        /// </summary>
        public override void CheckEvent()
        {
            bool rev = revertCtxBufferFrame % 2 != 0;
            AABBCullingContext[] before = rev ? ctx0 : ctx1,
                after = rev ? ctx1 : ctx0;
            CheckEvent(before, after, bufferCount);
        }

        public override AABBCullingContext GetInternalVisibleContextAt(int index)
        {
            if (index < 0 || index > bufferCount - 1) { return default; }
            bool rev = revertCtxBufferFrame % 2 != 0;
            AABBCullingContext[] curr = rev ? ctx1 : ctx0;
            return curr[index];
        }

        protected virtual void CheckEvent(AABBCullingContext[] before, AABBCullingContext[] after, int count)
        {
            // 2000 times, 0.40 ms
            for (int i = 0; i < count; i++)
            {
                var prevState = (byte)(HeightToLodLevel(before[i].height, lodLevels) & AABBCullingGroupEvent.lodLevelMask);
                var currState = (byte)(HeightToLodLevel(after[i].height, lodLevels) & AABBCullingGroupEvent.lodLevelMask);

                if (!before[i].visible && after[i].visible) { currState |= AABBCullingGroupEvent.visibleMask; }
                if (before[i].visible && !after[i].visible) { prevState |= AABBCullingGroupEvent.visibleMask; }
                if (prevState != currState)
                {
                    // send...
                    AABBCullingGroupEvent ctx = default;
                    ctx.index = i;
                    ctx.prevState = prevState;
                    ctx.currState = currState;
                    onStateChanged?.Invoke(ctx);
                }
            }
        }

        protected virtual void Update_VP_Matrix()
        {
            if (referenceCamera)
            {
                VpMatrix(ref vpMatrix, referenceCamera);
                GeometryUtility.CalculateFrustumPlanes(vpMatrix, frustumPlanes);
            }
        }

        protected virtual unsafe void Culling(AABBCullingContext[] dst, Bounds[] src, int count)
        {
            // 2000 bounds, about 7.0 ms
            Vector3* vec8 = stackalloc Vector3[8];
            for (int i = 0; i < count; i++)
            {
                src[i].GetBoundsVerticesUnsafe(vec8);
                float maxY = float.MinValue, minY = float.MaxValue;
                for (int j = 0; j < 8; j++)
                {
                    Vector4 p = default;
                    ToPoint(vec8[j], ref p);

                    Vector4 clipPos = default;
                    vpMatrix.Mul(p, ref clipPos);
                    // ???: (clipSpace.xy * 0.5 + clipSpace.w * 0.5) / clipSpace.w
                    float viewPosY = 0.5f + 0.5f * clipPos.y / clipPos.w;
                    minY = Min(minY, viewPosY); maxY = Max(maxY, viewPosY);
                }

                AABBCullingContext cullingResult = default;
                cullingResult.height = maxY - minY;
                cullingResult.visible = GeometryUtility.TestPlanesAABB(frustumPlanes, src[i]);
                dst[i] = cullingResult;
            }
        }
    }
}