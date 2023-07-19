using System.Collections.Generic;
using UnityEngine;

namespace Com.Input.EventSystem
{
    public static class RaycastUtils
    {
        static readonly RaycastHit[] _sharedHits = new RaycastHit[256];

        public static void RaycastAll(in Ray ray,
            List<RaycastHit> results,
            int layerMask,
            float maxDistance,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            var hitCount = Physics.RaycastNonAlloc(ray, _sharedHits, maxDistance, layerMask, queryTriggerInteraction);
            for (int i = 0; i < hitCount; i++)
            {
                results.Add(_sharedHits[i]);
            }
            if (results.Count != 0)
            {
                results.Sort((a, b) => a.distance.CompareTo(b.distance));
            }
        }
    }
}