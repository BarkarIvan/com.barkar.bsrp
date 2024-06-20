using System.Collections;
using System.Collections.Generic;
using Barkar.BSRP.CameraRenderer;
using Barkar.BSRP.Passes.Data;
using UnityEngine;

using UnityEngine.Rendering;

namespace Barkar.BSRP.Passes
{



    public class DrawSkyboxPass
    {
        private static readonly ProfilingSampler SkyboxProfilingSampler = new ProfilingSampler("Draw Skybox Pass");

        public static void DrawSkybox(UnityEngine.Rendering.RenderGraphModule.RenderGraph renderGraph,
            in RenderDestinationTextures input, Camera camera)
        {
            using (var builder =
                   renderGraph.AddRenderPass<DrawSkyboxPassData>(SkyboxProfilingSampler.name,
                       out var drawSkyboxPassData,
                       SkyboxProfilingSampler))
            {
            

            drawSkyboxPassData.ColorAttacment = builder.ReadWriteTexture(input.ColorAttachment);
            drawSkyboxPassData.DepthAttachment = builder.ReadWriteTexture(input.DepthAttachment);


            builder.SetRenderFunc((DrawSkyboxPassData drawSkyboxPassData,
                UnityEngine.Rendering.RenderGraphModule.RenderGraphContext context) =>
            {
                context.renderContext.ExecuteCommandBuffer(context.cmd);
                context.cmd.Clear();
                var rl = context.renderContext.CreateSkyboxRendererList(camera);
                context.cmd.DrawRendererList(rl);
            });
        }
    }
}
    
    
}
