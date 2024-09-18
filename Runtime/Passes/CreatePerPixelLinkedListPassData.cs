using UnityEngine.Rendering.RenderGraphModule;

namespace Barkar.BSRP.Passes
{
    public class CreatePerPixelLinkedListPassData
    {
        public RendererListHandle RendererList;
        public BufferHandle FragmentLinksBuffer;
        public BufferHandle StartOffsetBuffer;
    }
}