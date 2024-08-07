using UnityEngine.Rendering.RenderGraphModule;

namespace Barkar.BSRP.CameraRenderer
{
    public readonly ref struct RenderDestinationTextures
    {
        public readonly TextureHandle ColorAttachment0;
        public readonly TextureHandle ColorAttachment1;
        public readonly TextureHandle ColorAttachment2;
        public readonly TextureHandle ColorAttachment3;

        public readonly TextureHandle DepthAttachment;
        public readonly TextureHandle DepthAttachmentCopy;

        public RenderDestinationTextures(TextureHandle colorAttachment0, TextureHandle colorAttachment1, TextureHandle colorAttachment2, TextureHandle colorAttachment3, TextureHandle depthAttachment, TextureHandle depthAttachmentCopy)
        {
            ColorAttachment0 = colorAttachment0;
            ColorAttachment1 = colorAttachment1;
            ColorAttachment2 = colorAttachment2;
            ColorAttachment3 = colorAttachment3;
            DepthAttachment = depthAttachment;
            DepthAttachmentCopy = depthAttachmentCopy;

        }
    }
}
  
