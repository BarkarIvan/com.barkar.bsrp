using System.Net.Mime;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace Barkar.BSRP.Passes.Data
{
    public class DrawGeometryPassData
    {
        public TextureHandle ColorAttachment;
        public TextureHandle DepthAttachment;
        public RendererListHandle RendererListHandle;
    }
}