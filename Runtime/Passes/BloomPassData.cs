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
        
    }

    public struct BloomData
    {
        public TextureHandle BloomTexture;
        public Texture2D LensDirtTexture;
        public bool UseLensDirtTexture;
        public Vector4 BloomParams;

        public BloomData(TextureHandle bloomTexture, Texture2D lensDirtTexture, bool useLensDirtTexture,Vector4 bloomParams)
        {
            BloomTexture = bloomTexture;
            LensDirtTexture = lensDirtTexture;
            UseLensDirtTexture = useLensDirtTexture;
            BloomParams = bloomParams;
        }

    }
    
}
