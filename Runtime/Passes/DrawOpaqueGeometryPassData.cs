using System.Net.Mime;
using UnityEngine.Rendering.RenderGraphModule;


namespace Barkar.BSRP.Passes.Data
{
    public class DrawOpaqueGeometryPassData
    {
        public TextureHandle ColorAttachment0;
        public TextureHandle ColorAttachment1;
        public TextureHandle ColorAttachment2;
        public TextureHandle ColorAttachment3;


        public TextureHandle DepthAttachment;
        public RendererListHandle RendererList;
    }
}