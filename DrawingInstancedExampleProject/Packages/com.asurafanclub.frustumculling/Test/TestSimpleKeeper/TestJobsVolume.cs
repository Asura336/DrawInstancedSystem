using UnityEngine;

namespace Com.Culling.Test
{
    public class TestJobsVolume : MonoBehaviour
    {
        JobsAABBCullingVolume volume;
        Renderer m_renderer;

        private void Awake()
        {
            volume = GetComponent<JobsAABBCullingVolume>();
            m_renderer = GetComponentInChildren<Renderer>();

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