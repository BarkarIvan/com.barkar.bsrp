using Barkar.BSRP.CameraRenderer;
using Barkar.BSRP.Passes.Bloom;
using Barkar.BSRP.Passes.Data;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace Barkar.BSRP.Passes
{
    public class BloomPass
    {
        private static readonly ProfilingSampler _profilingSampler = new("Bloom Pass");

        private static BaseRenderFunc<BloomPassData, RenderGraphContext> _renderFunc;

        private static Vector4 _filter = Vector4.zero;
        private static Camera _camera;

        private static Material material;
        private static BloomSettings _settings;
        private static Vector2Int _attachmentSize;
        private static Vector4 _bloomParams = new Vector4();


        public static Material _bloomMaterial
        {
            get
            {
                if (material == null)
                {
                    material = CoreUtils.CreateEngineMaterial("Hidden/BSRPCustomBloom");
                }

                return material;
            }
        }

        private static Material _finalMat;
        public static Material _finalPassMaterial
        {
            get
            {
                if (_finalMat == null)
                {
                    _finalMat = CoreUtils.CreateEngineMaterial("Hidden/FinalPass");;
                }
                return _finalMat;
            }
        }

        static BloomPass()
        {
            _renderFunc = RenderFunction;
        }

        private static readonly int _filterID = Shader.PropertyToID("_Filter");
        private static readonly int _customBloomLensDirtTextureID = Shader.PropertyToID("_CustomBloomLensDirtTexture");
        private static readonly int _customBloomParamsID = Shader.PropertyToID("_CustomBloomParams");
        private static GlobalKeyword _UseLensDirtKeyword = GlobalKeyword.Create("_USE_CUSTOM_LENSDIRT");


        public static void DrawBloom(Camera camera, RenderGraph renderGraph, BloomSettings settings,
            in RenderDestinationTextures input, Vector2Int attachmentSize)
        {
            using var builder =
                renderGraph.AddRenderPass<BloomPassData>(_profilingSampler.name, out var bloomPassData,
                    _profilingSampler);

            //prefilter
            float knee = Mathf.GammaToLinearSpace(settings.HDRThreshold * settings.HDRSoftThreshold);
            _filter.x = settings.HDRThreshold;
            _filter.y = _filter.x - knee;
            _filter.z = 2f * knee;
            _filter.w = 0.25f / (knee + 0.0001f);

            _camera = camera;
            _settings = settings;
            _attachmentSize = attachmentSize;
            bloomPassData.ColorSource = builder.ReadTexture(input.ColorAttachment);

            TextureDesc textureDescriptor = new TextureDesc(attachmentSize.x, attachmentSize.y);
            textureDescriptor.colorFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.HDR);
            textureDescriptor.name = "Bloom Prefilter";
            bloomPassData.BloomPassTexture = builder.CreateTransientTexture(textureDescriptor);
            bloomPassData.BlurPyramid[0] = bloomPassData.BloomPassTexture;

            for (int i = 1; i < settings.BlurPassesCount + 1; i++)
            {
                Debug.Log("i = " + i);
                
                var downsample = 1 << i;

                var sizeX = attachmentSize.x / downsample;
                var sizeY = attachmentSize.y / downsample;
                  if (sizeX < 1f || sizeY < 1f) {break;}
                TextureDesc desc = new TextureDesc(sizeX, sizeY)
                {
                    name = "Blur Pyramid " + i,
                    colorFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.HDR),
                    depthBufferBits = DepthBits.None,
                    msaaSamples = MSAASamples.None,
                    useMipMap = false,
                    autoGenerateMips = false
                };

                bloomPassData.BlurPyramid[i] = builder.CreateTransientTexture(desc);
          
            }
            //builder.AllowPassCulling(false); 
            _bloomParams.x = settings.LensDirtIntensity;
            _bloomParams.w = settings.Intensity;
            float dirtratio = (float)_settings.LensDirtTexture.width / (float)_settings.LensDirtTexture.height;
            float screenRatio = (float)_attachmentSize.x / (float)_attachmentSize.y;
            if (dirtratio > screenRatio)
            {
                _bloomParams.y = screenRatio / dirtratio;
            }
            else if (screenRatio > dirtratio)
            {
                _bloomParams.z = dirtratio / screenRatio;
            }
            
            
            builder.SetRenderFunc(_renderFunc);
        }

        private static void RenderFunction(BloomPassData data, RenderGraphContext context)
        {
            var cmd = context.cmd;
            //prefilter
            cmd.SetGlobalVector(_filterID, _filter);
            cmd.SetGlobalVector(_customBloomParamsID, _bloomParams);
            cmd.SetKeyword(_UseLensDirtKeyword, _settings.UseLensDirt);
            CoreUtils.SetRenderTarget(cmd, data.BlurPyramid[0], RenderBufferLoadAction.DontCare,
                RenderBufferStoreAction.Store, ClearFlag.All, Color.clear);
            Blitter.BlitTexture(cmd, data.ColorSource, Vector2.one, _bloomMaterial, 0);
            
            //blur

            if (_settings.BlurPassesCount > 0)
            {
                var offset = _settings.BlurOffset;

                for (int i = 0; i < _settings.BlurPassesCount; i++)
                {
                    var src = data.BlurPyramid[i];
                    var dst = data.BlurPyramid[i + 1];

                    var downsample = 1 << i;

                    var sizeX = _attachmentSize.x / downsample;
                    var sizeY = _attachmentSize.y / downsample;
                    
                   // RTHandle rtHandle = RTHandles.Alloc(src);
                    var texelSize = Vector2.one / new Vector2(sizeX, sizeY);
                    var halfPixel = texelSize * 0.5f;
                    
                    cmd.SetGlobalVector("_DualFilterOffset",
                        new Vector4(halfPixel.x * offset, halfPixel.y * offset, 1, 1));
                    
                    CoreUtils.SetRenderTarget(cmd, dst, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, ClearFlag.All, Color.clear);
                    Blitter.BlitTexture(cmd, src, Vector2.one, _bloomMaterial, 1);
                    
                }
                
                
                for (int i = _settings.BlurPassesCount; i >= 1 ; i--)
                {
                    var src = data.BlurPyramid[i];
                    var dst = data.BlurPyramid[i - 1];

                    var downsample = 1 << i;

                    var sizeX = _attachmentSize.x / downsample;
                    var sizeY = _attachmentSize.y / downsample;
                    
                    // RTHandle rtHandle = RTHandles.Alloc(src);
                    var texelSize = Vector2.one / new Vector2(sizeX, sizeY);
                    var halfPixel = texelSize * 0.5f;
                    
                    cmd.SetGlobalVector("_DualFilterOffset",
                        new Vector4(halfPixel.x * offset, halfPixel.y * offset, 1, 1));
                    
                    CoreUtils.SetRenderTarget(cmd, dst, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, ClearFlag.All, Color.clear);
                    Blitter.BlitTexture(cmd, src, Vector2.one, _bloomMaterial, 1);
                }
            }


            cmd.SetGlobalTexture("_CameraOpaque", data.ColorSource);
            cmd.SetGlobalTexture(_customBloomLensDirtTextureID, _settings.LensDirtTexture);

            cmd.SetGlobalTexture("_BloomTexture", data.BlurPyramid[0]);
            cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, RenderBufferLoadAction.DontCare,
                RenderBufferStoreAction.Store);
           cmd.SetViewport(_camera.pixelRect); //!!!!!
            cmd.DrawProcedural(Matrix4x4.identity, _finalPassMaterial, 0, MeshTopology.Triangles,
               3);
          
         
          
         // CoreUtils.SetRenderTarget(cmd, BuiltinRenderTextureType.CameraTarget, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
         //Blitter.BlitTexture(cmd, data.BlurPyramid[0], Vector2.one, _finalPassMaterial, 0);
            context.renderContext.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }
    }
}