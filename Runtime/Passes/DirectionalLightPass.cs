using Barkar.BSRP.CameraRenderer;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace Barkar.BSRP.Passes
{
    public class DirectionalLightPass
    {
        private readonly ProfilingSampler _profilingSampler = new("Directional lighting");
        private BaseRenderFunc<DirectionalLightPassData, RenderGraphContext> _renderFunc;

        public DirectionalLightPass()
        {
            _renderFunc = RenderFunction;
        }

        public void ExecutePass(RenderGraph renderGraph,
            in ContextContainer input, Material deferredLightsMaterial)
        {
            using var builder =
                renderGraph.AddRenderPass<DirectionalLightPassData>(_profilingSampler.name, out var passData,
                    _profilingSampler);

            builder.AllowPassCulling(false);
            RenderDestinationTextures destinationTextures = input.Get<RenderDestinationTextures>();

            
            passData.Gbuffer3 = builder.UseColorBuffer(destinationTextures.ColorAttachment3, 0);
            passData.DepthAttachment = builder.UseDepthBuffer(destinationTextures.DepthAttachment, DepthAccess.Read);
            passData.CameraDepth = builder.ReadTexture(destinationTextures.DepthAttachmentCopy);

            passData.deferredLightsMaterial = deferredLightsMaterial;

            builder.SetRenderFunc(_renderFunc);
        }

        private void RenderFunction(DirectionalLightPassData data, RenderGraphContext context)
        {
            var cmd = context.cmd;
            
       //     mpb.SetTexture(BSRPShaderIDs.CameraDepthID, data.CameraDepth);

            cmd.DrawProcedural(Matrix4x4.identity, data.deferredLightsMaterial, 0, MeshTopology.Triangles,
                3, 1);

            context.renderContext.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }
    }
}