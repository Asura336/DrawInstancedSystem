using UnityEngine;

#nullable enable
namespace Com.Input.EventSystem
{
    public struct RaycastResult
    {
        public GameObject? hitTarget;
        public Collider? hitCollider;
        public float distance;

        public Vector3 hitWorldPosition;
        public Vector3 hitWorldNormal;
        public Vector2 screenCoord;

        public bool Valid => hitTarget;

        public static implicit operator RaycastResult(in RaycastHit hit)
        {
            return new RaycastResult
            {
                hitTarget = hit.collider?.gameObject,
                hitCollider = hit.collider,
                distance = hit.distance,
                hitWorldPosition = hit.point,
                hitWorldNormal = hit.normal,
            };
        }
    }
}