
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace Barkar.BSRP.CameraRenderer
{
    public readonly ref struct RenderDestinationTextures
    {
        public readonly TextureHandle ColorAttachment;
        public readonly TextureHandle DepthAttachment;
       

        public RenderDestinationTextures(TextureHandle colorAttachment, TextureHandle depthAttachment)
        {
            ColorAttachment = colorAttachment;
            DepthAttachment = depthAttachment;
           
        }
    }
}
  
