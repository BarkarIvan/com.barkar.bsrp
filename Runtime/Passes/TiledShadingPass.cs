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
        public BufferHandle TileLightsIndicesBuffer;
        public BufferHandle TileLightsArgsBuffer;
        public TextureHandle DepthTextureHandle;
        public TextureHandle ResultTexture;
    }

    public class TiledShadingPass
    {
        private readonly ProfilingSampler _profilingSampler = new ProfilingSampler("Tile Generate Pass");
        private BaseRenderFunc<TileShadigPassData, RenderGraphContext> _renderFunc;
        private Camera _camera;
        private Matrix4x4 _cameraProjection;
      //  private Matrix4x4 _cameraProjectionInverse;
      //  private Vector2Int _threadGroups = Vector2Int.one;


        private uint _tileSizeX;
        private uint _tileSizeY;
        private int _tileCountX;
        private int _tileCountY;

        private float _nearPlaneWidth;
        private float _nearPlaneHeight;
        private Vector2 _cameraNearBasisH;
        private Vector2 _cameraNearBasisV;
        private Vector4 _cameraNearPlaneLB;
        private Vector4 _deferredTileParams;

        public int _maxLightsPerTile = 32;
        public int _lightsIndicesBufferSize;


        struct TileBox
        {
            public Vector4[] frustumPlanes;
        }


        private ComputeShader _tileGenerateShader;
        private int _tilesGenerateKernelIndex;

        //DEBUG
        private TileBox[] _tileBoxes;

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
            _tileCountX = Mathf.CeilToInt(info.width * 1f / _tileSizeX);
            _tileCountY = Mathf.CeilToInt(info.height * 1f / _tileSizeY);
            _lightsIndicesBufferSize = _tileCountX * _tileCountY * _maxLightsPerTile;


            var desc = new BufferDesc();
            desc.name = "Tiles Lights Args Buffer";
            desc.count = _lightsIndicesBufferSize;
            desc.stride = sizeof(int);
            desc.target = GraphicsBuffer.Target.Structured;
            data.TileLightsArgsBuffer = builder.WriteBuffer(renderGraph.CreateBuffer(desc));

            desc.name = "Tile Lights Indices Buffer";
            data.TileLightsIndicesBuffer = builder.WriteBuffer(renderGraph.CreateBuffer(desc));


            TextureDesc textureDesc = new TextureDesc(info.width, info.height);
            textureDesc.enableRandomWrite = true;
            textureDesc.useMipMap = true;
            textureDesc.colorFormat = GraphicsFormat.R16G16_SFloat;
            textureDesc.name = "Debug texture";

            data.DepthTextureHandle = builder.CreateTransientTexture(input.DepthAttachment);
           // Matrix4x4 viewMatrix = camera.worldToCameraMatrix;
            //_cameraProjection = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false) * viewMatrix;
            //_cameraProjectionInverse = _cameraProjection.inverse;

            _nearPlaneHeight = Mathf.Tan(Mathf.Deg2Rad * camera.fieldOfView * 0.5f) * 2 * camera.nearClipPlane;
            _nearPlaneWidth = camera.aspect * _nearPlaneHeight;

            _deferredTileParams = new Vector4(_tileSizeX, _tileSizeY, _tileCountX, _tileCountY);

            _cameraNearBasisH = new Vector2(_tileSizeX * _nearPlaneWidth / info.width, 0);
            _cameraNearBasisV = new Vector2(0, _tileSizeY * _nearPlaneHeight / info.height);
            _cameraNearPlaneLB = new Vector4(-_nearPlaneWidth / 2, -_nearPlaneHeight / 2, camera.nearClipPlane, 0);

            builder.AllowPassCulling(false);
            data.ResultTexture = builder.CreateTransientTexture(textureDesc);

            builder.SetRenderFunc(_renderFunc);
        }


        private void RenderFunction(TileShadigPassData data, RenderGraphContext context)
        {
            var cmd = context.cmd;

            cmd.SetComputeBufferParam(_tileGenerateShader, _tilesGenerateKernelIndex, "_RWTileLightsArgsBuffer",
                data.TileLightsArgsBuffer);
            cmd.SetComputeBufferParam(_tileGenerateShader, _tilesGenerateKernelIndex, "_RWTileLightsIndicesBuffer",
                data.TileLightsArgsBuffer);
            cmd.SetComputeTextureParam(_tileGenerateShader, _tilesGenerateKernelIndex, "_DepthTexture",
                data.DepthTextureHandle);
            cmd.SetComputeTextureParam(_tileGenerateShader, _tilesGenerateKernelIndex, "_DebugTexture",
                data.ResultTexture);
            cmd.SetComputeVectorParam(_tileGenerateShader, "_DeferredTileParams", _deferredTileParams);
            cmd.SetComputeVectorParam(_tileGenerateShader, "_CameraNearPlaneLB", _cameraNearPlaneLB);
            cmd.SetComputeVectorParam(_tileGenerateShader, "_CameraNearBasisH", _cameraNearBasisH);
            cmd.SetComputeVectorParam(_tileGenerateShader, "_CameraNearBasisV", _cameraNearBasisV);
            cmd.SetRandomWriteTarget(0, data.TileLightsArgsBuffer);
            cmd.SetRandomWriteTarget(0, data.TileLightsIndicesBuffer);
            cmd.DispatchCompute(_tileGenerateShader, _tilesGenerateKernelIndex, _tileCountX, _tileCountY, 1);
            cmd.ClearRandomWriteTargets();
            context.renderContext.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }
    }
}