using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.Mathf;

#nullable enable
namespace Com.Effect
{
    /// <summary>
    /// 绘制网格线的集成工具
    /// </summary>
    public class GridLineProcedualUtil
    {
        static readonly float[] POW = new float[8] {
            1, 10, 100, 1e3f,
            1e4f, 1e5f, 1e6f, 1e7f
        };
        public static float Pow10(int p) => p > -1 && p < 8 ? POW[p] : Pow(10, p);

        readonly CommandBuffer commandBuffer;
        static Mesh lineMesh = null!;
        static Mesh LineMesh
        {
            get
            {
                if (!lineMesh)
                {
                    lineMesh = new Mesh
                    {
                        name = "line"
                    };
                    lineMesh.SetVertices(new Vector3[] { new Vector3(0, 0, -0.5f), new Vector3(0, 0, 0.5f) });
                    lineMesh.SetIndices(new int[] { 0, 1 }, MeshTopology.Lines, 0);
                }
                return lineMesh;
            }
        }
        GridLineProcedualUtil(string name)
        {
            commandBuffer = new CommandBuffer
            {
                name = name
            };
        }

        readonly CameraEvent bindCameraEvent = CameraEvent.BeforeImageEffects;
        GridLineProcedualUtil(string name, CameraEvent cameraEvent) : this(name)
        {
            bindCameraEvent = cameraEvent;
        }

        readonly Material? sharedMaterial;
        public static readonly int _Color = Shader.PropertyToID("_Color");
        public static readonly int _FadeColor = Shader.PropertyToID("_FadeColor");
        public static readonly int _Clip = Shader.PropertyToID("_Clip");
        public static readonly int _Matrices = Shader.PropertyToID("_Matrices");
        public static readonly int _InstanceColors = Shader.PropertyToID("_InstanceColors");

        public GridLineProcedualUtil(string name, Material material, CameraEvent cameraEvent) : this(name, cameraEvent)
        {
            sharedMaterial = material;
            material.enableInstancing = true;
        }

        public static readonly Vector3 V_ZERO = Vector3.zero;
        public static readonly Matrix4x4 M_ZERO = Matrix4x4.zero;
        /// <summary>
        /// right, left, forward, back
        /// </summary>
        public static readonly Vector3[] AXIES = new Vector3[4] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back };
        /// <summary>
        /// right, left, forward, back
        /// </summary>
        public static readonly Quaternion[] ROTATES = new Quaternion[4] { Quaternion.Euler(0, 90, 0), Quaternion.Euler(0, -90, 0), Quaternion.identity, Quaternion.Euler(0, 180, 0) };

        public void Bind(in Camera targetCamera, bool enable)
        {
            if (!targetCamera)
            {
#if UNITY_EDITOR
                Debug.LogError($"Reference of param \"{nameof(targetCamera)}\" is null");
#endif
                return;
            }
            if (enable) { targetCamera.AddCommandBuffer(bindCameraEvent, commandBuffer); }
            else { targetCamera.RemoveCommandBuffer(bindCameraEvent, commandBuffer); }
        }

        public void Append(in Matrix4x4 matrix, MaterialPropertyBlock props)
        {
            commandBuffer.DrawMesh(LineMesh, matrix, sharedMaterial, 0, 0, props);
        }

        public void Append(int count, MaterialPropertyBlock props)
        {
            commandBuffer.DrawMeshInstancedProcedural(LineMesh, 0, sharedMaterial, 0, count, props);
        }

        public void Clear() => commandBuffer.Clear();
    }
}