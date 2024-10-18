using UnityEngine.Rendering.RenderGraphModule;

namespace Barkar.BSRP.Passes.Data
{
     class SetupPassData
     {
         public TextureHandle ColorAttachment0; // albedo-roughness
         public TextureHandle ColorAttachment1; // ? -metallic

         public TextureHandle ColorAttachment2; //normals
         public TextureHandle ColorAttachment3; //light Accum

         public TextureHandle DepthAttachment;
     }
}
