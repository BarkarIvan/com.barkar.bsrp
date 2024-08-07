using System.Data;
using Barkar.BSRP.CameraRenderer;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace Barkar.BSRP
{
    public class PointLightsPassData
    {
        public BufferHandle TileLightCountBuffer;
        public BufferHandle TileLightIndicesBuffer;
        public TextureHandle CameraDepth;
        public TextureHandle DepthAttachment;
       public TextureHandle NormalTexture;
        public TextureHandle AlbedoSmoothnessTexture;
        public TextureHandle RadianceMetallicTexture;
       // public TextureHandle DebugTexture;
       public TextureHandle LightAccumTexture;
        public MaterialPropertyBlock _tempMPB = new MaterialPropertyBlock();
        public Material _testPLMaterial;
    }

   


    public class PointLightsPass
    {
        private readonly ProfilingSampler _profilingSampler = new ProfilingSampler("Point Lights Pass");
        private BaseRenderFunc<PointLightsPassData, RenderGraphContext> _renderFunc;
      //  private Camera _camera;
       // private Matrix4x4 _cameraProjection;
       // private Vector2Int _tileCount = Vector2Int.one;

       // private ComputeShader _tileGenerateShader;
        private Vector4 _textureParams;

        //DEBUG
      //  private int _tilesGenerateKernelIndex;
      //  private uint _tileSizeX;
      //  private uint _tileSizeY;
       // private int _keernelIndex;
       // private int _perTileLightMaxCount = 32;
        

        public PointLightsPass()
        {
            _renderFunc = RenderFunction;
            
        }

        public void ExecutePointLightPass(RenderGraph renderGraph, in RenderDestinationTextures input,
           in PointLightsCullingData cullingData, Material testLightMaterial)
        {
            using var builder =
                renderGraph.AddRenderPass<PointLightsPassData>(_profilingSampler.name, out var data, _profilingSampler);


              var info = renderGraph.GetRenderTargetInfo(input.DepthAttachment);
            // _tileGenerateShader = tileShadingShader;

            // _tilesGenerateKernelIndex = _tileGenerateShader.FindKernel("TileShading");
            //  _tileGenerateShader.GetKernelThreadGroupSizes(_tilesGenerateKernelIndex, out _tileSizeX, out _tileSizeY,
            //      out _);
            //  _tileCount.x = Mathf.CeilToInt((float)info.width / _tileSizeX);
            // _tileCount.y = Mathf.CeilToInt((float)info.height / _tileSizeY);

            // _tileGenerateShader = tileShadingShader;

             _textureParams = new Vector4(info.width, info.height, 0, 0);
            // TextureDesc debugDesc = new TextureDesc(info.width, info.height);
            //  debugDesc.colorFormat = GraphicsFormat.A2B10G10R10_UNormPack32;
            //  debugDesc.name = "Debug texture";
            // debugDesc.enableRandomWrite = true;

            //  var bufferDescriptor = new BufferDesc();
            //   bufferDescriptor.name = "TilePointLightCount";
            //   bufferDescriptor.stride = sizeof(uint);
            //   bufferDescriptor.target = GraphicsBuffer.Target.Structured;

            //bufferDescriptor.count = _tileCount.x * _tileCount.y;
            data.TileLightCountBuffer = builder.ReadBuffer(cullingData.TileLightCountBuffer);
            // bufferDescriptor.name = "TilePointLightIndicesBuffer";
            // bufferDescriptor.count = (_tileCount.x * _tileCount.y) * _perTileLightMaxCount;
            data.TileLightIndicesBuffer = builder.ReadBuffer(cullingData.TileLightIndicesBuffer);

            data.DepthAttachment = builder.UseDepthBuffer(input.DepthAttachment, DepthAccess.Read);
            data.CameraDepth = builder.ReadTexture(input.DepthAttachmentCopy);

            data.AlbedoSmoothnessTexture = builder.ReadTexture(input.ColorAttachment0);
            data.RadianceMetallicTexture = builder.ReadTexture(input.ColorAttachment1);
            data.NormalTexture = builder.ReadTexture(input.ColorAttachment2);
            data.LightAccumTexture = builder.UseColorBuffer(input.ColorAttachment3,0);
            
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
        //    _camera = camera;
             data._testPLMaterial = testLightMaterial;
          
            
            builder.SetRenderFunc(_renderFunc);
        }


        private void RenderFunction(PointLightsPassData data, RenderGraphContext context)
        {
            var cmd = context.cmd;
            var mpb = context.renderGraphPool.GetTempMaterialPropertyBlock();
           mpb.SetTexture("_GBuffer0", data.AlbedoSmoothnessTexture);
           mpb.SetTexture("_GBuffer1", data.RadianceMetallicTexture);
           mpb.SetTexture("_GBuffer2", data.NormalTexture);
           mpb.SetTexture("_CameraDepth", data.CameraDepth);
           mpb.SetVector("_TextureParams", _textureParams);
           mpb.SetBuffer("_TileLightCountBuffer", data.TileLightCountBuffer);
           mpb.SetBuffer("_TileLightIndicesBuffer", data.TileLightIndicesBuffer);
            cmd.DrawProcedural(Matrix4x4.identity, data._testPLMaterial, 1, MeshTopology.Triangles, 3, 1, mpb);

            context.renderContext.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }
    }
}