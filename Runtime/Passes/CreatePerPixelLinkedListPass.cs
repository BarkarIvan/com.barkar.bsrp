using Barkar.BSRP.CameraRenderer;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

namespace Barkar.BSRP.Passes
{
    
    //TEMP!!!!
    
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
    
    //
    public class CreatePerPixelLinkedListPassData
    {
        public RendererListHandle RendererList;
        public TextureHandle DepthAttachment;
     //   public TextureHandle Destination;
        public BufferHandle FragmentLinksBuffer;
        public BufferHandle StartOffsetBuffer;
    }

    public class CreatePerPixelLinkedListPass
    {
        private readonly ProfilingSampler _profilingSampler = new("Draw Transparent Objects");

        private readonly int nodesPerPixel = 5;

        private RendererListDesc _rendererListDesc;
        private readonly BaseRenderFunc<CreatePerPixelLinkedListPassData, RenderGraphContext> _renderFunc;


        //move to another compute pass
        private ComputeShader _renderTransparencyCompute;
        private int _renderTransparencyKernel;

        private Vector2 _textureSize;

        private int _temCount;
        //


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
            data.DepthAttachment = builder.UseDepthBuffer(destinationTextures.DepthAttachment, DepthAccess.Read);
            
            var info = renderGraph.GetRenderTargetInfo(destinationTextures.ColorAttachment3);
            var bufferDesc = new BufferDesc();
            bufferDesc.name = "Fragment links buffer";
            bufferDesc.count = info.width * info.height * nodesPerPixel;
            bufferDesc.stride = sizeof(uint) + sizeof(float) * 6;//sizeof(uint) * 4; //col, transmission, depth, next
            bufferDesc.target = GraphicsBuffer.Target.Counter;
            data.FragmentLinksBuffer = builder.WriteBuffer(renderGraph.CreateBuffer(bufferDesc));

            bufferDesc.name = "Start Offset Buffer";
            bufferDesc.count = info.width * info.height ;
            bufferDesc.stride = sizeof(uint);
            bufferDesc.target = GraphicsBuffer.Target.Raw;
            data.StartOffsetBuffer = builder.WriteBuffer(renderGraph.CreateBuffer(bufferDesc));

          //  data.Destination = destinationTextures.ColorAttachment3;
/*
            //to compute pass
            _renderTransparencyCompute = BSRPResourcesLoader.RenderTransparentComputeShader;
            _renderTransparencyKernel = _renderTransparencyCompute.FindKernel("RenderTransparent");
            _textureSize = new Vector2(info.width, info.height);
           _temCount = info.width * info.height;
           */
            builder.AllowPassCulling(false); //del
            builder.SetRenderFunc(_renderFunc);
            var output = input.GetOrCreate<PerPixelLinkedListData>();
            output.FragmentLinksBuffer = data.FragmentLinksBuffer;
            output.StartOffsetBuffer = data.StartOffsetBuffer;

        }

        private void RenderFunction(CreatePerPixelLinkedListPassData data, RenderGraphContext context)
        {
            var cmd = context.cmd;
            
            cmd.SetRandomWriteTarget(1, data.FragmentLinksBuffer);
            cmd.SetRandomWriteTarget(2, data.StartOffsetBuffer);
          //cmd.SetRandomWriteTarget(0, data.Destination);
            cmd.DrawRendererList(data.RendererList);

            context.renderContext.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            cmd.ClearRandomWriteTargets();

            /*
            //TODO Separate to compute pass
            //render transparency WIP
            //todo reset into compute
            uint[] reset = new uint[_temCount];
            var b = (GraphicsBuffer)data.StartOffsetBuffer;
            b.SetData(reset);
            //
            cmd.SetBufferCounterValue(data.FragmentLinksBuffer, 0);
            cmd.SetComputeBufferParam(_renderTransparencyCompute, _renderTransparencyKernel, "_FragmentLinksBuffer",
                data.FragmentLinksBuffer);
            cmd.SetComputeBufferParam(_renderTransparencyCompute, _renderTransparencyKernel, "_StartOffsetBuffer",
                data.StartOffsetBuffer);
            cmd.SetComputeTextureParam(_renderTransparencyCompute, _renderTransparencyKernel, "_LightAccumTexture",
                data.Destination);
            int groupsX = Mathf.CeilToInt(_textureSize.x / 8f);
            int groupsY = Mathf.CeilToInt(_textureSize.y / 8f);
            cmd.DispatchCompute(_renderTransparencyCompute, _renderTransparencyKernel,groupsX, groupsY, 1);

            context.renderContext.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            */
        }
    }
}