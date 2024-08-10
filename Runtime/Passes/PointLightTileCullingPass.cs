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
        private BaseRenderFunc<PointLightTileCullingPassData, RenderGraphContext> _renderFunc;
        private Matrix4x4 _cameraProjection;
        private Vector2Int _tileCount = Vector2Int.one;

        private ComputeShader _tileGenerateShader;

        //DEBUG
        private int _tilesGenerateKernelIndex;
        private uint _tileSizeX;
        private uint _tileSizeY;
        private int _keernelIndex;
        private int _perTileLightMaxCount = 32;
        

        public PointLightTileCullingPass()
        {
            _renderFunc = RenderFunction;
            
        }

        public PointLightsCullingData ExecuteTileCullingPass(RenderGraph renderGraph, ContextContainer input, ComputeShader tileShadingShader)
        {
            using var builder =
                renderGraph.AddRenderPass<PointLightTileCullingPassData>(_profilingSampler.name, out var data, _profilingSampler);
            RenderDestinationTextures destinationTextures = input.Get<RenderDestinationTextures>();

            var info = renderGraph.GetRenderTargetInfo(destinationTextures.DepthAttachment);
            _tileGenerateShader = tileShadingShader;

            _tilesGenerateKernelIndex = _tileGenerateShader.FindKernel("TileShading");
            _tileGenerateShader.GetKernelThreadGroupSizes(_tilesGenerateKernelIndex, out _tileSizeX, out _tileSizeY,
                out _);
            _tileCount.x = Mathf.CeilToInt((float)info.width / _tileSizeX);
            _tileCount.y = Mathf.CeilToInt((float)info.height / _tileSizeY);

            _tileGenerateShader = tileShadingShader;
            
            var bufferDescriptor = new BufferDesc();
            bufferDescriptor.name = "TilePointLightCount";
            bufferDescriptor.stride = sizeof(uint);
            bufferDescriptor.target = GraphicsBuffer.Target.Structured;
            bufferDescriptor.count = _tileCount.x * _tileCount.y;
            data.TileLightCountBuffer = builder.WriteBuffer(renderGraph.CreateBuffer(bufferDescriptor));
            bufferDescriptor.name = "TilePointLightIndicesBuffer";
            bufferDescriptor.count = (_tileCount.x * _tileCount.y) * _perTileLightMaxCount;
            data.TileLightIndicesBuffer = builder.WriteBuffer(renderGraph.CreateBuffer(bufferDescriptor));
            data.CameraDepthTexture = builder.ReadTexture(destinationTextures.DepthAttachmentCopy);
        
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
            builder.SetRenderFunc(_renderFunc);
            return new PointLightsCullingData(data.TileLightCountBuffer, data.TileLightIndicesBuffer);
        }


        private void RenderFunction(PointLightTileCullingPassData data, RenderGraphContext context)
        {
            var cmd = context.cmd;
            cmd.SetComputeBufferParam(_tileGenerateShader, _keernelIndex, "_TileLightCountBuffer",
                data.TileLightCountBuffer);
            cmd.SetComputeBufferParam(_tileGenerateShader, _keernelIndex, "_TileLightIndicesBuffer",
                data.TileLightIndicesBuffer);

            cmd.SetComputeTextureParam(_tileGenerateShader, _keernelIndex, "_DepthTexture", data.CameraDepthTexture);
            cmd.SetRandomWriteTarget(0, data.TileLightCountBuffer);
            cmd.SetRandomWriteTarget(1, data.TileLightIndicesBuffer);
            cmd.DispatchCompute(_tileGenerateShader, _keernelIndex, _tileCount.x, _tileCount.y, 1);
            
            cmd.ClearRandomWriteTargets();
            context.renderContext.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            
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