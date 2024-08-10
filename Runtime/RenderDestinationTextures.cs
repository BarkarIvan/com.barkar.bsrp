using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace Barkar.BSRP.CameraRenderer
{
    public class RenderDestinationTextures : ContextItem
    {
        public  TextureHandle ColorAttachment0;
        public  TextureHandle ColorAttachment1;
        public  TextureHandle ColorAttachment2;
        public  TextureHandle ColorAttachment3;

        public  TextureHandle DepthAttachment;
        public  TextureHandle DepthAttachmentCopy;
        public TextureHandle LightTextureCopy;

      
        public RenderDestinationTextures()
        {
            ColorAttachment0 = default;
            ColorAttachment1 = default;
            ColorAttachment2 = default;
            ColorAttachment3 = default;
            DepthAttachment = default;
            DepthAttachmentCopy = default;
            LightTextureCopy = default;
        }

        public override void Reset()
        {
            
        }
        
    }
}  
