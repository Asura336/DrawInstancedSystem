using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Assertions;

#nullable enable
namespace Com.Core
{
    public static class UnityTypeExtensions
    {
        #region Numbers
        public static int ToMillimeter(this float v) => Mathf.RoundToInt(v * 1000);

        /// <summary>
        /// 按某个极小量舍入
        /// </summary>
        /// <param name="self"></param>
        /// <param name="epsilon"></param>
        /// <returns></returns>
        public static float TrimTail(this float self, float epsilon = 1e-6f) => Mathf.Round(self / epsilon) * epsilon;

        public static float Inverse(this float self) => self == 0 ? float.PositiveInfinity : 1f / self;
        #endregion

        #region Vector3 and Quaternion
        public static bool EqualsTol(this in Vector3 a, in Vector3 b, float tol = 1e-5f) =>
            Mathf.Abs(a.x - b.x) < tol && Mathf.Abs(a.y - b.y) < tol && Mathf.Abs(a.z - b.z) < tol;
        public static bool EqualsTol(this in Vector2 a, in Vector2 b, float tol = 1e-5f) =>
            Mathf.Abs(a.x - b.x) < tol && Mathf.Abs(a.y - b.y) < tol;

        public static Vector3 Clamp(this in Vector3 self, float min, float max) =>
            new Vector3(Mathf.Clamp(self.x, min, max), Mathf.Clamp(self.y, min, max), Mathf.Clamp(self.z, min, max));
        public static Vector3 Clamp(this in Vector3 self, Vector3 min, Vector3 max) =>
            new Vector3(Mathf.Clamp(self.x, min.x, max.x), Mathf.Clamp(self.y, min.y, max.y), Mathf.Clamp(self.z, min.z, max.z));
        public static Vector2 Clamp(this in Vector2 self, float min, float max) =>
            new Vector3(Mathf.Clamp(self.x, min, max), Mathf.Clamp(self.y, min, max));
        public static Vector3 Clamp(this in Vector2 self, Vector2 min, Vector2 max) =>
            new Vector3(Mathf.Clamp(self.x, min.x, max.x), Mathf.Clamp(self.y, min.y, max.y));

        /// <summary>
        /// 向量的曼哈顿长度，值为各成员的绝对值之和
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        public static float Manhattan(this in Vector3 a) => Mathf.Abs(a.x) + Mathf.Abs(a.y) + Mathf.Abs(a.z);
        /// <summary>
        /// 向量的曼哈顿长度，值为各成员的绝对值之和
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        public static float Manhattan(this in Vector2 a) => Mathf.Abs(a.x) + Mathf.Abs(a.y);

        public static Vector3Int ToMillimeter(this in Vector3 v) => Vector3Int.RoundToInt(v * 1000);
        public static Vector2Int ToMillimeter(this in Vector2 v) => Vector2Int.RoundToInt(v * 1000);

        public static Vector3 ToVector3(this in Vector3Int v) => new Vector3(v.x, v.y, v.z);
        public static Vector2 ToVector2(this in Vector2Int v) => new Vector2(v.x, v.y);

        public static Vector3 Inverse(this in Vector3 v) => new Vector3(Inverse(v.x), Inverse(v.y), Inverse(v.z));
        public static Vector2 Inverse(this in Vector2 v) => new Vector3(Inverse(v.x), Inverse(v.y));

        public static Vector3 RoundToEven(this in Vector3 v, int digits = 6) =>
            new Vector3((float)Math.Round(v.x, digits), (float)Math.Round(v.y, digits), (float)Math.Round(v.z, digits));

        public static Vector2 RoundToEven(this in Vector2 v, int digits = 6) =>
            new Vector2((float)Math.Round(v.x, digits), (float)Math.Round(v.y, digits));


        static readonly Quaternion _euler_0_0_180 = Quaternion.Euler(0, 0, 180);
        public static Quaternion LocalIdentity(this in Vector3 forwardDir) =>
            _euler_0_0_180 * Quaternion.LookRotation(forwardDir, Vector3.down);

        /// <summary>
        /// 取多边形重心
        /// </summary>
        /// <param name="vector2s"></param>
        /// <returns></returns>
        public static Vector2 PolygonGravityCenter(this IList<Vector2> vector2s)
        {
            float area = 0;
            Vector2 result = Vector2.zero;
            for (int i = 0; i < vector2s.Count; i++)
            {
                var next = (i + 1) % vector2s.Count;
                var t = (vector2s[next].x * vector2s[i].y
                    - vector2s[next].y * vector2s[i].x) * 0.5f;
                area += t;
                result += (vector2s[i] + vector2s[next]) * (t / 3f);
            }
            result /= area;
            return result;
        }
        #endregion Vector3 and Quaternion

        #region Plane
        /// <summary>
        /// 射线与平面相交，取相交点
        /// </summary>
        /// <param name="self"></param>
        /// <param name="ray"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static bool RaycastPoint(this in Plane self, in Ray ray, out Vector3 point)
        {
            var cast = self.Raycast(ray, out var _enter);
            point = ray.GetPoint(_enter);
            return cast;
        }
        #endregion

        #region bounds
        /// <summary>
        /// AABB 包围盒的所有顶点坐标
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="point8"></param>
        public static unsafe void GetBoundsVerticesNonAlloc(this Bounds bounds, Vector3[] point8)
        {
            fixed (Vector3* p = point8)
            {
                UnityBoundsExtensions.GetBoundsVerticesUnsafe(bounds, p);
            }
        }
        #endregion

        #region transform
        /// <summary>
        /// 获得以另一节点为参考系的本地变换
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        public static Matrix4x4 GetLocalTrsOf(this Transform transform, Transform origin)
        {
            if (transform)
            {
                if (origin)
                {
                    if (transform.IsChildOf(origin))
                    {
                        Matrix4x4 matrix = Matrix4x4.identity;
                        for (var t = transform; t != origin; t = t.parent)
                        {
                            matrix *= Matrix4x4.TRS(t.localPosition, t.localRotation, t.localScale);
                        }
                        return matrix;
                    }
                    else
                    {
                        return origin.worldToLocalMatrix * transform.localToWorldMatrix;
                    }
                }
                else
                {
                    return transform.localToWorldMatrix;
                }
            }
            return Matrix4x4.identity;
        }
        #endregion

        #region Singleton Script
        /// <summary>
        /// 检查脚本为单例，用于初始化阶段。
        /// 编辑器调试时脚本实例超过一个会报错。
        /// 返回检查结果，只有一个脚本实例返回真。
        /// </summary>
        /// <typeparam name="T">自定义脚本的类型</typeparam>
        /// <param name="self">脚本中的 <see langword="this"/></param>
        public static bool SingletonCheck<T>(this T self) where T : MonoBehaviour
        {
            var result = GameObject.FindObjectOfType<T>() == self;
            Assert.IsTrue(result, $"在 {self.name} 节点出现重复的 {typeof(T)} 实例");
            return result;
        }

        /// <summary>
        /// 检查单例脚本或者为脚本构造不会自动销毁的节点，用于初始化阶段。
        /// 通常返回值赋予脚本中名为 "Current" 的公开静态属性
        /// </summary>
        /// <typeparam name="T">自定义脚本的类型</typeparam>
        /// <param name="self">可以传入任何一个实例，为空也行，不会读写这个参数，仅用于扩展方法的形式</param>
        /// <param name="nodeName">如果构造了节点，指定节点的名字，不指定时为脚本类名</param>
        /// <returns></returns>
#pragma warning disable IDE0060 // 删除未使用的参数
        public static T CurrentNode<T>(this T? self, string? nodeName = null) where T : MonoBehaviour
#pragma warning restore IDE0060 // 删除未使用的参数
        {
            var instance = GameObject.FindObjectOfType<T>() ??
                new GameObject(string.IsNullOrEmpty(nodeName) ? typeof(T).Name : nodeName).AddComponent<T>();
            if (instance && !instance.transform.parent) { GameObject.DontDestroyOnLoad(instance.gameObject); }
            return instance;
        }
        #endregion

        #region Component check
        /// <summary>
        /// <code>
        /// var obj = src.Maybe(src = getter());
        /// </code>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static T Maybe<T>(this T a, T b) where T : Component => a ? a : b;
        #endregion

        #region string
        public static bool CustomStartsWith(this string a, string b)
        {
            int aLen = a.Length;
            int bLen = b.Length;
            int ap = 0; int bp = 0;
            while (ap < aLen && bp < bLen && a[ap] == b[bp])
            {
                ++ap;
                ++bp;
            }
            return bp == bLen;
        }

        public static bool CustomEndsWith(this string a, string b)
        {
            int ap = a.Length - 1;
            int bp = b.Length - 1;

            while (ap >= 0 && bp >= 0 && a[ap] == b[bp])
            {
                ap--;
                bp--;
            }

            return bp < 0;
        }
        #endregion

        #region base64
        public static string ConvertToBase64Str(this Texture texture)
        {
            string b64;
            RenderTexture rt = RenderTexture.GetTemporary(texture.width, texture.height);
            {
                Graphics.Blit(texture, rt);
                var prevRT = RenderTexture.active;
                {
                    Texture2D t2d = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
                    t2d.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                    b64 = Convert.ToBase64String(t2d.EncodeToPNG());
                }
                RenderTexture.active = prevRT;
            }
            RenderTexture.ReleaseTemporary(rt);
            return b64;
        }
        #endregion
    }
}
#nullable restore