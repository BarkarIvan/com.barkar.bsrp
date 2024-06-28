using UnityEngine;

namespace Barkar.BSRP.Passes.Bloom
{
    [System.Serializable]
    public struct BloomSettings
    {
        [Range(1,2)]
        public int Downsample;
        [Space(5)]
        public int BlurPassesCount;
        public float BlurOffset;
        [Space(5)]
        [Range(1, 5)] public float HDRThreshold;
        [Range(0, 1)] public float HDRSoftThreshold;
        [Range(0, 2)] public float Intensity;
        [Space(5)]
        public bool UseLensDirt;
        public Texture2D LensDirtTexture;
        [Range(0, 2)] public float LensDirtIntensity;
    }
}
