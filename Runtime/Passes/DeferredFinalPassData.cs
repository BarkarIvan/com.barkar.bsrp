using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;

namespace Barkar.BSRP.Passes
{
    public class DeferredFinalPassData
    {
        public TextureHandle GBuffer3;
        public TextureHandle BackBuffer;
        public Material DeferredFinalPassMaterial;
    }
}