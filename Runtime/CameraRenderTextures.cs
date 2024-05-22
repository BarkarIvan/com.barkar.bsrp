
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace Barkar.BSRP.CameraRenderer
{
    public readonly ref struct CameraRenderTextures
    {
        public readonly TextureHandle ColorAttachment;
        public readonly TextureHandle DepthAttachment;
        public readonly TextureHandle ColorCopy;
        public readonly TextureHandle DepthCopy;

        public CameraRenderTextures(TextureHandle colorAttachment, TextureHandle depthAttachment, TextureHandle colorCopy,
            TextureHandle depthCopy)
        {
            ColorAttachment = colorAttachment;
            DepthAttachment = depthAttachment;
            ColorCopy = colorCopy;
            DepthCopy = depthCopy;
        }
    }
}
  
