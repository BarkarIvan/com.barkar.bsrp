using Barkar.BSRP.CameraRenderer;
using Barkar.BSRP.Settings.Shadows;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace Barkar.BSRP.Passes
{
    public class ScreenSpaceShadowPass
    {
        private readonly ProfilingSampler _profilingSampler = new ProfilingSampler("Screen Space Shadow Map");
        private BaseRenderFunc<ScreenSpaceShadowPassData, RasterGraphContext> _renderFunc;

        public ScreenSpaceShadowPass()
        {
            _renderFunc = RenderFunction;
        }

        public void ExecutePass(RenderGraph renderGraph, in ContextContainer input,
            ShadowSettings settings, Material screenSpaceShadowMapMaterial)
        {
            using var builder =
                renderGraph.AddRasterRenderPass<ScreenSpaceShadowPassData>(_profilingSampler.name, out var data,
                    _profilingSampler);

            RenderDestinationTextures destinationTextures = input.Get<RenderDestinationTextures>();
            LightingResources lightingResources = input.Get<LightingResources>();
            
            data.ScreenSpaceShadowPassMaterial = screenSpaceShadowMapMaterial;
            builder.SetRenderAttachment(destinationTextures.ColorAttachment3, 0);
            //builder.UseGlobalTexture(BSRPShaderIDs.CameraDepthID, AccessFlags.Read);
           builder.SetRenderAttachmentDepth(destinationTextures.DepthAttachment, AccessFlags.Read);
            builder.UseBuffer(lightingResources.DirectionalLightBuffer);
            //builder.UseTexture(lightingResources.DirectionalShadowMap);
            //builder.UseGlobalTexture(BSRPShaderIDs.MainLightShadowMapID, AccessFlags.Read);
            builder.SetRenderFunc(_renderFunc);
        }

        private void RenderFunction(ScreenSpaceShadowPassData data, RasterGraphContext context)
        {
            context.cmd.DrawProcedural(Matrix4x4.identity, data.ScreenSpaceShadowPassMaterial, 0, MeshTopology.Triangles, 3 );
        }
    }
}