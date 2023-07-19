using UnityEngine;

namespace Com.Effect
{
    public abstract class AbsGridLineComponent : MonoBehaviour
    {
        public static readonly Color[] axisColor = new Color[3] { Color.red, Color.green, Color.blue };

        [Range(-1, 5)]
        public int level;
        public abstract void Apply();
    }
}