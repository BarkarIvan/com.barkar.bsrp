using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;

namespace Barkar.BSRP.Passes.Data   
{
    public class BloomPassData
    {
        const int kMaxBlurPassCount = 8;
        public TextureHandle ColorSource;
        public TextureHandle BloomPassTexture;
        public TextureHandle[] BlurPyramid = new TextureHandle[kMaxBlurPassCount];
        
    }
    
}
