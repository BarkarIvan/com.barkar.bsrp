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
        public BufferHandle PointLightDataBuffer;
        public TextureHandle DepthTextureHandle;
        public TextureHandle DebugTexture;
    }

    public class TiledShadingPass
    {
        private readonly ProfilingSampler _profilingSampler = new ProfilingSampler("Tile Generate Pass");
        private BaseRenderFunc<TileShadigPassData, RenderGraphContext> _renderFunc;
        private Camera _camera;
        private Matrix4x4 _cameraProjection;

        private Vector2 _textureSize;
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

        public void ExecuteTileShadingPass(RenderGraph renderGraph, in RenderDestinationTextures input, in LightingResources lightingResources,
            CullingResults cullingResults, Camera camera, ComputeShader tileShadingShader)
        {
            using var builder =
                renderGraph.AddRenderPass<TileShadigPassData>(_profilingSampler.name, out var data, _profilingSampler);

            var info = renderGraph.GetRenderTargetInfo(input.DepthAttachment);
            _camera = camera;
            _tileGenerateShader = tileShadingShader;
            _tilesGenerateKernelIndex = _tileGenerateShader.FindKernel("TilesGenerate");
            _tileGenerateShader.GetKernelThreadGroupSizes(_tilesGenerateKernelIndex, out _tileSizeX, out _tileSizeY,
                out uint groupSizeZ);
            _tileCountX = Mathf.CeilToInt((float)info.width / _tileSizeX);
            _tileCountY = Mathf.CeilToInt((float)info.height / _tileSizeY);
            _lightsIndicesBufferSize = _tileCountX * _tileCountY * _maxLightsPerTile;
            var argsBufferSize = _tileCountX * _tileCountY;
            var desc = new BufferDesc();
            desc.name = "Tiles Lights Args Buffer";
            desc.count = argsBufferSize;
            desc.stride = sizeof(int);
            desc.target = GraphicsBuffer.Target.Structured;
            data.TileLightsArgsBuffer = builder.WriteBuffer(renderGraph.CreateBuffer(desc));

            desc.count = _lightsIndicesBufferSize;
            desc.name = "Tile Lights Indices Buffer";
            data.TileLightsIndicesBuffer = builder.WriteBuffer(renderGraph.CreateBuffer(desc));

            data.PointLightDataBuffer = builder.ReadBuffer(lightingResources.PointLightBuffer);
            TextureDesc textureDesc = new TextureDesc(info.width, info.height);
            textureDesc.enableRandomWrite = true;
            textureDesc.useMipMap = false;
            textureDesc.colorFormat = GraphicsFormat.B8G8R8A8_SRGB;
            textureDesc.name = "Debug texture";
            data.DebugTexture = builder.CreateTransientTexture(textureDesc);

            data.DepthTextureHandle = builder.ReadTexture(input.DepthAttachment);
            _textureSize = new Vector2(info.width, info.width);
            _cameraProjection = camera.projectionMatrix;

            _nearPlaneHeight = Mathf.Tan(Mathf.Deg2Rad * camera.fieldOfView * 0.5f) * 2 * camera.nearClipPlane;
            _nearPlaneWidth = camera.aspect * _nearPlaneHeight;

            _deferredTileParams = new Vector4(_tileSizeX, _tileSizeY, _tileCountX, _tileCountY);

             
            builder.AllowPassCulling(false);

            builder.SetRenderFunc(_renderFunc);
        }


        private void RenderFunction(TileShadigPassData data, RenderGraphContext context)
        {
            var cmd = context.cmd;
            cmd.ClearRandomWriteTargets();

           // context.renderContext.SetupCameraProperties(_camera);
            cmd.SetComputeBufferParam(_tileGenerateShader, _tilesGenerateKernelIndex, "_RWTileLightsArgsBuffer",
                data.TileLightsArgsBuffer);
            cmd.SetComputeBufferParam(_tileGenerateShader, _tilesGenerateKernelIndex, "_RWTileLightsIndicesBuffer",
                data.TileLightsArgsBuffer);
            cmd.SetComputeTextureParam(_tileGenerateShader, _tilesGenerateKernelIndex, "_DepthTexture",
                data.DepthTextureHandle);
            cmd.SetComputeTextureParam(_tileGenerateShader, _tilesGenerateKernelIndex, "_DebugTexture",
                data.DebugTexture);
            cmd.SetComputeMatrixParam(_tileGenerateShader, "_ProjectionMatrix", _cameraProjection );
            cmd.SetComputeVectorParam(_tileGenerateShader, "_TextureSize", _textureSize);
            cmd.SetComputeVectorParam(_tileGenerateShader, "_DeferredTileParams", _deferredTileParams);
            cmd.SetComputeVectorParam(_tileGenerateShader, "_CameraNearPlaneLB", _cameraNearPlaneLB);
            cmd.SetComputeVectorParam(_tileGenerateShader, "_CameraNearBasisH", _cameraNearBasisH);
            cmd.SetComputeVectorParam(_tileGenerateShader, "_CameraNearBasisV", _cameraNearBasisV);
            cmd.SetRandomWriteTarget(3, data.DebugTexture);
            cmd.SetRandomWriteTarget(2, data.TileLightsArgsBuffer);
            cmd.SetRandomWriteTarget(1, data.TileLightsIndicesBuffer);

            cmd.DispatchCompute(_tileGenerateShader, _tilesGenerateKernelIndex, _tileCountX, _tileCountY, 1);
           context.renderContext.ExecuteCommandBuffer(cmd);

           cmd.Clear();
        }
    }
}