using Barkar.BSRP.CameraRenderer;
using UnityEngine;
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
    }


    public class TiledShadingPass
    {
        private readonly ProfilingSampler _profilingSampler = new ProfilingSampler("Tile Generate Pass");
        private BaseRenderFunc<TileShadigPassData, ComputeGraphContext> _renderFunc;
        private Camera _camera;
        private Matrix4x4 _cameraProjection;
        private Matrix4x4 _cameraProjectionInverse;
        private Vector2Int _threadGroups = Vector2Int.one;


        public static Vector2Int NumTiles = new Vector2Int(16, 16);
        public static int MaxNumLights = 1024;
        public static int MaxNumLightsPerTile = 64;

        public static int pointLightSizeOf = (3 + 1 + 3 + 1) * sizeof(float);

        struct TileBox
        {
            public Vector4[] frustumPlanes;
        }

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
        private TileBox[] _tileBoxes;

        public TiledShadingPass()
        {
            _renderFunc = RenderFunction;
        }

        public void ExecuteTileShadingPass(RenderGraph renderGraph, in RenderDestinationTextures input,
            CullingResults cullingResults, Camera camera, ComputeShader tileShadingShader)
        {
            using var builder =
                renderGraph.AddComputePass<TileShadigPassData>(_profilingSampler.name, out var data, _profilingSampler);
           
            var info = renderGraph.GetRenderTargetInfo(input.DepthAttachment);
            
            var tileBufferDescriptor = new BufferDesc();
            tileBufferDescriptor.name = "Tiles Buffer";
            tileBufferDescriptor.count = (info.width / 16) * (info.height / 16);
            tileBufferDescriptor.stride = sizeof(float) * 4 * 6;
            tileBufferDescriptor.target = GraphicsBuffer.Target.Structured;

           
            _threadGroups.x = Mathf.CeilToInt(info.width / 16);
            _threadGroups.y = Mathf.CeilToInt(info.height / 16);

            _tileGenerateShader = tileShadingShader;
            data.DepthTextureHandle = builder.CreateTransientTexture(input.DepthAttachment);
            Matrix4x4 viewMatrix = camera.worldToCameraMatrix;
            _cameraProjection = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false) * viewMatrix;
            _cameraProjectionInverse = _cameraProjection.inverse;
            
            data.TileBuffer = builder.UseBuffer(renderGraph.CreateBuffer(tileBufferDescriptor), AccessFlags.ReadWrite);

            builder.SetRenderFunc(_renderFunc);
            
           /* 
            GraphicsBuffer d = data.TileBuffer;
            d.GetData(_tileBoxes);
            foreach (var tileBox in _tileBoxes)
            {
                for (int i = 0; i < tileBox.frustumPlanes.Length; i++)
                {
                    Vector3 normal = new Vector3(tileBox.frustumPlanes[i].x, tileBox.frustumPlanes[i].y,
                        tileBox.frustumPlanes[i].z);
                    Vector3 point = -normal * tileBox.frustumPlanes[i].w;

                    Gizmos.DrawSphere(point, 0.1f);
                }
            }
            
            */
            
        }

        
       

        private void RenderFunction(TileShadigPassData data, ComputeGraphContext context)
        {
            var cmd = context.cmd;
         
            var tilegenerate = _tileGenerateShader.FindKernel("TilesGenerate");
            _tileGenerateShader.SetBuffer(tilegenerate, "_tileBox", data.TileBuffer);
            _tileGenerateShader.SetMatrix("_CameraProjection", _cameraProjection);
            _tileGenerateShader.SetMatrix("_CameraProjectionInverse", _cameraProjectionInverse);
            _tileGenerateShader.SetTexture(tilegenerate, "_DepthTexture",data.DepthTextureHandle, 4);
            cmd.DispatchCompute(_tileGenerateShader, tilegenerate, _threadGroups.x, _threadGroups.y, 1);
        

            
        }
    }
}