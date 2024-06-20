using System.Net.Mime;


namespace Barkar.BSRP.Passes.Data
{
    public class DrawGeometryPassData
    {
        public UnityEngine.Rendering.RenderGraphModule.TextureHandle ColorAttachment;
        public UnityEngine.Rendering.RenderGraphModule.TextureHandle DepthAttachment;
        public UnityEngine.Rendering.RenderGraphModule.RendererListHandle RendererListHandle;
    }
}