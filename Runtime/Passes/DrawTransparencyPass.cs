using Barkar.BSRP.CameraRenderer;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

namespace Barkar.BSRP.Passes
{
    public class DrawTransparencyPassData
    {
        public RendererListHandle RendererList;
        public TextureHandle DestinationTexture;
    }
    
    public class DrawTransparencyPass
    {
        private readonly ProfilingSampler _profilingSampler = new("Draw Transparent Objects");

        private RendererListDesc _rendererListDesc;
        private BaseRenderFunc<DrawTransparencyPassData, RenderGraphContext> _renderFunc;

        public DrawTransparencyPass()
        {
            _renderFunc = RenderFunction;
        }

        public void DrawTransparencyGeometry(RenderGraph renderGraph, ShaderTagId[] shaderTagIds, Camera camera,
            CullingResults cullingResults, in ContextContainer input, int renderingLayerMask)
        {
            using var builder =
                renderGraph.AddRenderPass<DrawTransparencyPassData>(_profilingSampler.name, out var data,
                    _profilingSampler);
            _rendererListDesc =
                new RendererListDesc(shaderTagIds, cullingResults, camera)
                {
                    renderQueueRange = RenderQueueRange.opaque,
                    sortingCriteria = SortingCriteria.CommonOpaque,
                    renderingLayerMask = (uint)renderingLayerMask,
                    rendererConfiguration = PerObjectData.ReflectionProbes |
                                            PerObjectData.Lightmaps |
                                            PerObjectData.ShadowMask |
                                            PerObjectData.LightProbe |
                                            PerObjectData.OcclusionProbe |
                                            PerObjectData.LightProbeProxyVolume |
                                            PerObjectData.OcclusionProbeProxyVolume,

                };

            data.RendererList = builder.UseRendererList(renderGraph.CreateRendererList(_rendererListDesc));
           
            RenderDestinationTextures destinationTextures = input.Get<RenderDestinationTextures>();
            data.DestinationTexture = builder.WriteTexture(destinationTextures.ColorAttachment3);
            
            builder.SetRenderFunc(_renderFunc);
        }

        private void RenderFunction(DrawTransparencyPassData data, RenderGraphContext context)
        {
            //set random write
        }
    }
}
