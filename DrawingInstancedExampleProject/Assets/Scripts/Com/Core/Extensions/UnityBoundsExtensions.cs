using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Com.Core
{
    /// <summary>
    /// 计算包围盒尺寸等工具函数
    /// </summary>
    public static class UnityBoundsExtensions
    {
        public static Bounds GetLocalBounds(this Collider collider)
        {
            switch (collider)
            {
                case BoxCollider b:
                    return new Bounds(b.center, b.size);
                case SphereCollider s:
                    return new Bounds(s.center, Vector3.one * s.radius);
                case CapsuleCollider c:
                    float _cRad = c.radius;
                    return new Bounds(c.center, new Vector3(_cRad, c.height, _cRad));
                case MeshCollider m:
                    Mesh _mesh = m.sharedMesh;
                    if (!_mesh) { goto default; }
                    return _mesh.bounds;
                default:
                    if (Application.isEditor)
                    {
                        Debug.LogWarning($"未能计算 {collider.gameObject.name} : {collider.GetType().Name} 的本地包围盒");
                    }
                    return new Bounds(Vector3.zero, Vector3.zero);
            }
        }


        #region calculate world bounds
        // unsafe code...
        public unsafe static void GetBoundsVerticesUnsafe(this in Bounds b, in Vector3* vector8)
        {
            MinMax3 bo = b;
            Vector3 min = bo.min, max = bo.max;
            float minX = min.x; float maxX = max.x;
            float minY = min.y; float maxY = max.y;
            float minZ = min.z; float maxZ = max.z;

            for (int i = 0; i < 8; i++)
            {
                float* itemHead = (float*)&vector8[i];
                itemHead[0] = i < 4 ? minX : maxX;
                itemHead[1] = (i / 2) % 2 == 0 ? minY : maxY;
                itemHead[2] = i % 2 == 0 ? minZ : maxZ;
                //vector8[i] = new Vector3(i < 4 ? minX : maxX, (i / 2) % 2 == 0 ? minY : maxY, i % 2 == 0 ? minZ : maxZ);
            }
        }

        public unsafe static Bounds ToWorldBounds(this in Bounds localBounds, Transform trans)
            => localBounds.Mul(trans.localToWorldMatrix);
        #endregion

        public static unsafe Bounds MinMaxToBounds(in Vector3* min, in Vector3* max)
        {
            Bounds o = default;
            MinMaxToBounds(min, max, &o);
            return o;
        }

        public static unsafe void MinMaxToBounds(in Vector3* min, in Vector3* max, in Bounds* boundsHead)
        {
            /* Bounds:
             *   Vector3 m_Center;
             *   Vector3 m_Extents;
             */

            Vector3* pCenter = (Vector3*)boundsHead;
            Vector3* pExtends = pCenter + 1;

            float* pMin = (float*)min;
            float* pMax = (float*)max;

            __average3(pMin, pMax, (float*)pCenter);
            Vector3 size = default;
            __minus3(pMax, pMin, (float*)&size);
            *pExtends = size;
            __mul3((float*)pExtends, 0.5f, (float*)pExtends);
        }

        /// <summary>
        /// 变换包围盒的每个点，以变换后的点再次计算包围盒
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="mul"></param>
        /// <returns></returns>
        public unsafe static Bounds Mul(this in Bounds bounds, in Matrix4x4 mul)
        {
            //var vector8 = stackalloc Vector3[8];
            //Vector3 min = Vector3.positiveInfinity, max = Vector3.negativeInfinity;
            //GetBoundsVerticesUnsafe(bounds, vector8);
            //for (int i = 0; i < 8; i++)
            //{
            //    Vector3 point = mul.MultiplyPoint3x4(vector8[i]);
            //    min = Vector3.Min(min, point);
            //    max = Vector3.Max(max, point);
            //}
            //return new Bounds(Vector3.Lerp(min, max, 0.5f), max - min);


            var vector8 = stackalloc Vector3[8];
            GetBoundsVerticesUnsafe(bounds, vector8);

            MinMax3 o = MinMax3.negativeInfinity;
            float* min = (float*)&o;
            float* max = min + 3;
            for (int i = 0; i < 8; i++)
            {
                Vector3 point = default;
                MultiplyPoint3x4(mul, vector8[i], ref point);

                float* p = (float*)&point;
                __min3(min, p, min);
                __max3(max, p, max);
            }
            return o.AsBounds();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void MultiplyPoint3x4(in Matrix4x4 mul, in Vector3 point, ref Vector3 result)
        {
            result.x = mul.m00 * point.x + mul.m01 * point.y + mul.m02 * point.z + mul.m03;
            result.y = mul.m10 * point.x + mul.m11 * point.y + mul.m12 * point.z + mul.m13;
            result.z = mul.m20 * point.x + mul.m21 * point.y + mul.m22 * point.z + mul.m23;
        }

#pragma warning disable IDE1006 // 命名样式
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static unsafe void __min3(float* a, float* b, float* o)
        {
            o[0] = a[0] < b[0] ? a[0] : b[0];
            o[1] = a[1] < b[1] ? a[1] : b[1];
            o[2] = a[2] < b[2] ? a[2] : b[2];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static unsafe void __max3(float* a, float* b, float* o)
        {
            o[0] = a[0] > b[0] ? a[0] : b[0];
            o[1] = a[1] > b[1] ? a[1] : b[1];
            o[2] = a[2] > b[2] ? a[2] : b[2];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static unsafe void __average3(float* a, float* b, float* o)
        {
            o[0] = (a[0] + b[0]) * 0.5f;
            o[1] = (a[1] + b[1]) * 0.5f;
            o[2] = (a[2] + b[2]) * 0.5f;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static unsafe void __add3(float* a, float* b, float* o)
        {
            o[0] = a[0] + b[0];
            o[1] = a[1] + b[1];
            o[2] = a[2] + b[2];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static unsafe void __minus3(float* a, float* b, float* o)
        {
            o[0] = a[0] - b[0];
            o[1] = a[1] - b[1];
            o[2] = a[2] - b[2];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static unsafe void __mul3(float* a, float b, float* o)
        {
            o[0] = a[0] * b;
            o[1] = a[1] * b;
            o[2] = a[2] * b;
        }
#pragma warning restore IDE1006 // 命名样式

        [StructLayout(LayoutKind.Sequential)]
        struct MinMax3
        {
            public Vector3 min;
            public Vector3 max;

            public static readonly MinMax3 negativeInfinity = (Vector3.positiveInfinity, Vector3.negativeInfinity);

            public void Deconstructor(out Vector3 min, out Vector3 max)
            {
                min = this.min;
                max = this.max;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static unsafe MinMax3 FromBounds(in Bounds* pb)
            {
                float* boundsHead = (float*)pb;
                Vector3* pCenter = (Vector3*)boundsHead;
                Vector3* pExtends = pCenter + 1;
                MinMax3 o = default;
                Vector3* oHead = (Vector3*)&o;
                __minus3((float*)pCenter, (float*)pExtends, (float*)oHead);
                __add3((float*)pCenter, (float*)pExtends, (float*)(oHead + 1));
                return o;
            }

            public static implicit operator MinMax3(in (Vector3 min, Vector3 max) b) => new MinMax3
            {
                min = b.min,
                max = b.max
            };

            /* Bounds:
             *   Vector3 m_Center;
             *   Vector3 m_Extents;
             */

            public static unsafe implicit operator MinMax3(Bounds b) => FromBounds(&b);

            public static implicit operator Bounds(in MinMax3 self) => self.AsBounds();

            public unsafe Bounds AsBounds()
            {
                //Bounds o = default;
                //o.min = min;
                //o.max = max;
                //return o;


                fixed (MinMax3* pSelf = &this)
                {
                    Vector3* head = (Vector3*)pSelf;
                    return MinMaxToBounds(head, head + 1);
                }
            }

            public unsafe void SetMinMax(Vector3 p)
            {
                // 4238 times, 1.0 ms
                float* pp = (float*)&p;
                fixed (MinMax3* pSelf = &this)
                {
                    Vector3* head = (Vector3*)pSelf;
                    float* pMin = (float*)head;
                    float* pMax = (float*)(head + 1);
                    __min3(pMin, pp, pMin);
                    __max3(pMax, pp, pMax);
                }

                // 4238 times, 4.03 ms
                //min = Vector3.Min(min, p);
                //max = Vector3.Max(max, p);
            }
        }

        public static Bounds Encapsulate(this in Bounds self, in Vector3 a, in Vector3 b)
        {
            MinMax3 o = self;
            o.SetMinMax(a);
            o.SetMinMax(b);
            return o.AsBounds();
        }

        /// <summary>
        /// 比 <see cref="Bounds.Encapsulate(Bounds)"/> 更快，需要接收返回值
        /// </summary>
        /// <param name="self"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static unsafe Bounds EncapsulateBounds(this in Bounds self, in Bounds b)
        {
            //Bounds o = self;
            //o.Encapsulate(b);
            //return o;

            // 2119 times, 3.8 ms
            MinMax3 o = self, i = b;
            o.SetMinMax(i.min);
            o.SetMinMax(i.max);
            return o.AsBounds();
        }
    }
}