using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;

namespace Barkar.BSRP.Data   
{
    public class BloomPassData
    {
        const int kMaxBlurPassCount = 8;
        public TextureHandle ColorSource;
        public TextureHandle BloomPassTexture;
        public TextureHandle[] BlurPyramid = new TextureHandle[kMaxBlurPassCount];
        public Material BloomMaterial;
        public Material CompositingMaterial;
        public Texture2D LensDirtTexture;

        public bool UseLensDirt;
        public bool BloomEnable;
        public int BlurPassesCount;
        public float BlurPassOffset;
        
        public Vector4 BloomParams = Vector4.one;
        public Vector4 Prefilter;
        public Vector2Int OriginalSize;
    }

    
    
}
