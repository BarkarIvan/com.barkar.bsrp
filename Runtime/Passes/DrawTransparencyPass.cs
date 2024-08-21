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
        public TextureHandle DepthAttachment;
        public TextureHandle Destination;
        public BufferHandle FragmentLinksBuffer;
        public BufferHandle StartOffsetBuffer;
    }
    
    public class DrawTransparencyPass
    {
        private readonly ProfilingSampler _profilingSampler = new("Draw Transparent Objects");

        private int nodesPerPixel = 5;
        
        private RendererListDesc _rendererListDesc;
        private BaseRenderFunc<DrawTransparencyPassData, RenderGraphContext> _renderFunc;

        
        //move to another compute pass
        private ComputeShader _renderTransparencyCompute;
        private int _renderTransparencyKernel;
        
        //
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
            data.DepthAttachment = builder.UseDepthBuffer(destinationTextures.DepthAttachment, DepthAccess.Read);

            var info = renderGraph.GetRenderTargetInfo(destinationTextures.ColorAttachment3);
            BufferDesc bufferDesc = new BufferDesc();
            bufferDesc.name = "Fragment links buffer";
            bufferDesc.count = info.width * info.height * nodesPerPixel;
            bufferDesc.stride = sizeof(uint) * 4; //col, transmission, depth, next
            bufferDesc.target = GraphicsBuffer.Target.Counter;
            data.FragmentLinksBuffer = builder.WriteBuffer(renderGraph.CreateBuffer(bufferDesc));

            bufferDesc.name = "Start Offset Buffer";
            bufferDesc.count = info.width * info.height;
            bufferDesc.stride = sizeof(uint);
            bufferDesc.target = GraphicsBuffer.Target.Raw;
            data.StartOffsetBuffer = builder.WriteBuffer(renderGraph.CreateBuffer(bufferDesc));

            data.DepthAttachment = builder.UseDepthBuffer(destinationTextures.DepthAttachment, DepthAccess.Read);
            data.Destination = builder.UseColorBuffer(destinationTextures.ColorAttachment3, 0);
           
            //to compute pass
            _renderTransparencyCompute = BSRPResourcesLoader.RenderTransparentComputeShader;
            _renderTransparencyKernel = _renderTransparencyCompute.FindKernel("RenderTransparent");
          
            //
            
            builder.AllowPassCulling(false); //del
            builder.SetRenderFunc(_renderFunc);
        }

        private void RenderFunction(DrawTransparencyPassData data, RenderGraphContext context)
        {
            var cmd = context.cmd;
            
            cmd.SetRandomWriteTarget(1, data.FragmentLinksBuffer);
            cmd.SetRandomWriteTarget(2, data.StartOffsetBuffer);
            cmd.DrawRendererList(data.RendererList);
            cmd.ClearRandomWriteTargets();
            
            //TODO Separate to compute pass!
            //render transparency WIP
            
            
            context.renderContext.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }
    }
}
