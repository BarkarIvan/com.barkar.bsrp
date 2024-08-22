using Barkar.BSRP.CameraRenderer;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace Barkar.BSRP.Passes
{
    public class PointLightTileCullingPassData
    {
        public BufferHandle TileLightCountBuffer;
        public BufferHandle TileLightIndicesBuffer;
        public TextureHandle CameraDepthTexture;
    }

    public readonly ref struct PointLightsCullingData
    {
        public readonly BufferHandle TileLightCountBuffer;
        public readonly BufferHandle TileLightIndicesBuffer;

        public PointLightsCullingData(BufferHandle tileLightCountBuffer, BufferHandle tileLightIndicesBuffer)
        {
            TileLightCountBuffer = tileLightCountBuffer;
            TileLightIndicesBuffer = tileLightIndicesBuffer;
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
        private int _keernelIndex;
        private int _perTileLightMaxCount = 32;
        
        public PointLightTileCullingPass()
        {
            _renderFunc = RenderFunction;
        }

        public PointLightsCullingData ExecuteTileCullingPass(RenderGraph renderGraph, ContextContainer input)
        {
            using var builder =
                renderGraph.AddComputePass<PointLightTileCullingPassData>(_profilingSampler.name, out var data, _profilingSampler);
            RenderDestinationTextures destinationTextures = input.Get<RenderDestinationTextures>();

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
        
            /*
            var p = _cameraProjection;
        _invProjParam= new Vector4(
                    p.m20 / (p.m00 * p.m23),
                    p.m21 / (p.m11 * p.m23),
                    -1f / p.m23,
                    (-p.m22 + p.m20 * p.m02 / p.m00 + p.m21 * p.m12 / p.m11) / p.m23
                );
            */

            builder.AllowPassCulling(false);
            builder.EnableAsyncCompute(true); // ??

            builder.SetRenderFunc(_renderFunc);
            return new PointLightsCullingData(data.TileLightCountBuffer, data.TileLightIndicesBuffer);
        }


        private void RenderFunction(PointLightTileCullingPassData data, ComputeGraphContext context)
        {
            var cmd = context.cmd;
            cmd.SetComputeBufferParam(_tileGenerateShader, _keernelIndex, "_TileLightCountBuffer",
                data.TileLightCountBuffer);
            cmd.SetComputeBufferParam(_tileGenerateShader, _keernelIndex, "_TileLightIndicesBuffer",
                data.TileLightIndicesBuffer);

            cmd.SetComputeTextureParam(_tileGenerateShader, _keernelIndex, "_DepthTexture", data.CameraDepthTexture);
        
            //used when it was render pass
            // cmd.SetRandomWriteTarget(0, data.TileLightCountBuffer);
            // cmd.SetRandomWriteTarget(1, data.TileLightIndicesBuffer);
            cmd.DispatchCompute(_tileGenerateShader, _keernelIndex, _tileCount.x, _tileCount.y, 1);
            
           // cmd.ClearRandomWriteTargets();
           //  context.cmd.ExecuteCommandBuffer(cmd);
           //  cmd.Clear();
            
            ////DEBUG
            /*
           uint[] lightCountData = new uint[(uint)(_tileCount.x * _tileCount.y)];
            var b = (GraphicsBuffer)data.TileLightCountBuffer;
            b.GetData(lightCountData);


            for (int i = 0; i < (_tileCount.x * _tileCount.y); i++)
            {
                Debug.Log($"Tile {i}: Light Count = {lightCountData[i]}");
                
            }
            ////
        */
        }
    }
}