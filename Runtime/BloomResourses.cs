using Barkar.BSRP.Passes.Bloom;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace Barkar.BSRP
{
    public class BloomResourses : ContextItem
    {

        public TextureHandle BloomTexture;
        public BloomSettings BloomSettings;
        
        public override void Reset()
        {
            BloomTexture = default;
            BloomSettings = default;

        }
    }
}