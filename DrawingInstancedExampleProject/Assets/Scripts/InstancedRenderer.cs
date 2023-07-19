using Com.Culling;
using Com.Rendering;
using UnityEngine;

[RequireComponent(typeof(InstancedMeshRenderToken), typeof(CustomAABBCullingVolume))]
public class InstancedRenderer : MonoBehaviour
{
    [Range(0, 1024)] public int number = 10;
    [Range(0, 1)] public float space = 0.1f;
    [Range(0.01f, 0.3f)] public float columnRadius = 0.05f;
    [Range(0.05f, 1f)] public float columnLength = 1;
    public Color color = Color.white;
    [SerializeField] string dispatherName = "testDispatcher";
    [SerializeField] string dispatherNameLod1 = "testDispatcher_lod1";

    InstancedMeshRenderToken token;
    CustomAABBCullingVolume cullingGroupVolume;
    [SerializeField] bool useLod1 = false;

    /* 此用例展示了绘制实例符号（InstancedMeshRenderToken）和视野剔除组件协同工作的方式。
     * 要使用绘制实例符号，需要先构造绘制实例调度器（InstancedMeshRenderDispatcher），
     * 调度器不会自行构造，需要在初始化阶段从预制体加载。
     * 
     * 绘制实例符号只存储调度器的名称、绘制实例数目和每实例本地变换矩阵的缓冲区，实际的网格和材质存储在调度器，
     * 因此写入调度器名称和绘制数量、读写 InstancedMeshRenderToken.forceRenderingOff 不会带来很大的 CPU 开销。
     */

    private void Awake()
    {
        token = GetComponent<InstancedMeshRenderToken>();
        cullingGroupVolume = GetComponent<CustomAABBCullingVolume>();

        cullingGroupVolume.onBecameVisible.AddListener(Culling_onBecameVisible);
        cullingGroupVolume.onBecameInvisible.AddListener(Culling_onBecameInvisible);
        cullingGroupVolume.onVolumeDisabled.AddListener(Culling_onBecameVisible);
        cullingGroupVolume.lodChanged.AddListener(Culling_lodChanged);
    }

    private void Start()
    {
        Apply();
    }

    [ContextMenu("apply")]
    public unsafe void Apply()
    {
        /* lod1 的网格使用引擎自带的立方体
         * 这里的处理方式依赖于网格本身尺寸
         * 因为圆柱体的长宽高是 (1, 2, 1)，而立方体的长宽高是 (1, 1, 1)
         * 最后要让它们看起来一样
         * 
         * 实际应用中不同 lod 层级的网格长宽高应该完全一致
         */
        float virtualColumnLength = useLod1 ? columnLength : columnLength * 0.5f;
        token.DispatcherName = useLod1
            ? dispatherNameLod1
            : dispatherName;
        token.Count = number;

        Matrix4x4 trs = Matrix4x4.TRS(default,
            Quaternion.Euler(0, 0, 90),
            new Vector3(columnRadius, virtualColumnLength, columnRadius));
        Vector4 transaction = new Vector4(0, 0, 0, 1);
        float step = space + columnRadius * 2;
        float z = columnRadius;
        for (int i = 0; i < number; i++)
        {
            transaction.z = z;
            trs.SetColumn(3, transaction);
            token.LocalOffsetRefAt(i) = trs;
            z += step;
        }
        token.ClearLocalOffsetsOutOfCount();
        token.UpdateLocalOffsets();

        token.InstanceColor = color;

        float wholeLength = transaction.z;
        Vector3 center = new Vector3(0, 0, wholeLength * 0.5f);
        Vector3 extents = new Vector3(columnLength * 0.5f, columnRadius, wholeLength * 0.5f);
        Bounds localBounds = new Bounds
        {
            center = center,
            extents = extents
        };

        token.LocalBounds = localBounds;
        cullingGroupVolume.LocalBounds = localBounds;

        token.CheckDispatch();
    }

    void Culling_onBecameVisible() => token.forceRenderingOff = false;
    void Culling_onBecameInvisible() => token.forceRenderingOff = true;

    void Culling_lodChanged(int level)
    {
        useLod1 = level > 1;
        Apply();
    }
}
