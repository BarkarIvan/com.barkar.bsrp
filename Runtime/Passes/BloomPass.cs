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
        private static Vector2Int _originalTextureSize;
        private static Vector4 _bloomParams = Vector4.one; //one - bcs lens scale

        private static MaterialPropertyBlock _mpb;
        private static readonly int _dualFilterOffsetID = Shader.PropertyToID("_DualFilterOffset");
        private static readonly int _filterID = Shader.PropertyToID("_Filter");
        private static readonly int _sourceTextureID = Shader.PropertyToID("_SourceTexture");
        private static Material _bloomMaterial;

        static BloomPass()
        {
            _renderFunc = RenderFunction;
        }
        
        public static BloomData DrawBloom( RenderGraph renderGraph, BloomSettings settings,
            in RenderDestinationTextures input, Material bloomMaterial)
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
            
            var rtinfo = renderGraph.GetRenderTargetInfo(input.ColorAttachment);
            
            _originalTextureSize = new Vector2Int(rtinfo.width, rtinfo.height);
            _bloomMaterial = bloomMaterial;
            
            bloomPassData.ColorSource = builder.ReadTexture(input.ColorAttachment);

            TextureDesc textureDescriptor = new TextureDesc(_originalTextureSize.x >> settings.Downsample,
                _originalTextureSize.y >> settings.Downsample);
            textureDescriptor.colorFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.HDR);
            textureDescriptor.name = "Bloom Texture [0]";
          
            bloomPassData.BloomPassTexture = builder.UseColorBuffer(renderGraph.CreateTexture(textureDescriptor), 0);
            bloomPassData.BlurPyramid[0] = bloomPassData.BloomPassTexture;

            for (int i = 1; i < settings.BlurPassesCount + 1; i++)
            {
                var downsample = 1 << i;
                var sizeX = _originalTextureSize.x / downsample;
                var sizeY = _originalTextureSize.y / downsample;
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
            float screenRatio = (float)_originalTextureSize.x / (float)_originalTextureSize.y;
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
            _mpb = context.renderGraphPool.GetTempMaterialPropertyBlock();
            //prefilter
            _mpb.SetVector(_filterID, _filter);
            _mpb.SetTexture(_sourceTextureID, data.ColorSource);
            cmd.SetRenderTarget( data.BlurPyramid[0], RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.ClearRenderTarget(RTClearFlags.ColorDepth, Color.clear);
            cmd.DrawProcedural( Matrix4x4.identity, _bloomMaterial, 0, MeshTopology.Triangles,3, 1, _mpb);

            //blur
            if (_settings.BlurPassesCount > 0)
            {
                var offset = _settings.BlurOffset;

                for (int i = 0; i < _settings.BlurPassesCount; i++)
                {
                    var src = data.BlurPyramid[i];
                    var dst = data.BlurPyramid[i + 1];

                    var downsample = 1 << i;

                    var sizeX = _originalTextureSize.x / downsample;
                    var sizeY = _originalTextureSize.y / downsample;

                    var texelSize = Vector2.one / new Vector2(sizeX, sizeY);
                    var halfPixel = texelSize * 0.5f;

                    _mpb.SetVector(_dualFilterOffsetID,
                        new Vector4(halfPixel.x * offset, halfPixel.y * offset, 1, 1));
                    _mpb.SetTexture(_sourceTextureID, src);
                    cmd.SetRenderTarget( dst, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                    cmd.ClearRenderTarget(RTClearFlags.ColorDepth, Color.clear);
                    cmd.DrawProcedural( Matrix4x4.identity, _bloomMaterial, 1, MeshTopology.Triangles,3, 1, _mpb);
                }

                for (int i = _settings.BlurPassesCount; i >= 1; i--)
                {
                    var src = data.BlurPyramid[i];
                    var dst = data.BlurPyramid[i - 1];

                    var downsample = 1 << i;

                    var sizeX = _originalTextureSize.x / downsample;
                    var sizeY = _originalTextureSize.y / downsample;

                    var texelSize = Vector2.one / new Vector2(sizeX, sizeY);
                    var halfPixel = texelSize * 0.5f;

                    _bloomMaterial.SetVector(_dualFilterOffsetID,
                        new Vector4(halfPixel.x * offset, halfPixel.y * offset, 1, 1));
                    
                    _mpb.SetVector(_dualFilterOffsetID,
                        new Vector4(halfPixel.x * offset, halfPixel.y * offset, 1, 1));
                    _mpb.SetTexture(_sourceTextureID, src);
                    cmd.SetRenderTarget( dst, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                    cmd.ClearRenderTarget(RTClearFlags.ColorDepth, Color.clear);
                    cmd.DrawProcedural( Matrix4x4.identity, _bloomMaterial, 1, MeshTopology.Triangles,3, 1, _mpb);
                }
            }

            context.renderContext.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }
    }
}