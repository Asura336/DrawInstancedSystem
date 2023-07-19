using UnityEngine;

namespace Com.Culling.Test
{
    public class TestSimpleVolume : MonoBehaviour
    {
        SimpleAABBCullingVolume volume;
        Renderer m_renderer;

        private void Awake()
        {
            volume = GetComponent<SimpleAABBCullingVolume>();
            m_renderer = GetComponent<Renderer>();

            volume.onBecameVisible.AddListener(Volume_onBecameVisible);
            volume.onBecameInvisible.AddListener(Volume_onBecameInvisible);
        }

        void Volume_onBecameVisible()
        {
            m_renderer.enabled = true;
        }

        void Volume_onBecameInvisible()
        {
            m_renderer.enabled = false;
        }
    }
}