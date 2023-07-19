using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

namespace Com.Rendering
{
    internal static class DrawInstancedSystemTools
    {
        public const int sizeofFloat4 = sizeof(float) * 4;
        public const int sizeofFloat3x4 = sizeof(float) * 12;
        public const int sizeofFloat4x4 = sizeof(float) * 16;
        public static readonly int id_Color = Shader.PropertyToID("_Color");
        public static readonly int id_Colors = Shader.PropertyToID("_Colors");
        public static readonly int id_Matrices = Shader.PropertyToID("_Matrices");


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void SetData<T>(ComputeBuffer dst, NativeArray<T> src, int length, int sizeT) where T : unmanaged
        {
            var dstHandle = dst.BeginWrite<T>(0, length);
            UnsafeUtility.MemCpy(dstHandle.GetUnsafePtr(), src.GetUnsafeReadOnlyPtr(), length * sizeT);
            dst.EndWrite<T>(length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetData(ComputeBuffer dst, NativeArray<float3x4> src, int length)
        {
            SetData(dst, src, length, sizeofFloat3x4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetData(ComputeBuffer dst, NativeArray<float4x4> src, int length)
        {
            SetData(dst, src, length, sizeofFloat4x4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetData(ComputeBuffer dst, NativeArray<float4> src, int length)
        {
            SetData(dst, src, length, sizeofFloat4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Erase<T>(NativeList<T> buffer, int index, int last) where T : unmanaged
        {
            //buffer[index] = buffer[last];
            T* ptr = (T*)buffer.GetUnsafePtr();
            ptr[index] = ptr[last];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe long UsedMemory<T>(NativeList<T> list) where T : unmanaged
        {
            return list.IsCreated ? 0 : sizeof(T) * list.Capacity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe long UsedMemory(ComputeBuffer buffer)
        {
            return buffer is null ? 0 : buffer.count * buffer.stride;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void CombineHash(int* seed, int hashValue)
        {
            // boost combine hash
            // seed ^= hash_value(v) + 0x9e3779b9 + (seed << 6) + (seed >> 2);
            uint useed = *(uint*)seed;
            uint v = *(uint*)&hashValue + 0x9e3779b9 + (useed << 6) + (useed >> 2);
            *seed ^= *(int*)&v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool EqualsBounds(Bounds* a, Bounds* b)
        {
            ulong* pa = (ulong*)a, pb = (ulong*)b;
            // Bounds 相当于 6 个 float
            for (int i = 0; i < 3; i++)
            {
                if (pa[i] != pb[i]) { return false; }
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EqualsColor(in Color lhs, in Color rhs)
        {
            float num = lhs.r - rhs.r;
            float num2 = lhs.g - rhs.g;
            float num3 = lhs.b - rhs.b;
            float num4 = lhs.a - rhs.a;
            float num5 = num * num + num2 * num2 + num3 * num3 + num4 * num4;
            return num5 < 9.99999944E-11f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Realloc<T>(ref T[] dst, int size)
        {
            if (dst is null) { dst = new T[size]; }
            else if (dst.Length != size) { Array.Resize(ref dst, size); }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MultiplyPoint3x4(this in Matrix4x4 mul, in Vector3 point, ref Vector3 result)
        {
            result.x = mul.m00 * point.x + mul.m01 * point.y + mul.m02 * point.z + mul.m03;
            result.y = mul.m10 * point.x + mul.m11 * point.y + mul.m12 * point.z + mul.m13;
            result.z = mul.m20 * point.x + mul.m21 * point.y + mul.m22 * point.z + mul.m23;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static void GetBoundsVerticesUnsafe(this in Bounds b, in Vector3* vector8)
        {
            //Vector3 center = b.center, extents = b.extents;
            //Vector3 min = default, max = default;
            //Minus3((float*)&center, (float*)&extents, (float*)&min);
            //Plus3((float*)&center, (float*)&extents, (float*)&max);
            Vector3 min = b.min, max = b.max;

            float minX = min.x; float maxX = max.x;
            float minY = min.y; float maxY = max.y;
            float minZ = min.z; float maxZ = max.z;

            vector3((float*)vector8, min.x, min.y, min.z);
            vector3((float*)(vector8 + 1), min.x, min.y, max.z);
            vector3((float*)(vector8 + 2), min.x, max.y, min.z);
            vector3((float*)(vector8 + 3), min.x, max.y, max.z);
            vector3((float*)(vector8 + 4), max.x, min.y, min.z);
            vector3((float*)(vector8 + 5), max.x, min.y, max.z);
            vector3((float*)(vector8 + 6), max.x, max.y, min.z);
            vector3((float*)(vector8 + 7), max.x, max.y, max.z);

            static unsafe void vector3(float* o, float x, float y, float z)
            {
                o[0] = x;
                o[1] = y;
                o[2] = z;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Min3(float* lhs, float* rhs, float* result)
        {
            result[0] = lhs[0] < rhs[0] ? lhs[0] : rhs[0];
            result[1] = lhs[1] < rhs[1] ? lhs[1] : rhs[1];
            result[2] = lhs[2] < rhs[2] ? lhs[2] : rhs[2];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Max3(float* lhs, float* rhs, float* result)
        {
            result[0] = lhs[0] > rhs[0] ? lhs[0] : rhs[0];
            result[1] = lhs[1] > rhs[1] ? lhs[1] : rhs[1];
            result[2] = lhs[2] > rhs[2] ? lhs[2] : rhs[2];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Average3(float* a, float* b, float* o)
        {
            o[0] = (a[0] + b[0]) * 0.5f;
            o[1] = (a[1] + b[1]) * 0.5f;
            o[2] = (a[2] + b[2]) * 0.5f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Plus3(float* a, float* b, float* o)
        {
            o[0] = a[0] + b[0];
            o[1] = a[1] + b[1];
            o[2] = a[2] + b[2];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Minus3(float* a, float* b, float* o)
        {
            o[0] = a[0] - b[0];
            o[1] = a[1] - b[1];
            o[2] = a[2] - b[2];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Mul3(float* a, float b, float* o)
        {
            o[0] = a[0] * b;
            o[1] = a[1] * b;
            o[2] = a[2] * b;
        }

        public static int CeilToPow2(int value)
        {
            for (int i = 2; i < int.MaxValue; i <<= 1)
            {
                if (i >= value) { return i; }
            }
            return 1 << 30;
        }

        [BurstCompile(FloatPrecision = FloatPrecision.Standard,
           FloatMode = FloatMode.Fast,
           CompileSynchronously = true)]
        public struct MulTrsJobFor : IJobParallelFor
        {
            /* 计算每批次内所有绘制实例的变换
             */

            public int batchSize;
            [NativeDisableParallelForRestriction]
            [ReadOnly] public NativeArray<float4x4>.ReadOnly batchLocalToWorld;
            [NativeDisableParallelForRestriction]
            [ReadOnly] public NativeArray<bool>.ReadOnly batchLocalToWorldDirty;
            [NativeDisableParallelForRestriction]
            [ReadOnly] public NativeArray<int>.ReadOnly batchCount;
            [ReadOnly] public NativeArray<float4x4>.ReadOnly instLocalOffset;
            [WriteOnly] public NativeList<float4x4>.ParallelWriter instTrs;

            public unsafe void Execute(int index)
            {
                int batchIndex = index / batchSize;
                bool inRange = (index % batchSize) < batchCount[batchIndex];
                if (inRange)
                {
                    if (batchLocalToWorldDirty[batchIndex])
                    {
                        (*instTrs.ListData)[index] = mul(batchLocalToWorld[batchIndex], instLocalOffset[index]);
                    }
                }
                else
                {
                    (*instTrs.ListData)[index] = 0;
                }
            }
        }

        [BurstCompile(FloatPrecision = FloatPrecision.Standard,
           FloatMode = FloatMode.Fast,
           CompileSynchronously = true)]
        public struct TransposeBoundsFor : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float4x4>.ReadOnly localToWorld;
            [ReadOnly] public NativeArray<float3x2>.ReadOnly inputLocalBounds;
            [WriteOnly] public NativeArray<float3x2> outputWorldMinMax;

            public unsafe void Execute(int index)
            {
                float3x2 bounds = inputLocalBounds[index];
                float3 center = bounds.c0, extents = bounds.c1;
                float3 bMin = center - extents, bMax = center + extents;
                float4* ps = stackalloc float4[8];
                // world pos
                ps[0] = float4(bMin.x, bMin.y, bMin.z, 1);
                ps[1] = float4(bMin.x, bMin.y, bMax.z, 1);
                ps[2] = float4(bMin.x, bMax.y, bMin.z, 1);
                ps[3] = float4(bMin.x, bMax.y, bMax.z, 1);
                ps[4] = float4(bMax.x, bMin.y, bMin.z, 1);
                ps[5] = float4(bMax.x, bMin.y, bMax.z, 1);
                ps[6] = float4(bMax.x, bMax.y, bMin.z, 1);
                ps[7] = float4(bMax.x, bMax.y, bMax.z, 1);

                float3 worldMin = float3(float.MaxValue), worldMax = float3(float.MinValue);
                for (int i = 0; i < 8; i++)
                {
                    float3 worldP = mul(localToWorld[index], ps[i]).xyz;
                    worldMin = min(worldMin, worldP);
                    worldMax = max(worldMax, worldP);
                }
                outputWorldMinMax[index] = float3x2(worldMin, worldMax);
            }
        }

        [BurstCompile(FloatPrecision = FloatPrecision.Standard,
          FloatMode = FloatMode.Fast,
          CompileSynchronously = true)]
        public struct BoundsMinMaxJobFor : IJobFor
        {
            [ReadOnly] public NativeArray<float3x2> srcMinMax;
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> minMax2;

            public void Execute(int index)
            {
                float3x2 item = srcMinMax[index];
                float3 pMin = item.c0, pMax = item.c1;
                minMax2[0] = min(minMax2[0], pMin);
                minMax2[1] = max(minMax2[1], pMax);
            }
        }
    }
}