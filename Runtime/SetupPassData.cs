using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace Barkar.BSRP.Passes.Data
{
     class SetupPassData
     {
         public TextureHandle ColorAttachment;
         public TextureHandle DepthAttachment;
         public CameraClearFlags CameraClearFlags;
         public Vector2Int AttachmentSize;
     }
}
