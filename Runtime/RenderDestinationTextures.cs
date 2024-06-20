


namespace Barkar.BSRP.CameraRenderer
{
    public readonly ref struct RenderDestinationTextures
    {
        public readonly UnityEngine.Rendering.RenderGraphModule.TextureHandle ColorAttachment;
        public readonly UnityEngine.Rendering.RenderGraphModule.TextureHandle DepthAttachment;
       

        public RenderDestinationTextures(UnityEngine.Rendering.RenderGraphModule.TextureHandle colorAttachment, UnityEngine.Rendering.RenderGraphModule.TextureHandle depthAttachment)
        {
            ColorAttachment = colorAttachment;
            DepthAttachment = depthAttachment;
        }
    }
}
  
