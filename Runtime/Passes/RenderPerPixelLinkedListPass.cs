using Barkar.BSRP.CameraRenderer;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

namespace Barkar.BSRP.Passes
{
    public class RenderPPLLPassData
    {
        public TextureHandle Destination;
        public BufferHandle FragmentLinksBuffer;
        public BufferHandle StartOffsetBuffer;
    }

    public class RenderPerPixelLinkedListPass
    {
        private readonly ProfilingSampler _profilingSampler = new("Render OIT PPLL Compute");
        
        private RendererListDesc _rendererListDesc;
        private readonly BaseRenderFunc<RenderPPLLPassData, ComputeGraphContext> _renderFunc;

        private ComputeShader _renderTransparencyCompute;
        private int _renderTransparencyKernel;
        private int _clearBufferKernel;
        private Vector2 _textureSize;
        
        public RenderPerPixelLinkedListPass()
        {
            _renderFunc = RenderFunction;
        }

        public void ExecutePass(RenderGraph renderGraph, in ContextContainer input)
        {
            using var builder =
                renderGraph.AddComputePass<RenderPPLLPassData>(_profilingSampler.name, out var data,
                    _profilingSampler);
            
            var destinationTextures = input.Get<RenderDestinationTextures>();
            var inputData = input.Get<PerPixelLinkedListData>();

            builder.UseTexture(destinationTextures.ColorAttachment3);
            data.Destination = destinationTextures.ColorAttachment3;
            data.FragmentLinksBuffer = builder.UseBuffer(inputData.FragmentLinksBuffer);
            data.StartOffsetBuffer = builder.UseBuffer(inputData.StartOffsetBuffer);

            var info = renderGraph.GetRenderTargetInfo(destinationTextures.ColorAttachment3);
            _renderTransparencyCompute = BSRPResourcesLoader.RenderTransparencyCompute;
            _renderTransparencyKernel = _renderTransparencyCompute.FindKernel("RenderTransparent");
            _clearBufferKernel = _renderTransparencyCompute.FindKernel("ResetStartOffsetBuffer");
            _textureSize = new Vector2(info.width, info.height);
           
            
            builder.SetRenderFunc(_renderFunc);
        }

        private void RenderFunction(RenderPPLLPassData data, ComputeGraphContext context)
        {
            var cmd = context.cmd;
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
           
             groupsX = Mathf.CeilToInt(_textureSize.x / 32f);
              groupsY = Mathf.CeilToInt(_textureSize.y / 32f);
            cmd.SetComputeBufferParam(_renderTransparencyCompute, _clearBufferKernel, "_StartOffsetBuffer",
                data.StartOffsetBuffer);
            cmd.DispatchCompute(_renderTransparencyCompute, _clearBufferKernel, groupsX, groupsY,1 );
         
        }
    }
}