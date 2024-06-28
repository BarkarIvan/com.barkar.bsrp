using Barkar.BSRP.CameraRenderer;
using UnityEngine;

using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace Barkar.BSRP.Passes
{
    public class FinalPassData
    {
        public  TextureHandle ColorAttachment;
        public  TextureHandle DepthAttachment;
       // public  
        public  Camera Camera;
    }
   
    public class FinalPass
    {
        private static readonly ProfilingSampler FinalPassProfilingSampler = new ("Final Pass");

        private static readonly int
            sourceTextureID = Shader.PropertyToID("_SourceTexture");

        static Material FinalPassMaterial;

        public static void DrawFinalPass(RenderGraph renderGraph,
            in RenderDestinationTextures input, Camera camera, Material finalPassMaterial)
        {
            using (var builder = renderGraph.AddRenderPass<FinalPassData>(FinalPassProfilingSampler.name,
                       out var finalPassData, FinalPassProfilingSampler))
            {
                finalPassData.ColorAttachment = builder.ReadTexture(input.ColorAttachment);
                finalPassData.DepthAttachment = builder.ReadTexture(input.DepthAttachment);
                FinalPassMaterial = finalPassMaterial;
                finalPassData.Camera = camera;
                //var t = builder.ReadTexture(B)
                builder.SetRenderFunc((FinalPassData finalPassData, RenderGraphContext context) =>
                {
                    var cmd = context.cmd;
                    cmd.SetGlobalTexture(sourceTextureID, finalPassData.ColorAttachment);
                    cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, RenderBufferLoadAction.DontCare,
                        RenderBufferStoreAction.Store);
                    cmd.SetViewport(finalPassData.Camera.pixelRect);
                    cmd.DrawProcedural(Matrix4x4.identity, FinalPassMaterial, 0, MeshTopology.Triangles,
                        3);
                    context.renderContext.ExecuteCommandBuffer(cmd);
                    cmd.Clear();
                });
            }
        }


    }
}
