using Barkar.BSRP.CameraRenderer;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace Barkar.BSRP.Passes
{
    public class PointLightsData : ContextItem
    {
        public  BufferHandle TileLightCountBuffer;
        public  BufferHandle TileLightIndicesBuffer;

        public override void Reset()
        {
            TileLightCountBuffer = default;
            TileLightIndicesBuffer = default;
        }
    }
    
    public class PointLightTileCullingPass
    {
        private readonly ProfilingSampler _profilingSampler = new ProfilingSampler("Point Light Tile Culling Pass");
        private BaseRenderFunc<PointLightTileCullingPassData, ComputeGraphContext> _renderFunc;
        private Matrix4x4 _cameraProjection;
        private Vector2Int _tileCount = Vector2Int.one;

        private ComputeShader _tileGenerateShader;

        private int _tilesGenerateKernelIndex;
        private uint _tileSizeX;
        private uint _tileSizeY;
        private int _kernelIndex;
        private readonly int _perTileLightMaxCount = 32;
        
        public PointLightTileCullingPass()
        {
            _renderFunc = RenderFunction;
        }

        public void ExecutePass(RenderGraph renderGraph, ContextContainer input)
        {
            using var builder =
                renderGraph.AddComputePass<PointLightTileCullingPassData>(_profilingSampler.name, out var data, _profilingSampler);
            RenderDestinationTextures destinationTextures = input.Get<RenderDestinationTextures>();
            PointLightsData pointLightsData = input.GetOrCreate<PointLightsData>();
            var info = renderGraph.GetRenderTargetInfo(destinationTextures.DepthAttachment);
            _tileGenerateShader = BSRPResourcesLoader.PointLightsTileCullingComputeShader;
            _tilesGenerateKernelIndex = _tileGenerateShader.FindKernel("PointLightTileCulling");
            _tileGenerateShader.GetKernelThreadGroupSizes(_tilesGenerateKernelIndex, out _tileSizeX, out _tileSizeY,
                out _);
            _tileCount.x = Mathf.CeilToInt((float)info.width / _tileSizeX);
            _tileCount.y = Mathf.CeilToInt((float)info.height / _tileSizeY);
            
            var bufferDescriptor = new BufferDesc();
            bufferDescriptor.name = "TilePointLightCount";
            bufferDescriptor.stride = sizeof(uint);
            bufferDescriptor.target = GraphicsBuffer.Target.Structured;
            bufferDescriptor.count = _tileCount.x * _tileCount.y;
            data.TileLightCountBuffer = builder.UseBuffer(renderGraph.CreateBuffer(bufferDescriptor), AccessFlags.Write);
            bufferDescriptor.name = "TilePointLightIndicesBuffer";
            bufferDescriptor.count = (_tileCount.x * _tileCount.y) * _perTileLightMaxCount;
            data.CameraDepthTexture = (destinationTextures.DepthAttachmentCopy);
            builder.UseTexture(data.CameraDepthTexture); 
            data.TileLightIndicesBuffer = builder.UseBuffer(renderGraph.CreateBuffer(bufferDescriptor), AccessFlags.Write);
            builder.AllowPassCulling(false);
            builder.SetRenderFunc(_renderFunc);
            pointLightsData.TileLightCountBuffer = data.TileLightCountBuffer;
            pointLightsData.TileLightIndicesBuffer = data.TileLightIndicesBuffer;
        }

        private void RenderFunction(PointLightTileCullingPassData data, ComputeGraphContext context)
        {
            var cmd = context.cmd;
            cmd.SetComputeBufferParam(_tileGenerateShader, _kernelIndex, "_TileLightCountBuffer",
                data.TileLightCountBuffer);
            cmd.SetComputeBufferParam(_tileGenerateShader, _kernelIndex, "_TileLightIndicesBuffer",
                data.TileLightIndicesBuffer);
            cmd.SetComputeTextureParam(_tileGenerateShader, _kernelIndex, "_DepthTexture", data.CameraDepthTexture);
            cmd.DispatchCompute(_tileGenerateShader, _kernelIndex, _tileCount.x, _tileCount.y, 1);
        }
    }
}