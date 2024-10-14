using Barkar.BSRP.CameraRenderer;
using Barkar.BSRP.Passes.Data;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace Barkar.BSRP.Passes
{
    public class DrawSkyboxPass
    {
        private static readonly ProfilingSampler SkyboxProfilingSampler = new("Draw Skybox Pass");
        private readonly BaseRenderFunc<DrawSkyboxPassData, RasterGraphContext> _renderFunc;
        private Camera _camera;
        public DrawSkyboxPass()
        {
            _renderFunc = RenderFunction;
        }

        public void ExecutePass(RenderGraph renderGraph, in ContextContainer input, Camera camera)
        {
            using var builder = renderGraph.AddRasterRenderPass<DrawSkyboxPassData>(SkyboxProfilingSampler.name,
                out var data,
                SkyboxProfilingSampler);

            _camera = camera;
            var destinationTextures = input.Get<RenderDestinationTextures>();
            
            //TODO DELETE
            var gtaotextures = input.Get<GTAOTexturesItem>();
            builder.UseTexture(gtaotextures.GTAOTExture);
            ///
            
            data.SkyboxRendererList = renderGraph.CreateSkyboxRendererList(camera);
            builder.UseRendererList(data.SkyboxRendererList);
            builder.SetRenderAttachment(destinationTextures.ColorAttachment3, 0);
            builder.SetRenderAttachmentDepth(destinationTextures.DepthAttachment);
            builder.SetRenderFunc(_renderFunc);
            builder.AllowPassCulling(false);
        }

        private void RenderFunction(DrawSkyboxPassData data, RasterGraphContext context)
        {
            context.cmd.DrawRendererList(data.SkyboxRendererList);
        }
    }
}