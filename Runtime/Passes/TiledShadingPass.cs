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
        private Matrix4x4 _cameraProjectionInverse;
        private Vector2Int _tileCount = Vector2Int.one;
        
  

        struct PointLight
        {
            public Vector3 Color;
            public float Intencity;
            public Vector3 Position;
            public float Radius;
        }

        public static int lightIndexSizeOf = 2 * sizeof(int);

        struct LightIndex
        {
            public int count;
            public int start;
        }

        private ComputeShader _tileGenerateShader;

        //DEBUG
        private int _tilesGenerateKernelIndex;
        private uint _tileSizeX;
        private uint _tileSizeY;

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
            _tilesGenerateKernelIndex = _tileGenerateShader.FindKernel("TilesGenerate");
            _tileGenerateShader.GetKernelThreadGroupSizes(_tilesGenerateKernelIndex, out _tileSizeX, out _tileSizeY,
                out _);
            _tileCount.x = Mathf.CeilToInt((float)info.width / _tileSizeX);
            _tileCount.y = Mathf.CeilToInt((float)info.height / _tileSizeY);


            var tileBufferDescriptor = new BufferDesc();
            tileBufferDescriptor.name = "Tiles Buffer";
            tileBufferDescriptor.count = _tileCount.x * _tileCount.y;
            tileBufferDescriptor.stride = sizeof(float) * 4 * 6;
            tileBufferDescriptor.target = GraphicsBuffer.Target.Structured;


            _tileGenerateShader = tileShadingShader;

            TextureDesc desc = new TextureDesc(info.width, info.height);
            desc.colorFormat = GraphicsFormat.A2B10G10R10_UNormPack32;
            desc.name = "Debug texture";
            desc.enableRandomWrite = true;

            data.DepthTextureHandle = builder.CreateTransientTexture(input.DepthAttachment);
            Matrix4x4 viewMatrix = camera.worldToCameraMatrix;
            _cameraProjection = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);// * viewMatrix;
            _cameraProjectionInverse = _cameraProjection.inverse;

            data.TileBuffer = builder.WriteBuffer(renderGraph.CreateBuffer(tileBufferDescriptor));


            builder.AllowPassCulling(false);
            data.DebugTexture = builder.CreateTransientTexture(desc);

            builder.SetRenderFunc(_renderFunc);
        }


        private void RenderFunction(TileShadigPassData data, RenderGraphContext context)
        {
            var cmd = context.cmd;

            var tilegenerate = _tileGenerateShader.FindKernel("TilesGenerate");
            cmd.SetComputeBufferParam(_tileGenerateShader, tilegenerate, "_tileBox", data.TileBuffer);
            cmd.SetComputeMatrixParam(_tileGenerateShader, "_CameraProjection", _cameraProjection);
            cmd.SetComputeMatrixParam(_tileGenerateShader, "_CameraProjectionInverse", _cameraProjectionInverse);
            cmd.SetComputeTextureParam(_tileGenerateShader, tilegenerate, "_DepthTexture", data.DepthTextureHandle);
            cmd.SetComputeTextureParam(_tileGenerateShader, tilegenerate, "_DebugTexture", data.DebugTexture);
                //cmd.SetRandomWriteTarget(1, data.TileBuffer);
            cmd.SetRandomWriteTarget(1, data.DebugTexture);
            cmd.DispatchCompute(_tileGenerateShader, tilegenerate, _tileCount.x, _tileCount.y, 1);
            cmd.ClearRandomWriteTargets();

           // cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
            cmd.Blit(data.DebugTexture, BuiltinRenderTextureType.CameraTarget);
            context.renderContext.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }
    }
}