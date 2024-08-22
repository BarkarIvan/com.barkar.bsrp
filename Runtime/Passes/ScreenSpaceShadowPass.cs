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
        private BaseRenderFunc<ScreenSpaceShadowPassData, RenderGraphContext> _renderFunc;
        private ShadowSettings _shadowSettings;

        static readonly GlobalKeyword[] directionalFilterKeywords =
        {
            GlobalKeyword.Create("_SOFT_SHADOWS_LOW"),
            GlobalKeyword.Create("_SOFT_SHADOWS_MEDIUM"),
            GlobalKeyword.Create("_SOFT_SHADOWS_HIGH"),
        };

        public void SetKeywords(GlobalKeyword[] keywords, int enabledIndex, CommandBuffer cmd)
        {
            for (int i = 0; i < keywords.Length; i++)
            {
                cmd.SetKeyword(keywords[i], i == enabledIndex);
            }
        }

        public ScreenSpaceShadowPass()
        {
            _renderFunc = RenderFunction;
        }

        public void DrawScreenSpaceShadow(RenderGraph renderGraph, in ContextContainer input,
            in LightingResources lightingResources, ShadowSettings settings, Material screenSpaceShadowMapMaterial)
        {
            using var builder =
                renderGraph.AddRenderPass<ScreenSpaceShadowPassData>(_profilingSampler.name, out var data,
                    _profilingSampler);

            RenderDestinationTextures destinationTextures = input.Get<RenderDestinationTextures>();

            data.ScreenSpaceShadowPassMaterial = screenSpaceShadowMapMaterial;
            data.TargetGBuffer = builder.UseColorBuffer(destinationTextures.ColorAttachment3, 0);
            data.CameraDepth = builder.ReadTexture(destinationTextures.DepthAttachmentCopy);
            data.TargetGBuffer = builder.UseColorBuffer(destinationTextures.ColorAttachment3, 0);
            data.DepthAttachment = builder.UseDepthBuffer(destinationTextures.DepthAttachment, DepthAccess.Read);
            builder.ReadBuffer(lightingResources.DirectionalLightBuffer);
            builder.ReadTexture(lightingResources.DirectionalShadowMap);
            _shadowSettings = settings;

            builder.SetRenderFunc(_renderFunc);
        }

        private void RenderFunction(ScreenSpaceShadowPassData data, RenderGraphContext context)
        {
            var cmd = context.cmd;
            var mpb = context.renderGraphPool.GetTempMaterialPropertyBlock();
            SetKeywords(directionalFilterKeywords, (int)_shadowSettings.Direcrional.SoftShadows - 1, cmd);
            mpb.SetTexture(BSRPShaderIDs.CameraDepthID, data.CameraDepth);

            cmd.DrawProcedural(Matrix4x4.identity, data.ScreenSpaceShadowPassMaterial, 0, MeshTopology.Triangles, 3, 1,
                mpb);

            context.renderContext.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }
    }
}