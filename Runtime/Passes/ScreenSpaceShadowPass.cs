using Barkar.BSRP.CameraRenderer;
using Barkar.BSRP.Settings.Shadows;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace Barkar.BSRP
{
    
    public class ScreenSpaceShadowPassData
    {
       // public TextureHandle ShadowMap;
        public TextureHandle TargetGBuffer;
        public TextureHandle DepthAttachment;
        public Material ScreenSpaceShadowPassMaterial;
      //  public BufferHandle DirectionalLightDataBuffer;

    }

    
    public class ScreenSpaceShadowPass
    {

        private readonly ProfilingSampler _profilingSampler = new ProfilingSampler("Screen Space Shadow Map");
        private BaseRenderFunc<ScreenSpaceShadowPassData, RenderGraphContext> _renderFunc;
        private ShadowSettings _shadowSettings;

        private Camera _camera;
        
        static readonly GlobalKeyword[] directionalFilterKeywords =
        {
            GlobalKeyword.Create("_SOFT_SHADOWS_LOW"),
            GlobalKeyword.Create("_SOFT_SHADOWS_MEDIUM"),
            GlobalKeyword.Create("_SOFT_SHADOWS_HIGH"),
        };
        
        
        
        public  void SetKeywords(GlobalKeyword[] keywords, int enabledIndex, CommandBuffer cmd)
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

        public void DrawScreenSpaceShadow(RenderGraph renderGraph, in RenderDestinationTextures input, LightingResources lightingResources, ShadowSettings settings, Material screenSpaceShadowMapMaterial, Camera camera)
        {
            using var builder =
                renderGraph.AddRenderPass<ScreenSpaceShadowPassData>(_profilingSampler.name, out var passData,
                    _profilingSampler);

            passData.ScreenSpaceShadowPassMaterial = screenSpaceShadowMapMaterial;
                //passData.ShadowMap = builder.ReadTexture(lightingResources.DirectionalShadowMap);
            passData.TargetGBuffer = builder.UseColorBuffer(input.ColorAttachment3, 0);
           // passData.DirectionalLightDataBuffer = builder.ReadBuffer(lightingResources.DirectionalLightBuffer);
            passData.DepthAttachment = builder.ReadTexture(input.DepthAttachment);
            passData.TargetGBuffer = builder.UseColorBuffer(input.ColorAttachment3, 0);
            builder.ReadBuffer(lightingResources.DirectionalLightBuffer);
            builder.ReadTexture(lightingResources.DirectionalShadowMap);
            _camera = camera;
            _shadowSettings = settings;
            builder.SetRenderFunc(_renderFunc);
        }

        private void RenderFunction(ScreenSpaceShadowPassData data, RenderGraphContext context)
        {
            var cmd = context.cmd;
            
            var mpb = context.renderGraphPool.GetTempMaterialPropertyBlock();
            //prefilter
           
            SetKeywords(directionalFilterKeywords, (int)_shadowSettings.Direcrional.SoftShadows - 1, cmd);

            // mpb.SetTexture(_GBuffer3ID, data.Gbuffer3 );
            mpb.SetTexture("_CameraDepth", data.DepthAttachment);
            
           // cmd.SetViewport(_camera.pixelRect);
            cmd.DrawProcedural(Matrix4x4.identity, data.ScreenSpaceShadowPassMaterial, 0, MeshTopology.Triangles, 3, 1, mpb);
            context.renderContext.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }

    }
}
