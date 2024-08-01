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
        public TextureHandle DebugTexture;
        public TextureHandle LightAccumTexture;
        public MaterialPropertyBlock _tempMPB;
        public Material _testPLMaterial;
    }


    public class TiledShadingPass
    {
        private readonly ProfilingSampler _profilingSampler = new ProfilingSampler("Tile Generate Pass");
        private BaseRenderFunc<TileShadigPassData, RenderGraphContext> _renderFunc;
        private Camera _camera;
        private Matrix4x4 _cameraProjection;
        private Vector2Int _tileCount = Vector2Int.one;

        private ComputeShader _tileGenerateShader;

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
            _keernelIndex = _tileGenerateShader.FindKernel("TilesGenerate");

            _tilesGenerateKernelIndex = _tileGenerateShader.FindKernel("TilesGenerate");
            _tileGenerateShader.GetKernelThreadGroupSizes(_tilesGenerateKernelIndex, out _tileSizeX, out _tileSizeY,
                out _);
            _tileCount.x = Mathf.CeilToInt((float)info.width / _tileSizeX);
            _tileCount.y = Mathf.CeilToInt((float)info.height / _tileSizeY);

            _tileGenerateShader = tileShadingShader;

            TextureDesc debugDesc = new TextureDesc(info.width, info.height);
            debugDesc.colorFormat = GraphicsFormat.A2B10G10R10_UNormPack32;
            debugDesc.name = "Debug texture";
            debugDesc.enableRandomWrite = true;

            var bufferDescriptor = new BufferDesc();
            bufferDescriptor.name = "TilePointLightCount";
            bufferDescriptor.stride = sizeof(uint);
            bufferDescriptor.target = GraphicsBuffer.Target.Structured;

            bufferDescriptor.count = _tileCount.x * _tileCount.y;
            data.TileLightCountBuffer = builder.WriteBuffer(renderGraph.CreateBuffer(bufferDescriptor));

            bufferDescriptor.name = "TilePointLightIndicesBuffer";
            bufferDescriptor.count = (_tileCount.x * _tileCount.y) * _perTileLightMaxCount;
            data.TileLightIndicesBuffer = builder.WriteBuffer(renderGraph.CreateBuffer(bufferDescriptor));

            data.LightAccumTexture = builder.ReadWriteTexture(input.ColorAttachment3);
            builder.UseDepthBuffer(input.DepthAttachment, DepthAccess.Read);

            data.DepthTextureHandle = builder.ReadTexture(input.DepthAttachment);
            Matrix4x4 viewMatrix = camera.worldToCameraMatrix;
            _cameraProjection = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false); // * viewMatrix;

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
            data.DebugTexture = builder.CreateTransientTexture(debugDesc);
            _camera = camera;
            //test
            data._tempMPB = new MaterialPropertyBlock();
            data._testPLMaterial = testLightMaterial;
            data.NormalTexture = builder.ReadWriteTexture(input.ColorAttachment2);
            builder.SetRenderFunc(_renderFunc);
        }


        private void RenderFunction(TileShadigPassData data, RenderGraphContext context)
        {
            var cmd = context.cmd;
//context.renderContext.SetupCameraProperties(_camera);
            cmd.SetComputeBufferParam(_tileGenerateShader, _keernelIndex, "_TileLightCountBuffer",
                data.TileLightCountBuffer);
            cmd.SetComputeBufferParam(_tileGenerateShader, _keernelIndex, "_TileLightIndicesBuffer",
                data.TileLightIndicesBuffer);

            cmd.SetComputeTextureParam(_tileGenerateShader, _keernelIndex, "_DepthTexture", data.DepthTextureHandle);
            cmd.SetComputeTextureParam(_tileGenerateShader, _keernelIndex, "_DebugTexture", data.DebugTexture);
            cmd.SetRandomWriteTarget(2, data.DebugTexture);
            cmd.SetRandomWriteTarget(0, data.TileLightCountBuffer);
            cmd.SetRandomWriteTarget(1, data.TileLightIndicesBuffer);
            cmd.DispatchCompute(_tileGenerateShader, _keernelIndex, _tileCount.x, _tileCount.y, 1);
            //  cmd.Blit(data.DebugTexture, BuiltinRenderTextureType.CameraTarget);
            // context.renderContext.ExecuteCommandBuffer(cmd);
            //cmd.Clear();
            // data._tempMPB.SetTexture("_GBuffer2",data.NormalTexture);
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
            
            data._tempMPB.SetBuffer("_TileLightCountBuffer", data.TileLightCountBuffer);
            data._tempMPB.SetBuffer("_TileLightIndicesBuffer", data.TileLightIndicesBuffer);
            // data._tempMPB.SetVector("_Size", new Vector4(_camera.pixelWidth, _camera.pixelHeight,0,0));
              cmd.SetRenderTarget(data.LightAccumTexture);
            cmd.DrawProcedural(Matrix4x4.identity, data._testPLMaterial, 1, MeshTopology.Triangles, 3, 1, data._tempMPB);

            context.renderContext.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }
    }
}