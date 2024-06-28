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
        private static BloomSettings _settings;
        private static Vector2Int _attachmentSize;
        private static Vector4 _bloomParams = Vector4.one;



        private static Material _material;
        private static MaterialPropertyBlock _mbp = new MaterialPropertyBlock();
        private static readonly int _dualFilterOffsetID = Shader.PropertyToID("_DualFilterOffset");
        private static readonly int _filterID = Shader.PropertyToID("_Filter");

        private static Material _bloomMaterial
        {
            get
            {
                if (_material == null)
                {
                    _material = CoreUtils.CreateEngineMaterial("Hidden/BSRPCustomBloom");
                }
                return _material;
            }
        }
        
        static BloomPass()
        {
            _renderFunc = RenderFunction;
        }



        public static BloomData DrawBloom( RenderGraph renderGraph, BloomSettings settings,
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

            _settings = settings;
            _attachmentSize = attachmentSize;
            bloomPassData.ColorSource = builder.ReadTexture(input.ColorAttachment);

            TextureDesc textureDescriptor = new TextureDesc(attachmentSize.x >> settings.Downsample,
                attachmentSize.y >> settings.Downsample);
            textureDescriptor.colorFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.HDR);
            textureDescriptor.name = "Bloom Prefilter [0]";
            bloomPassData.BloomPassTexture = builder.UseColorBuffer(renderGraph.CreateTexture(textureDescriptor), 0);
            bloomPassData.BlurPyramid[0] = bloomPassData.BloomPassTexture;

            for (int i = 1; i < settings.BlurPassesCount + 1; i++)
            {
                Debug.Log("i = " + i);

                var downsample = 1 << i;

                var sizeX = attachmentSize.x / downsample;
                var sizeY = attachmentSize.y / downsample;
                if (sizeX < 1f || sizeY < 1f)
                {
                    break;
                }

                TextureDesc desc = new TextureDesc(sizeX, sizeY)
                {
                    name = "Blur Pyramid " + "[" + i + "]",
                    colorFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.HDR),
                    depthBufferBits = DepthBits.None,
                    msaaSamples = MSAASamples.None,
                    useMipMap = false,
                    autoGenerateMips = false
                };

                bloomPassData.BlurPyramid[i] = builder.CreateTransientTexture(desc);
            }

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

            return new BloomData(bloomPassData.BloomPassTexture, settings.LensDirtTexture, settings.UseLensDirt,
                _bloomParams);
        }

        private static void RenderFunction(BloomPassData data, RenderGraphContext context)
        {
            var cmd = context.cmd;
           
            //prefilter
            cmd.SetGlobalVector(_filterID, _filter);
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

                    var texelSize = Vector2.one / new Vector2(sizeX, sizeY);
                    var halfPixel = texelSize * 0.5f;

                    _bloomMaterial.SetVector(_dualFilterOffsetID,
                        new Vector4(halfPixel.x * offset, halfPixel.y * offset, 1, 1));

                    CoreUtils.SetRenderTarget(cmd, dst, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                        ClearFlag.All, Color.clear);
                    Blitter.BlitTexture(cmd, src, Vector2.one, _bloomMaterial, 1);
                }


                for (int i = _settings.BlurPassesCount; i >= 1; i--)
                {
                    var src = data.BlurPyramid[i];
                    var dst = data.BlurPyramid[i - 1];

                    var downsample = 1 << i;

                    var sizeX = _attachmentSize.x / downsample;
                    var sizeY = _attachmentSize.y / downsample;

                    var texelSize = Vector2.one / new Vector2(sizeX, sizeY);
                    var halfPixel = texelSize * 0.5f;

                    _bloomMaterial.SetVector(_dualFilterOffsetID,
                        new Vector4(halfPixel.x * offset, halfPixel.y * offset, 1, 1));
                    
                    CoreUtils.SetRenderTarget(cmd, dst, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                        ClearFlag.All, Color.clear);
                    Blitter.BlitTexture(cmd, src, Vector2.one, _bloomMaterial, 1);
                }
            }

            context.renderContext.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }
    }
}