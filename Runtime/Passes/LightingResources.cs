

using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace Barkar.BSRP.Passes
{
    public class LightingResources : ContextItem
    {
        public  BufferHandle DirectionalLightBuffer;
        public  TextureHandle DirectionalShadowMap;

        
        public override void Reset()
        {
           
        }
    }
}
