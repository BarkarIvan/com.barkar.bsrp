using Barkar.BSRP.CameraRenderer;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

namespace Barkar.BSRP.Passes
{
    public class RenderTransparencyPPLLPassData
    
    {
        public RendererListHandle RendererList;
        public TextureHandle DepthAttachment;
     //   public TextureHandle Destination;
        public BufferHandle FragmentLinksBuffer;
        public BufferHandle StartOffsetBuffer;
    }

    public class RenderTransparencyPPLLPass
    {
        private readonly ProfilingSampler _profilingSampler = new("Draw Transparent Objects");

        private readonly int nodesPerPixel = 5;

        private RendererListDesc _rendererListDesc;
        private readonly BaseRenderFunc<RenderTransparencyPPLLPassData, ComputeGraphContext> _renderFunc;


        //move to another compute pass
        private ComputeShader _renderTransparencyCompute;
        private int _renderTransparencyKernel;

        private Vector2 _textureSize;

        private int _temCount;
        //


        public RenderTransparencyPPLLPass()
        {
            _renderFunc = RenderFunction;
        }

        public void DrawTransparencyGeometry(RenderGraph renderGraph, ShaderTagId[] shaderTagIds, Camera camera,
            CullingResults cullingResults, in ContextContainer input, int renderingLayerMask)
        {
            using var builder =
                renderGraph.AddComputePass<CreatePerPixelLinkedListPassData>(_profilingSampler.name, out var data,
                    _profilingSampler);

            
            var destinationTextures = input.Get<RenderDestinationTextures>();
         

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
        }

        private void RenderFunction(RenderTransparencyPPLLPassData data, ComputeGraphContext context)
        {
            var cmd = context.cmd;
       
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