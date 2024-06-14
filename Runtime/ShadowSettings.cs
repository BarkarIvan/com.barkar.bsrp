using UnityEngine;

namespace Barkar.BSRP.Settings.Shadows
{
    [System.Serializable]
    public class ShadowSettings 
    {

        public enum ShadowMapSize
        {
            _256 = 256,
            _512 = 512,
            _1024 = 1024,
            _2048 = 2048,
            _4096 = 4096
        }

        [Min(0.001f)] public float ShadowDistance = 1f;

        [System.Serializable]
        public struct DirectionalShadowSettings
        {
            public ShadowMapSize MapSize;
        }

        public DirectionalShadowSettings Direcrional = new()
        {

            MapSize = ShadowMapSize._1024
        };
    }
}
