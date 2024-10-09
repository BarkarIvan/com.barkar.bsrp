using UnityEngine;

namespace Barkar.BSRP.Settings.GTAO
{
    [System.Serializable]

    public struct GTAOSettings
    {
        [Range(1f,3f)] public float Intensity;
        [Range(0f,1f)] public float DirLightStrenght;
        [Range(0f, 0.1f)] public float Radius;
        [Range(0, 6)] public int SampleCount;
    }
}