using System.Collections;
using System.Collections.Generic;
using Barkar.BSRP.CameraRenderer;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace Barkar.BSRP.Passes
{
    public class FinalPassData
    {
        public  TextureHandle ColorAttachment;
        public  TextureHandle DepthAttachment;
        public  Material FinalPassMaterial;
        public  Camera Camera;
    }
   
    public class FinalPass
    {
        private static readonly ProfilingSampler FinalPassProfilingSampler = new ProfilingSampler("Final Pass");

        private static readonly int
            sourceTextureID = Shader.PropertyToID("_SourceTexture");
        public static void DrawFinalPass(RenderGraph renderGraph, in RenderDestinationTextures input, Camera camera, Material finalPassMaterial)
        {
            using RenderGraphBuilder builder = renderGraph.AddRenderPass<FinalPassData>(FinalPassProfilingSampler.name,
                out var finalPassData, FinalPassProfilingSampler);

            finalPassData.ColorAttachment = builder.ReadTexture(input.ColorAttachment);
            finalPassData.DepthAttachment = builder.ReadTexture(input.DepthAttachment);
            finalPassData.FinalPassMaterial = finalPassMaterial;
            finalPassData.Camera = camera;
            
            builder.SetRenderFunc((FinalPassData finalPassData, RenderGraphContext context) =>
            {
                var cmd = context.cmd;
                cmd.SetGlobalTexture(sourceTextureID, finalPassData.ColorAttachment);
                cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                cmd.SetViewport(finalPassData.Camera.pixelRect);
                cmd.DrawProcedural(Matrix4x4.identity, finalPassData.FinalPassMaterial, 0, MeshTopology.Triangles, 3);
                context.renderContext.ExecuteCommandBuffer(cmd);
                cmd.Clear();
            });

        }
        
        
    }
}
