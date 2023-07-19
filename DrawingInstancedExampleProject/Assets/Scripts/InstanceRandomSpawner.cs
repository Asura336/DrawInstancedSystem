using System.Collections;
using Com.Rendering;
using UnityEngine;

public class InstanceRandomSpawner : MonoBehaviour
{
    public InstancedRenderer prefab;
    public int count = 2500;

    private IEnumerator Start()
    {
        var instantiateRoot = transform;
        float xstep = 2.5f, zstep = 10;
        float xmax = 200;
        float pointX = 0, pointZ = 0;
        for (int i = 0; i < count; i++)
        {
            var o = Instantiate(prefab, instantiateRoot);
            o.transform.localPosition = new Vector3(pointX, Random.Range(-0.5f, 0.5f), pointZ);
            o.color = Random.ColorHSV();
            o.space = Random.Range(0.05f, 0.2f);
            o.number = Random.Range(13, 120);

            pointX += xstep;
            if (pointX > xmax)
            {
                pointX = 0;
                pointZ += zstep;
            }

            if (i % 100 == 99)
            {
                yield return null;
            }
        }
    }

    [ContextMenu("used memory")]
    public void PrintUsedMemory()
    {
        Debug.Log($"Native: {InstancedMeshRenderDispatcher.GetNativeUsedMemory()}");
    }

    [ContextMenu("trim excess")]
    public void CallTrimExcess()
    {
        InstancedMeshRenderDispatcher.TrimExcess();
    }
}
