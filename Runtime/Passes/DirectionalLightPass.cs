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

        public void DrawDirectinalLight(RenderGraph renderGraph,
            in RenderDestinationTextures input, Material testfinalPassMaterial)
        {
            using var builder =
                renderGraph.AddRenderPass<DirectionalLightPassData>(_profilingSampler.name, out var passData,
                    _profilingSampler);

            builder.AllowPassCulling(false);

            passData.Gbuffer0 = builder.ReadTexture(input.ColorAttachment0);
            passData.Gbuffer1 = builder.ReadTexture(input.ColorAttachment1);
            passData.Gbuffer2 = builder.ReadTexture(input.ColorAttachment2);
            passData.Gbuffer3 = builder.UseColorBuffer(input.ColorAttachment3, 0);
            passData.DepthAttachment = builder.UseDepthBuffer(input.DepthAttachment, DepthAccess.Read);
            passData.CameraDepth = builder.ReadTexture(input.DepthAttachmentCopy);

            passData.TestFinalMaterial = testfinalPassMaterial;
            passData.PropertyBlock = new MaterialPropertyBlock();
            
            builder.SetRenderFunc(_renderFunc);
        }

        private void RenderFunction(DirectionalLightPassData data, RenderGraphContext context)
        {
            var cmd = context.cmd;

            data.PropertyBlock.SetTexture(BSRPResources.GBuffer0ID, data.Gbuffer0);
            data.PropertyBlock.SetTexture(BSRPResources.GBuffer1ID, data.Gbuffer1);
            data.PropertyBlock.SetTexture(BSRPResources.GBuffer2ID, data.Gbuffer2);
            data.PropertyBlock.SetTexture(BSRPResources.CameraDepthID, data.CameraDepth);

            cmd.DrawProcedural(Matrix4x4.identity, data.TestFinalMaterial, 0, MeshTopology.Triangles,
                3, 1, data.PropertyBlock);

            context.renderContext.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }
    }
}