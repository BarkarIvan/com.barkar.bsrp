using Barkar.BSRP.CameraRenderer;
using Barkar.BSRP.Passes.Data;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace Barkar.BSRP.Passes
{
    public class DrawSkyboxPass
    {
        private static readonly ProfilingSampler SkyboxProfilingSampler = new ProfilingSampler("Draw Skybox Pass");

        private BaseRenderFunc<DrawSkyboxPassData, RenderGraphContext> _renderFunc;
        private Camera _camera;

        public DrawSkyboxPass()
        {
            _renderFunc = RenderFunction;
        }

        public void DrawSkybox(RenderGraph renderGraph, in ContextContainer input, Camera camera)
        {
            using var builder = renderGraph.AddRenderPass<DrawSkyboxPassData>(SkyboxProfilingSampler.name,
                    out var drawSkyboxPassData,
                    SkyboxProfilingSampler);

            RenderDestinationTextures destinationTextures = input.Get<RenderDestinationTextures>();

            drawSkyboxPassData.ColorAttacment = builder.UseColorBuffer(destinationTextures.ColorAttachment3, 0);
            drawSkyboxPassData.DepthAttachment =
                builder.UseDepthBuffer(destinationTextures.DepthAttachment, DepthAccess.ReadWrite);

            _camera = camera;
            builder.SetRenderFunc(_renderFunc);
        }

        private void RenderFunction(DrawSkyboxPassData data, RenderGraphContext context)
        {
            context.renderContext.ExecuteCommandBuffer(context.cmd);
            context.cmd.Clear();
            var rl = context.renderContext.CreateSkyboxRendererList(_camera);
            context.cmd.DrawRendererList(rl);
        }
    }
}