using System;
using Com.Core;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.Mathf;

#nullable enable
namespace Com.Effect
{
    using static GridLineProcedualUtil;

    public class GridLineProcedual : AbsGridLineComponent
    {
        const float originYOffset = 0.02f;

        public Material material = null!;
        public CameraEvent cameraEvent = CameraEvent.BeforeImageEffects;

        Camera targetCamera = null!;
        Color BackGround
        {
            get
            {
                var c = targetCamera.backgroundColor;
                c.a = 0;
                return c;
            }
        }

        GridLineProcedualUtil gridLineDrawer = null!;
        GridLineProcedualUtil gridLineDrawer1 = null!;
        MaterialPropertyBlock props = null!;
        private void Awake()
        {
            targetCamera = GetComponent<Camera>();
            if (!targetCamera)
            {
                throw new NullReferenceException("节点上没有相机");
            }

            gridLineDrawer = new GridLineProcedualUtil("GridLine", material, cameraEvent);
            gridLineDrawer1 = new GridLineProcedualUtil("GridLine1", material, cameraEvent);

            props = new MaterialPropertyBlock();
            props.SetFloat("_UniqueID", UnityEngine.Random.value);
        }

        [Header("声明网格颜色")]
        [SerializeField] Color defColor = new Color32(150, 150, 150, 255);
        [Header("声明更稀疏一级的网格颜色")]
        [SerializeField] Color defNextLevel = new Color32(204, 204, 204, 255);

        Color color = new Color32(150, 150, 150, 255);
        Color nextLevel = new Color32(204, 204, 204, 255);

        [SerializeField] [Range(0f, 1f)] float axisLineAlpha = 1;

        [Header("判断纯色背景的明暗分界点，按饱和度")]
        [Range(0, 1f)]
        [SerializeField] float valueLimit = 0.5f;
        [Header("纯色背景下网格颜色饱和度乘数，暗色背景下调亮网格")]
        [Range(1f, 2f)]
        [SerializeField] float lightMul = 1.25f;
        [Header("纯色背景下网格颜色饱和度乘数，亮色背景下调暗网格")]
        [Range(0, 1f)]
        [SerializeField] float darkMul = 0.9f;

        const int SIZE_OF_MATRIX4x4 = sizeof(float) * 16;
        const int quartStep = 500;
        const int bufferLen = quartStep * 4 + 2;  // 2002
        const int quartStep1 = quartStep / 10;
        const int bufferLen1 = quartStep1 * 4 + 2;  // 2002


        ComputeBuffer _matricesBuffer = null!;
        private void OnEnable()
        {
            gridLineDrawer.Bind(targetCamera, true);
            gridLineDrawer1.Bind(targetCamera, true);
            _matricesBuffer = new ComputeBuffer(bufferLen, SIZE_OF_MATRIX4x4);
            Apply();
        }

        private void OnDisable()
        {
            gridLineDrawer.Bind(targetCamera, false);
            gridLineDrawer1.Bind(targetCamera, false);
            _matricesBuffer?.Dispose();
        }

        Vector3 prevCameraPosition;
        Color prevBackground;
        int prevLevel;
        private void FixedUpdate()
        {
            var currentPos = transform.position;
            var manhattanDelta = (currentPos - prevCameraPosition).Manhattan();
            var limit = ReApplyLimit(out level);

            if (prevLevel != level || manhattanDelta > limit * 0.1f || prevBackground != BackGround)
            {
                prevCameraPosition = currentPos;
                prevBackground = BackGround;
                prevLevel = level;

                var gridSize = limit * 10;  // 取整使用的层级按更大一层
                origin = currentPos;
                origin.y = originYOffset;
                origin.x = origin.x.TrimTail(gridSize);
                origin.z = origin.z.TrimTail(gridSize);

                if (targetCamera.clearFlags == CameraClearFlags.SolidColor)
                {
                    // background => color & nextLevel
                    // 背景是深色时比背景略浅，背景是浅色时比背景略深
                    var hsv = ColorUtil.RGBToHSV(prevBackground);
                    Vector4 hsv_1 = hsv, hsv_2 = hsv;
                    const float fixValue = 0.0875f;
                    hsv.z = Max(hsv.z, fixValue);
                    if (hsv.z < valueLimit)
                    {
                        var _value =
                        hsv_1.z = Clamp(hsv.z + fixValue, fixValue, 1 - fixValue) * lightMul;
                        hsv_2.z = _value * lightMul;
                    }
                    else
                    {
                        var _value =
                        hsv_1.z = Clamp(hsv.z - fixValue, fixValue, 1 - fixValue) * darkMul;
                        hsv_2.z = _value * darkMul;
                    }
                    color = RGBA(ColorUtil.HSVToRGB_Smooth(hsv_1), defColor.a);
                    nextLevel = RGBA(ColorUtil.HSVToRGB_Smooth(hsv_2), defNextLevel.a);
                }
                else
                {
                    color = defColor;
                    nextLevel = defNextLevel;
                }

                Apply();
            }
        }

        [SerializeField] Vector3 origin;
        float ReApplyLimit(out int level)
        {
            if (targetCamera.orthographic)
            {
                var size = targetCamera.orthographicSize;
                // size -> limit
                level = GetLevel(size / 2, 10);
            }
            else
            {
                var absHeight = Abs(targetCamera.transform.position.y);
                // height -> limit
                level = GetLevel(absHeight / 4, 10);
            }
            return Pow10(level);
        }

        static int GetLevel(float size, int maxLevel = 10)
        {
            for (int i = 0; i < maxLevel; i++)
            {
                if (size < Pow10(i))
                {
                    return i - 1;
                }
            }
            return maxLevel;
        }

        public ComputeShader gridLineCompute = null!;
        static readonly int _Matrices = Shader.PropertyToID("_Matrices");
        static readonly int _start = Shader.PropertyToID("start");
        static readonly int _step = Shader.PropertyToID("step");
        static readonly int _trs = Shader.PropertyToID("trs");
        static readonly int _indexOffs = Shader.PropertyToID("indexOffs");
        static readonly int _indexCount = Shader.PropertyToID("indexCount");
        static readonly int _skipWhen = Shader.PropertyToID("skipWhen");
        public override void Apply()
        {
            if (!gridLineCompute)
            {
                throw new NullReferenceException("没有附加计算着色器 GridLineCompute.compute");
            }

            static int getThreadNum(int count) => count / 64 + (count % 64 != 0 ? 1 : 0);
            float farClipPlane = targetCamera.farClipPlane;

            // base
            {
                gridLineDrawer.Clear();

                props.Clear();
                props.SetColor(_Color, color);
                props.SetColor(_FadeColor, BackGround);
                props.SetFloat(_Clip, Min(Pow10(level + 1) * 3, farClipPlane));

                var csHoricontal = gridLineCompute.FindKernel("Horizontal");
                var csVertical = gridLineCompute.FindKernel("Vertical");

                var stepDis = Pow10(level);
                var lineLen = stepDis * quartStep * 2;
                var scl = new Vector3(1, 1, lineLen);
                var skipWhen = stepDis * 10;
                {
                    var start = origin + Vector3.back * (stepDis * quartStep);
                    var step = Vector3.forward * stepDis;
                    var trs = Matrix4x4.TRS(start, ROTATES[0], scl);
                    var count = quartStep * 2 + 1;

                    // cs
                    gridLineCompute.SetBuffer(csHoricontal, _Matrices, _matricesBuffer);
                    gridLineCompute.SetVector(_start, start);
                    gridLineCompute.SetVector(_step, step);
                    gridLineCompute.SetMatrix(_trs, trs);
                    gridLineCompute.SetInt(_indexOffs, 0);
                    gridLineCompute.SetInt(_indexCount, count);
                    gridLineCompute.SetFloat(_skipWhen, skipWhen);
                    gridLineCompute.Dispatch(csHoricontal, getThreadNum(count), 1, 1);
                }
                {
                    // 1001 -> 2002
                    var offs = quartStep * 2 + 1;
                    var start = origin + Vector3.left * (stepDis * quartStep);
                    var step = Vector3.right * stepDis;
                    var trs = Matrix4x4.TRS(start, ROTATES[2], scl);
                    var count = quartStep * 2 + 1;

                    gridLineCompute.SetBuffer(csVertical, _Matrices, _matricesBuffer);
                    gridLineCompute.SetVector(_start, start);
                    gridLineCompute.SetVector(_step, step);
                    gridLineCompute.SetMatrix(_trs, trs);
                    gridLineCompute.SetInt(_indexOffs, offs);
                    gridLineCompute.SetInt(_indexCount, count + offs);
                    gridLineCompute.SetFloat(_skipWhen, skipWhen);
                    gridLineCompute.Dispatch(csVertical, getThreadNum(count), 1, 1);
                }
                props.SetBuffer(_Matrices, _matricesBuffer);
                gridLineDrawer.Append(bufferLen, props);
            }

            // level + 1
            {
                gridLineDrawer1.Clear();

                props.Clear();
                props.SetColor(_Color, nextLevel);
                props.SetColor(_FadeColor, BackGround);
                var _clipDis = Pow10(level + 2) * 2;
                props.SetFloat(_Clip, Min(_clipDis, farClipPlane));

                var csHoricontal = gridLineCompute.FindKernel("HorizontalSkip0");
                var csVertical = gridLineCompute.FindKernel("VerticalSkip0");

                var stepDis = Pow10(level + 1);
                var lineLen = stepDis * quartStep1 * 2;
                var scl = new Vector3(1, 1, lineLen);
                {
                    var start = origin + Vector3.back * (stepDis * quartStep1);
                    var step = Vector3.forward * stepDis;
                    var trs = Matrix4x4.TRS(start, ROTATES[0], scl);
                    var count = quartStep1 * 2 + 1;

                    gridLineCompute.SetBuffer(csHoricontal, _Matrices, _matricesBuffer);
                    gridLineCompute.SetVector(_start, start);
                    gridLineCompute.SetVector(_step, step);
                    gridLineCompute.SetMatrix(_trs, trs);
                    gridLineCompute.SetInt(_indexOffs, 0);
                    gridLineCompute.SetInt(_indexCount, count);
                    gridLineCompute.Dispatch(csHoricontal, getThreadNum(count), 1, 1);
                }
                {
                    // 1001 -> 2002
                    var offs = quartStep1 * 2 + 1;
                    var start = origin + Vector3.left * (stepDis * quartStep1);
                    var step = Vector3.right * stepDis;
                    var trs = Matrix4x4.TRS(start, ROTATES[2], scl);
                    var count = quartStep1 * 2 + 1;

                    gridLineCompute.SetBuffer(csVertical, _Matrices, _matricesBuffer);
                    gridLineCompute.SetVector(_start, start);
                    gridLineCompute.SetVector(_step, step);
                    gridLineCompute.SetMatrix(_trs, trs);
                    gridLineCompute.SetInt(_indexOffs, offs);
                    gridLineCompute.SetInt(_indexCount, count + offs);
                    gridLineCompute.Dispatch(csVertical, getThreadNum(count), 1, 1);
                }

                props.SetBuffer(_Matrices, _matricesBuffer);
                gridLineDrawer1.Append(bufferLen1, props);

                props.Clear();
                props.SetFloat(_Clip, Min(_clipDis, farClipPlane));
                props.SetColor(_Color, RGBA(axisColor[2], axisLineAlpha));
                props.SetColor(_FadeColor, RGBA(axisColor[2], 0));
                var axisLineScl = new Vector3(1, 1, _clipDis * 10);
                gridLineDrawer1.Append(Matrix4x4.TRS(V_ZERO, ROTATES[2], axisLineScl), props);
                props.SetColor(_Color, RGBA(axisColor[0], axisLineAlpha));
                props.SetColor(_FadeColor, RGBA(axisColor[0], 0));
                gridLineDrawer1.Append(Matrix4x4.TRS(V_ZERO, ROTATES[0], axisLineScl), props);
            }
        }

        static Color RGBA(Color col, float alpha)
        {
            col.a = alpha;
            return col;
        }
    }
}