using Barkar.BSRP.CameraRenderer;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

namespace Barkar.BSRP.Passes
{
    public class RenderTransparencyPPLLPassData
    
    {
       // public RendererListHandle RendererList;
       // public TextureHandle DepthAttachment;
        public TextureHandle Destination;
        public BufferHandle FragmentLinksBuffer;
        public BufferHandle StartOffsetBuffer;
    }

    public class RenderTransparencyPPLLPass
    {
        private readonly ProfilingSampler _profilingSampler = new("Draw Transparent Objects");
        

        private RendererListDesc _rendererListDesc;
        private readonly BaseRenderFunc<RenderTransparencyPPLLPassData, ComputeGraphContext> _renderFunc;


        //move to another compute pass
        private ComputeShader _renderTransparencyCompute;
        private int _renderTransparencyKernel;
        private int _clearBufferKernel;

        private Vector2 _textureSize;

        private int _temCount;
        //


        public RenderTransparencyPPLLPass()
        {
            _renderFunc = RenderFunction;
        }

        public void DrawTransparencyGeometry(RenderGraph renderGraph, in ContextContainer input)
        {
            using var builder =
                renderGraph.AddComputePass<RenderTransparencyPPLLPassData>(_profilingSampler.name, out var data,
                    _profilingSampler);

            
            var destinationTextures = input.Get<RenderDestinationTextures>();
            var inputData = input.Get<PerPixelLinkedListData>();

            builder.UseTexture(destinationTextures.ColorAttachment3);
            data.Destination = destinationTextures.ColorAttachment3;
            data.FragmentLinksBuffer = builder.UseBuffer(inputData.FragmentLinksBuffer);
            data.StartOffsetBuffer = builder.UseBuffer(inputData.StartOffsetBuffer);

            var info = renderGraph.GetRenderTargetInfo(destinationTextures.ColorAttachment3);
            //to compute pass
            _renderTransparencyCompute = BSRPResourcesLoader.RenderTransparencyCompute;
            _renderTransparencyKernel = _renderTransparencyCompute.FindKernel("RenderTransparent");
            _clearBufferKernel = _renderTransparencyCompute.FindKernel("ResetStartOffsetBuffer");
            _textureSize = new Vector2(info.width, info.height);
           _temCount = info.width * info.height;
           
            builder.AllowPassCulling(false); //del
            builder.SetRenderFunc(_renderFunc);
        }

        private void RenderFunction(RenderTransparencyPPLLPassData data, ComputeGraphContext context)
        {
            
            //todo reset into compute
           // uint[] reset = new uint[_temCount];
          //  var b = (GraphicsBuffer)data.StartOffsetBuffer;
          //  b.SetData(reset);
            //
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