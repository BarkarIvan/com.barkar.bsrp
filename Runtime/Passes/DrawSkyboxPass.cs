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

        public DrawSkyboxPass()
        {
            _renderFunc = RenderFunction;
        }

        public void ExecutePass(RenderGraph renderGraph, in ContextContainer input, Camera camera)
        {
            using var builder = renderGraph.AddRasterRenderPass<DrawSkyboxPassData>(SkyboxProfilingSampler.name,
                out var data,
                SkyboxProfilingSampler);

            var destinationTextures = input.Get<RenderDestinationTextures>();
            data.SkyboxRendererList = renderGraph.CreateSkyboxRendererList(camera);
            builder.UseRendererList(data.SkyboxRendererList);
            builder.SetRenderAttachment(destinationTextures.ColorAttachment3, 0);
            builder.SetRenderAttachmentDepth(destinationTextures.DepthAttachment);
            builder.SetRenderFunc(_renderFunc);
        }

        private void RenderFunction(DrawSkyboxPassData data, RasterGraphContext context)
        {
            context.cmd.DrawRendererList(data.SkyboxRendererList);
        }
    }
}