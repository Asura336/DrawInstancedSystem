using System.Runtime.InteropServices;
using UnityEngine;
using static UnityEngine.Mathf;
using static UnityEngine.Vector3;

namespace Com.Core
{
    /// <summary>
    /// 颜色相关的算法
    /// </summary>
    public static class ColorUtil
    {
        public static float FMod(float x, float y) => x % y;

        public static Vector2 FMod(in Vector2 x, float y) => new Vector2(FMod(x.x, y), FMod(x.y, y));

        public static Vector3 FMod(in Vector3 x, float y) => new Vector3(FMod(x.x, y), FMod(x.y, y), FMod(x.z, y));

        public static Vector3 Abs(in Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

        public static Vector3 Clamp01(in Vector3 v) => new Vector3(Mathf.Clamp01(v.x), Mathf.Clamp01(v.y), Mathf.Clamp01(v.z));

        /// <summary>
        /// https://www.shadertoy.com/view/MsS3Wc
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static Color HSVToRGB_Smooth(in Vector3 c)
        {
            //// Smooth HSV to RGB conversion 
            //// https://www.shadertoy.com/view/MsS3Wc
            //half3 hsv2rgb_smooth(in half3 c)
            //{
            //    half3 rgb = clamp(abs(fmod(c.x * 6.0 + half3(0.0, 4.0, 2.0), 6.0) - 3.0) - 1.0, 0.0, 1.0);
            //    rgb = rgb * rgb * (3.0 - 2.0 * rgb); // cubic smoothin
            //    return c.z * lerp(1, rgb, c.y);
            //}

            var _fmod = FMod(one * (c.x * 6) + new Vector3(0, 4, 2), 6);
            var _abs = Abs(_fmod - one * 3);
            var rgb = Clamp01(_abs - one);
            var rgb_smooth = Scale(Scale(rgb, rgb), one * 3 - rgb * 2);
            Vector4 o = c.z * Lerp(one, rgb_smooth, c.y);
            return o;
        }


        /// <summary>
        /// rgb 颜色到 hsv，alpha 保留。
        /// (x, y, z) = {h, s, v}，分量值域均为 [0, 1]。
        /// https://stackoverflow.com/questions/1335426/is-there-a-built-in-c-net-system-api-for-hsv-to-rgb
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static Vector4 RGBToHSV(in Color color)
        {
            Color32 c32 = color;
            System.Drawing.Color sdc = System.Drawing.Color.FromArgb(c32.a, c32.r, c32.g, c32.b);

            int max = Max(Max(c32.r, c32.g), c32.b);
            int min = Min(Min(c32.r, c32.g), c32.b);

            var _hue = (sdc.GetHue() % 360f) / 360f;
            var _saturation = (max == 0) ? 0 : 1f - (1f * min / max);
            var _value = max / 255f;
            return new Vector4(_hue, _saturation, _value, color.a);
        }


        [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 4)]
        struct Color32
        {
            [FieldOffset(3)]
            public byte r;
            [FieldOffset(2)]
            public byte g;
            [FieldOffset(1)]
            public byte b;
            [FieldOffset(0)]
            public byte a;
            public static implicit operator Color32(in Color c) => new Color32
            {
                r = (byte)(c.r * 255),
                g = (byte)(c.g * 255),
                b = (byte)(c.b * 255),
                a = (byte)(c.a * 255),
            };
            const float inv255 = 1 / 255f;
            public static implicit operator Color(in Color32 c32)
                => new Color(c32.r * inv255, c32.g * inv255, c32.b * inv255, c32.a * inv255);
        }
        /// <summary>
        /// 得到与输入颜色色差尽可能大的颜色，不改变透明度
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static unsafe Color ColorXORWhite(this in Color c)
        {
            const uint _mask = 0xffffff00;
            Color32 i = c;
            uint* p = (uint*)&i;
            uint a = _mask ^ (*p);
            return *(Color32*)&a;
        }
    }
}