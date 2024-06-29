using Barkar.BSRP.CameraRenderer;
using Barkar.BSRP.Data;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace Barkar.BSRP.Passes
{
    public class FinalPassData
    {
        public TextureHandle ColorAttachment;
        public TextureHandle BloomTexture;
    }

    public class FinalPass
    {
        private static readonly ProfilingSampler FinalPassProfilingSampler = new("Final Pass");

        private static readonly int
            cameraOpaqueTextureID = Shader.PropertyToID("_CameraOpaque");

        private static readonly int bloomTextureID = Shader.PropertyToID("_BloomTexture");

        private static Camera _camera;
        static Material FinalPassMaterial;
        private static MaterialPropertyBlock _mpb = new MaterialPropertyBlock();
        private static readonly int _customBloomLensDirtTextureID = Shader.PropertyToID("_CustomBloomLensDirtTexture");
        private static readonly int _customBloomParamsID = Shader.PropertyToID("_CustomBloomParams");
        private static GlobalKeyword _useLensDirtKeyword;

        private static BloomData _bloomData;

        private static BaseRenderFunc<FinalPassData, RenderGraphContext> _renderFunc;

        static FinalPass()
        {
            _renderFunc = RenderFunction;
            _useLensDirtKeyword = GlobalKeyword.Create("_USE_LENSDIRT");
        }

        public static void DrawFinalPass(RenderGraph renderGraph,
            in RenderDestinationTextures input, Camera camera, Material finalPassMaterial, in BloomData bloomData)
        {
            using var builder = renderGraph.AddRenderPass<FinalPassData>(FinalPassProfilingSampler.name,
                out var finalPassData, FinalPassProfilingSampler);
            
            _bloomData = bloomData;
            finalPassData.ColorAttachment = builder.ReadTexture(input.ColorAttachment);
            finalPassData.BloomTexture = builder.ReadTexture(_bloomData.BloomTexture);
            FinalPassMaterial = finalPassMaterial;
            _camera = camera;
            builder.SetRenderFunc(_renderFunc);
        }

        private static void RenderFunction(FinalPassData passData, RenderGraphContext context)
        {
            var cmd = context.cmd;
            _mpb.SetTexture(bloomTextureID, passData.BloomTexture);
            _mpb.SetTexture(cameraOpaqueTextureID, passData.ColorAttachment);
            _mpb.SetTexture(_customBloomLensDirtTextureID, _bloomData.LensDirtTexture);
            _mpb.SetVector(_customBloomParamsID, _bloomData.BloomParams);
           
            cmd.SetKeyword(_useLensDirtKeyword, _bloomData.UseLensDirtTexture);
            cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, RenderBufferLoadAction.DontCare,
                RenderBufferStoreAction.Store);
            cmd.SetViewport(_camera.pixelRect);
            cmd.DrawProcedural(Matrix4x4.identity, FinalPassMaterial, 0, MeshTopology.Triangles,
                3, 1, _mpb);
            context.renderContext.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }
    }
}