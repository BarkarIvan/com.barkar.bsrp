using UnityEngine;
using UnityEngine.Serialization;

namespace Barkar.BSRP.Settings.GTAO
{
    [System.Serializable]

    public struct GTAOSettings
    {
        [Range(1f,10f)] public float Pow;
        [Range(0f, 5f)] public float Radius;
        [Range(0, 4)] public int SampleCount;
        [Range(0f, 1f)] public float Thickness;
    }
}