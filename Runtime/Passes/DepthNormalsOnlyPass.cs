using Barkar.BSRP.CameraRenderer;
using Unity.Android.Gradle.Manifest;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

namespace Barkar.BSRP.Passes
{
    public class DepthNormalsOnlyPassData
    {
        public RendererListHandle RendererList;
        public TextureHandle NormalsTexture;
        public TextureHandle CameraDepthTexture;
    }
    public class DepthNormalsOnlyPass
    {

        private readonly ProfilingSampler _profilingSampler = new ProfilingSampler(" Depth Normals Pass");
        private RendererListDesc _rendererListDecriptipn;
        private readonly BaseRenderFunc<DepthNormalsOnlyPassData, RasterGraphContext> _renderFunc;

        public DepthNormalsOnlyPass()
        {
            _renderFunc = RenderFunction;
        }

        public void ExecutePass(RenderGraph renderGraph, ShaderTagId[] shaderTags, Camera camera,
            CullingResults cullingResults, in ContextContainer input, int renderingLayerMask)
        {
            using var builder  = renderGraph.AddRasterRenderPass<DepthNormalsOnlyPassData>(
                _profilingSampler.name, out var data, _profilingSampler);


            _rendererListDecriptipn = new RendererListDesc(shaderTags, cullingResults, camera)
            {
                renderQueueRange = RenderQueueRange.all,
                sortingCriteria = SortingCriteria.RenderQueue,
                renderingLayerMask = (uint)renderingLayerMask
            };

            data.RendererList = renderGraph.CreateRendererList(_rendererListDecriptipn);
            builder.UseRendererList(data.RendererList);

            var destinationTextures = input.Get<RenderDestinationTextures>();

            builder.SetRenderAttachment(destinationTextures.DepthAttachmentCopy,0);
            builder.SetRenderAttachmentDepth(destinationTextures.DepthAttachmentCopy);

        }

        private void RenderFunction(DepthNormalsOnlyPassData data, RasterGraphContext contetx)
        {
            contetx.cmd.DrawRendererList(data.RendererList);
        }


    }
}