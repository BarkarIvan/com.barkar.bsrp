using UnityEngine;

namespace Barkar.BSRP.Passes.Bloom
{
    [CreateAssetMenu(fileName = "Bloom Render Feature Resources", menuName = "ScriptableObjects/Rendering/Bloom Render Feature Resources", order = 2)]
    public class BloomSettings: ScriptableObject
    {
        [Range(1,2)]
        public int Downsample = 1;
        [Space(5)]
        public int BlurPassesCount = 3;
        public float BlurOffset = 3;
        [Space(5)]
        [Range(1, 2)] public float HDRThreshold = 1;
        [Range(0, 2)] public float HDRSoftThreshold = 1;
        [Range(0, 2)] public float Intensity = 1;
        [Space(5)]
        public bool UseLensDirt;
        public Texture2D LensDirtTexture;
        [Range(0, 2)] public float LensDirtIntensity = 1f;
    }
}
