using Barkar.BSRP.CameraRenderer;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

namespace Barkar.BSRP.Passes
{
    public class PerPixelLinkedListData : ContextItem
    {
        public BufferHandle FragmentLinksBuffer;
        public BufferHandle StartOffsetBuffer;

        public override void Reset()
        {
            FragmentLinksBuffer = default;
            StartOffsetBuffer = default;
        }
    }

    public class CreatePerPixelLinkedListPass
    {
        private readonly ProfilingSampler _profilingSampler = new("Create OIT PPLL");
        private readonly int _maxSortedPixels = 8;
        private RendererListDesc _rendererListDesc;
        private readonly BaseRenderFunc<CreatePerPixelLinkedListPassData, RenderGraphContext> _renderFunc;
        
        public CreatePerPixelLinkedListPass()
        {
            _renderFunc = RenderFunction;
        }

        public void ExecutePass(RenderGraph renderGraph, ShaderTagId[] shaderTagIds, Camera camera,
            CullingResults cullingResults, in ContextContainer input, int renderingLayerMask)
        {
            using var builder =
                renderGraph.AddRenderPass<CreatePerPixelLinkedListPassData>(_profilingSampler.name, out var data,
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
                                            PerObjectData.OcclusionProbeProxyVolume
                };

            data.RendererList = builder.UseRendererList(renderGraph.CreateRendererList(_rendererListDesc));

            var destinationTextures = input.Get<RenderDestinationTextures>();

            var info = renderGraph.GetRenderTargetInfo(destinationTextures.ColorAttachment3);
            var bufferDesc = new BufferDesc();
            bufferDesc.name = "Fragment links buffer";
            bufferDesc.count = (info.width * info.height) * _maxSortedPixels;
            bufferDesc.stride = sizeof(uint) * 3; 
            bufferDesc.target = GraphicsBuffer.Target.Counter;
            data.FragmentLinksBuffer = builder.WriteBuffer(renderGraph.CreateBuffer(bufferDesc));

            bufferDesc.name = "Start Offset Buffer";
            bufferDesc.count = info.width * info.height;
            bufferDesc.stride = sizeof(uint);
            bufferDesc.target = GraphicsBuffer.Target.Raw;
            data.StartOffsetBuffer = builder.WriteBuffer(renderGraph.CreateBuffer(bufferDesc));
            
            builder.SetRenderFunc(_renderFunc);
            var output = input.GetOrCreate<PerPixelLinkedListData>();
            output.FragmentLinksBuffer = data.FragmentLinksBuffer;
            output.StartOffsetBuffer = data.StartOffsetBuffer;
        }

        private void RenderFunction(CreatePerPixelLinkedListPassData data, RenderGraphContext context)
        {
            var cmd = context.cmd;
            cmd.SetRandomWriteTarget(1, data.FragmentLinksBuffer, false);
            cmd.SetRandomWriteTarget(2, data.StartOffsetBuffer);
            cmd.DrawRendererList(data.RendererList);
            cmd.ClearRandomWriteTargets();
        }
    }
}