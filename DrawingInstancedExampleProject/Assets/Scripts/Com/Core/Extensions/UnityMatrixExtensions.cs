using System.Runtime.CompilerServices;
using UnityEngine;

namespace Com.Core
{
    public static class UnityMatrixExtensions
    {
        /// <summary>
        /// same as <see cref="Matrix4x4.MultiplyPoint3x4(Vector3)"/>, faster
        /// </summary>
        /// <param name="mul"></param>
        /// <param name="point"></param>
        /// <param name="result"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MultiplyPoint3x4(this in Matrix4x4 mul, in Vector3 point, ref Vector3 result)
        {
            result.x = mul.m00 * point.x + mul.m01 * point.y + mul.m02 * point.z + mul.m03;
            result.y = mul.m10 * point.x + mul.m11 * point.y + mul.m12 * point.z + mul.m13;
            result.z = mul.m20 * point.x + mul.m21 * point.y + mul.m22 * point.z + mul.m23;
        }

        /// <summary>
        /// same as <see cref="Matrix4x4"/>'s * operator, faster
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <param name="result"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Mul(this in Matrix4x4 lhs, in Matrix4x4 rhs, ref Matrix4x4 result)
        {
            result.m00 = lhs.m00 * rhs.m00 + lhs.m01 * rhs.m10 + lhs.m02 * rhs.m20 + lhs.m03 * rhs.m30;
            result.m01 = lhs.m00 * rhs.m01 + lhs.m01 * rhs.m11 + lhs.m02 * rhs.m21 + lhs.m03 * rhs.m31;
            result.m02 = lhs.m00 * rhs.m02 + lhs.m01 * rhs.m12 + lhs.m02 * rhs.m22 + lhs.m03 * rhs.m32;
            result.m03 = lhs.m00 * rhs.m03 + lhs.m01 * rhs.m13 + lhs.m02 * rhs.m23 + lhs.m03 * rhs.m33;
            result.m10 = lhs.m10 * rhs.m00 + lhs.m11 * rhs.m10 + lhs.m12 * rhs.m20 + lhs.m13 * rhs.m30;
            result.m11 = lhs.m10 * rhs.m01 + lhs.m11 * rhs.m11 + lhs.m12 * rhs.m21 + lhs.m13 * rhs.m31;
            result.m12 = lhs.m10 * rhs.m02 + lhs.m11 * rhs.m12 + lhs.m12 * rhs.m22 + lhs.m13 * rhs.m32;
            result.m13 = lhs.m10 * rhs.m03 + lhs.m11 * rhs.m13 + lhs.m12 * rhs.m23 + lhs.m13 * rhs.m33;
            result.m20 = lhs.m20 * rhs.m00 + lhs.m21 * rhs.m10 + lhs.m22 * rhs.m20 + lhs.m23 * rhs.m30;
            result.m21 = lhs.m20 * rhs.m01 + lhs.m21 * rhs.m11 + lhs.m22 * rhs.m21 + lhs.m23 * rhs.m31;
            result.m22 = lhs.m20 * rhs.m02 + lhs.m21 * rhs.m12 + lhs.m22 * rhs.m22 + lhs.m23 * rhs.m32;
            result.m23 = lhs.m20 * rhs.m03 + lhs.m21 * rhs.m13 + lhs.m22 * rhs.m23 + lhs.m23 * rhs.m33;
            result.m30 = lhs.m30 * rhs.m00 + lhs.m31 * rhs.m10 + lhs.m32 * rhs.m20 + lhs.m33 * rhs.m30;
            result.m31 = lhs.m30 * rhs.m01 + lhs.m31 * rhs.m11 + lhs.m32 * rhs.m21 + lhs.m33 * rhs.m31;
            result.m32 = lhs.m30 * rhs.m02 + lhs.m31 * rhs.m12 + lhs.m32 * rhs.m22 + lhs.m33 * rhs.m32;
            result.m33 = lhs.m30 * rhs.m03 + lhs.m31 * rhs.m13 + lhs.m32 * rhs.m23 + lhs.m33 * rhs.m33;
        }

        /// <summary>
        /// same as <see cref="Matrix4x4"/>'s * operator
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="vector"></param>
        /// <param name="result"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Mul(this in Matrix4x4 lhs, in Vector4 vector, ref Vector4 result)
        {
            result.x = lhs.m00 * vector.x + lhs.m01 * vector.y + lhs.m02 * vector.z + lhs.m03 * vector.w;
            result.y = lhs.m10 * vector.x + lhs.m11 * vector.y + lhs.m12 * vector.z + lhs.m13 * vector.w;
            result.z = lhs.m20 * vector.x + lhs.m21 * vector.y + lhs.m22 * vector.z + lhs.m23 * vector.w;
            result.w = lhs.m30 * vector.x + lhs.m31 * vector.y + lhs.m32 * vector.z + lhs.m33 * vector.w;
        }

        public static Vector3 GetTranslation(this Matrix4x4 matrix)
        {
            Vector3 column3 = default;
            // m03, m13, m23, m33
            column3[0] = matrix.m03;
            column3[1] = matrix.m13;
            column3[2] = matrix.m23;
            return column3;
        }

        public static void SetTranslation(this ref Matrix4x4 matrix, in Vector3 translation)
        {
            matrix.m03 = translation.x;
            matrix.m13 = translation.y;
            matrix.m23 = translation.z;
        }

        public static void SetTranslation(this ref Matrix4x4 matrix, in Vector4 translation)
        {
            matrix.m03 = translation.x;
            matrix.m13 = translation.y;
            matrix.m23 = translation.z;
        }

        public static bool TryGetRotation(this Matrix4x4 matrix, out Quaternion rotation)
        {
            Vector3 forward = matrix.GetColumn(2);
            if (forward.sqrMagnitude == 0f)
            {
                rotation = Quaternion.identity;
                return false;
            }

            Vector3 up = matrix.GetColumn(1);
            if (up.sqrMagnitude == 0f)
            {
                rotation = Quaternion.LookRotation(forward);
                return false;
            }

            rotation = Quaternion.LookRotation(forward, up);
            return true;
        }

        public static bool TryDecomposeTRS(this Matrix4x4 matrix,
            out Vector3 translation, out Quaternion rotation, out Vector3 scale)
        {
            translation = matrix.GetTranslation();
            scale = matrix.lossyScale;
            return matrix.TryGetRotation(out rotation) && matrix.ValidTRS();
        }
    }
}