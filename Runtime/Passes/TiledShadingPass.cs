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
        public BufferHandle TileBuffer;
        public BufferHandle LightBuffer;
        public BufferHandle LightAssignBuffer;
        public BufferHandle AssignTableBuffer;
        public TextureHandle DepthTextureHandle;

        public TextureHandle DebugTexture;
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

        public TiledShadingPass()
        {
            _renderFunc = RenderFunction;
        }

        public void ExecuteTileShadingPass(RenderGraph renderGraph, in RenderDestinationTextures input,
            CullingResults cullingResults, Camera camera, ComputeShader tileShadingShader)
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

            data.DepthTextureHandle = builder.CreateTransientTexture(input.DepthAttachment);
            Matrix4x4 viewMatrix = camera.worldToCameraMatrix;
            _cameraProjection = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);// * viewMatrix;

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

            builder.SetRenderFunc(_renderFunc);
        }


        private void RenderFunction(TileShadigPassData data, RenderGraphContext context)
        {
            var cmd = context.cmd;

            cmd.SetComputeBufferParam(_tileGenerateShader, _keernelIndex, "_tileBox", data.TileBuffer);
            cmd.SetComputeTextureParam(_tileGenerateShader, _keernelIndex, "_DepthTexture", data.DepthTextureHandle);
            cmd.SetComputeTextureParam(_tileGenerateShader, _keernelIndex, "_DebugTexture", data.DebugTexture);
            cmd.SetRandomWriteTarget(1, data.DebugTexture);
            cmd.DispatchCompute(_tileGenerateShader, _keernelIndex, _tileCount.x, _tileCount.y, 1);
            cmd.Blit(data.DebugTexture, BuiltinRenderTextureType.CameraTarget);
            cmd.ClearRandomWriteTargets();

            context.renderContext.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }
    }
}