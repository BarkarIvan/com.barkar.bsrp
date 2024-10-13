using UnityEngine;

namespace Barkar.BSRP.Settings.GTAO
{
    [System.Serializable]

    public struct GTAOSettings
    {
        [Range(1f,10f)] public float Intensity;
        [Range(1f, 5f)] public float Radius;
        [Range(0, 6)] public int SampleCount;
        [Range(0, 1)] public float Thickness;
    }
}