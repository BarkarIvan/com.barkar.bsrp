using System.ComponentModel;
using Barkar.BSRP.CameraRenderer;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace Barkar.BSRP
{
    public class TileShadigPassData
    {
        public BufferHandle TileLightCountBuffer;
        public BufferHandle TileLightIndicesBuffer;
        public TextureHandle DepthTextureHandle;
        public TextureHandle NormalTexture;
        public TextureHandle AlbedoSmoothnessTexture;
        public TextureHandle RadianceMetallicTexture;
       // public TextureHandle DebugTexture;
        public TextureHandle LightAccumTexture;
        public MaterialPropertyBlock _tempMPB = new MaterialPropertyBlock();
        public Material _testPLMaterial;
    }


    public class TiledShadingPass
    {
        private readonly ProfilingSampler _profilingSampler = new ProfilingSampler("Tile Shading Pass");
        private BaseRenderFunc<TileShadigPassData, RenderGraphContext> _renderFunc;
        private Camera _camera;
        private Matrix4x4 _cameraProjection;
        private Vector2Int _tileCount = Vector2Int.one;

        private ComputeShader _tileGenerateShader;
        private Vector4 _textureParams;

        //DEBUG
        private int _tilesGenerateKernelIndex;
        private uint _tileSizeX;
        private uint _tileSizeY;
        private int _keernelIndex;
        private int _perTileLightMaxCount = 32;
        

        public TiledShadingPass()
        {
            _renderFunc = RenderFunction;
        }

        public void ExecuteTileShadingPass(RenderGraph renderGraph, in RenderDestinationTextures input,
            CullingResults cullingResults, Camera camera, ComputeShader tileShadingShader, Material testLightMaterial)
        {
            using var builder =
                renderGraph.AddRenderPass<TileShadigPassData>(_profilingSampler.name, out var data, _profilingSampler);


            var info = renderGraph.GetRenderTargetInfo(input.DepthAttachment);
            _tileGenerateShader = tileShadingShader;

            _tilesGenerateKernelIndex = _tileGenerateShader.FindKernel("TileShading");
            _tileGenerateShader.GetKernelThreadGroupSizes(_tilesGenerateKernelIndex, out _tileSizeX, out _tileSizeY,
                out _);
            _tileCount.x = Mathf.CeilToInt((float)info.width / _tileSizeX);
            _tileCount.y = Mathf.CeilToInt((float)info.height / _tileSizeY);

            _tileGenerateShader = tileShadingShader;

            _textureParams = new Vector4(info.width, info.height, 0, 0);
           // TextureDesc debugDesc = new TextureDesc(info.width, info.height);
          //  debugDesc.colorFormat = GraphicsFormat.A2B10G10R10_UNormPack32;
          //  debugDesc.name = "Debug texture";
           // debugDesc.enableRandomWrite = true;

            var bufferDescriptor = new BufferDesc();
            bufferDescriptor.name = "TilePointLightCount";
            bufferDescriptor.stride = sizeof(uint);
            bufferDescriptor.target = GraphicsBuffer.Target.Structured;

            bufferDescriptor.count = _tileCount.x * _tileCount.y;
            data.TileLightCountBuffer = builder.WriteBuffer(renderGraph.CreateBuffer(bufferDescriptor));
            bufferDescriptor.name = "TilePointLightIndicesBuffer";
            bufferDescriptor.count = (_tileCount.x * _tileCount.y) * _perTileLightMaxCount;
            data.TileLightIndicesBuffer = builder.WriteBuffer(renderGraph.CreateBuffer(bufferDescriptor));
            data.NormalTexture = builder.ReadTexture(input.ColorAttachment2);
            data.LightAccumTexture = builder.ReadWriteTexture(input.ColorAttachment3);
            data.DepthTextureHandle = builder.ReadTexture(input.DepthAttachment);
            data.AlbedoSmoothnessTexture = builder.ReadTexture(input.ColorAttachment0);
            data.RadianceMetallicTexture = builder.ReadTexture(input.ColorAttachment2);
          //  Matrix4x4 viewMatrix = camera.worldToCameraMatrix;
          //  _cameraProjection = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false); // * viewMatrix;

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
           // data.DebugTexture = builder.CreateTransientTexture(debugDesc);
            _camera = camera;
            data._testPLMaterial = testLightMaterial;
            
            data.NormalTexture = builder.ReadWriteTexture(input.ColorAttachment2);
            builder.SetRenderFunc(_renderFunc);
        }


        private void RenderFunction(TileShadigPassData data, RenderGraphContext context)
        {
            var cmd = context.cmd;
            cmd.SetComputeBufferParam(_tileGenerateShader, _keernelIndex, "_TileLightCountBuffer",
                data.TileLightCountBuffer);
            cmd.SetComputeBufferParam(_tileGenerateShader, _keernelIndex, "_TileLightIndicesBuffer",
                data.TileLightIndicesBuffer);

            cmd.SetComputeTextureParam(_tileGenerateShader, _keernelIndex, "_DepthTexture", data.DepthTextureHandle);
           // cmd.SetComputeTextureParam(_tileGenerateShader, _keernelIndex, "_DebugTexture", data.DebugTexture);
           // cmd.SetRandomWriteTarget(2, data.DebugTexture);
            cmd.SetRandomWriteTarget(0, data.TileLightCountBuffer);
            cmd.SetRandomWriteTarget(1, data.TileLightIndicesBuffer);
            cmd.DispatchCompute(_tileGenerateShader, _keernelIndex, _tileCount.x, _tileCount.y, 1);
            
            cmd.ClearRandomWriteTargets();
            context.renderContext.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            
            ////
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
            data._tempMPB.SetTexture("_GBuffer0", data.AlbedoSmoothnessTexture);
            data._tempMPB.SetTexture("_Gbuffer1", data.RadianceMetallicTexture);
            data._tempMPB.SetTexture("_GBuffer2", data.NormalTexture);
            data._tempMPB.SetTexture("_CameraDepth", data.DepthTextureHandle);
            data._tempMPB.SetVector("_TextureParams", _textureParams);
            data._tempMPB.SetBuffer("_TileLightCountBuffer", data.TileLightCountBuffer);
            data._tempMPB.SetBuffer("_TileLightIndicesBuffer", data.TileLightIndicesBuffer);
            cmd.SetRenderTarget(data.LightAccumTexture, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, data.DepthTextureHandle, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.DrawProcedural(Matrix4x4.identity, data._testPLMaterial, 1, MeshTopology.Triangles, 3, 1, data._tempMPB);

            context.renderContext.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }
    }
}