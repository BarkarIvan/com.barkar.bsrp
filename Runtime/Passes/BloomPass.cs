using Barkar.BSRP.CameraRenderer;
using Barkar.BSRP.Data;
using Barkar.BSRP.Passes.Bloom;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace Barkar.BSRP.Passes
{
    public class BloomPass
    {
        private readonly ProfilingSampler _profilingSampler = new("Bloom Pass");
        private BaseRenderFunc<BloomPassData, RenderGraphContext> _renderFunc;
        
        private readonly int _dualFilterOffsetID = Shader.PropertyToID("_DualFilterOffset");
        private readonly int _filterID = Shader.PropertyToID("_Filter");
        
        
         public BloomPass()
        {
            _renderFunc = RenderFunction;
           
        }
        
        public  void ExecuteBloomPass( RenderGraph renderGraph, BloomSettings settings,
            ContextContainer container, Material bloomMaterial)
        {
            using var builder =
                renderGraph.AddRenderPass<BloomPassData>(_profilingSampler.name, out var bloomPassData,
                    _profilingSampler);

           builder.AllowPassCulling(false);
           
           RenderDestinationTextures destinationTextures = container.Get<RenderDestinationTextures>();
           
           bloomPassData.ColorSource = builder.ReadTexture(destinationTextures.LightTextureCopy);
           
           //prefilter
            float knee = (settings.HDRThreshold * settings.HDRSoftThreshold);
            bloomPassData.Prefilter.x = settings.HDRThreshold;
            bloomPassData.Prefilter.y = bloomPassData.Prefilter.x - knee;
            bloomPassData.Prefilter.z = 2f * knee;
            bloomPassData.Prefilter.w = 0.25f / (knee + 0.0001f);

            /// bloom
            var rtinfo = renderGraph.GetRenderTargetInfo(destinationTextures.ColorAttachment3);
            
            bloomPassData.OriginalSize = new Vector2Int(rtinfo.width, rtinfo.height);
            bloomPassData.BloomMaterial = bloomMaterial;
            

            var width = bloomPassData.OriginalSize.x >> settings.Downsample;
            var height = bloomPassData.OriginalSize.y >> settings.Downsample;
            TextureDesc textureDescriptor = new TextureDesc(width, height);
            textureDescriptor.colorFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.HDR);
            textureDescriptor.name = "Bloom Pass Texture [0]";
            bloomPassData.BloomPassTexture = builder.WriteTexture(renderGraph.CreateTexture(textureDescriptor));
            
            bloomPassData.BlurPyramid[0] = bloomPassData.BloomPassTexture;
            bloomPassData.LensDirtTexture = settings.LensDirtTexture;
            for (int i = 1; i < settings.BlurPassesCount + 1; i++)
            {
                var downsample = 1 << i;
                var sizeX = width / downsample;
                var sizeY = height / downsample;
                if (sizeX < 1f || sizeY < 1f)
                {
                    break;
                }

                TextureDesc desc = new TextureDesc(sizeX, sizeY)
                {
                    name = "Blur Pyramid " + "[" + i + "]",
                    colorFormat = GraphicsFormat.A2B10G10R10_UNormPack32,
                    depthBufferBits = DepthBits.None,
                    msaaSamples = MSAASamples.None,
                    useMipMap = false,
                    autoGenerateMips = false
                };

                bloomPassData.BlurPyramid[i] = builder.CreateTransientTexture(desc);
            }

            bloomPassData.BloomParams.x = settings.LensDirtIntensity;
            bloomPassData.BloomParams.w = settings.Intensity;
            float dirtRatio = (float)settings.LensDirtTexture.width / (float)settings.LensDirtTexture.height;
            float screenRatio = (float)bloomPassData.OriginalSize.x / (float)bloomPassData.OriginalSize.y;
            if (dirtRatio > screenRatio)
            {
                bloomPassData.BloomParams.y = screenRatio / dirtRatio;
            }
            else if (screenRatio > dirtRatio)
            {
                bloomPassData.BloomParams.z = dirtRatio / screenRatio;
            }

            bloomPassData.BlurPassOffset = settings.BlurOffset;
            bloomPassData.BlurPassesCount = settings.BlurPassesCount;
            bloomPassData.UseLensDirt = settings.UseLensDirt;
            bloomPassData.BloomEnable = settings.BloomEnable;
            builder.SetRenderFunc(_renderFunc);
        }

        private  void RenderFunction(BloomPassData data, RenderGraphContext context)
        {
            var cmd = context.cmd;
            var bloomMPB = context.renderGraphPool.GetTempMaterialPropertyBlock();
            
            //prefilter
            bloomMPB.SetVector(_filterID, data.Prefilter);
            bloomMPB.SetTexture(BSRPShaderIDs.SourceTextureID, data.ColorSource);
            
            cmd.SetRenderTarget( data.BloomPassTexture, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.ClearRenderTarget(RTClearFlags.ColorDepth, Color.clear);
            cmd.DrawProcedural( Matrix4x4.identity, data.BloomMaterial, 0, MeshTopology.Triangles,3, 1, bloomMPB);
            context.renderContext.ExecuteCommandBuffer(cmd);
            cmd.Clear();
           
            //blur
            if (data.BlurPassesCount > 0)
            {
                var offset = data.BlurPassOffset;

                for (int i = 0; i < data.BlurPassesCount; i++)
                {
                    var src = data.BlurPyramid[i];
                    var dst = data.BlurPyramid[i + 1];

                    var downsample = 1 << i;

                    var sizeX = data.OriginalSize.x / downsample;
                    var sizeY = data.OriginalSize.y / downsample;

                    var texelSize = Vector2.one / new Vector2(sizeX, sizeY);
                    var halfPixel = texelSize * 0.5f;
                   
                    bloomMPB.SetVector(_dualFilterOffsetID,
                        new Vector4(halfPixel.x * offset, halfPixel.y * offset, 1, 1));
                    bloomMPB.SetTexture(BSRPShaderIDs.SourceTextureID, src);
                    cmd.SetRenderTarget( dst, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                    cmd.ClearRenderTarget(RTClearFlags.ColorDepth, Color.clear);
                    cmd.DrawProcedural( Matrix4x4.identity, data.BloomMaterial, 1, MeshTopology.Triangles,3, 1, bloomMPB);
                    context.renderContext.ExecuteCommandBuffer(cmd);
                    cmd.Clear();
                }

                for (int i = data.BlurPassesCount; i >= 1; i--)
                {
                    var src = data.BlurPyramid[i];
                    var dst = data.BlurPyramid[i - 1];

                    var downsample = 1 << i;

                    var sizeX = data.OriginalSize.x / downsample;
                    var sizeY = data.OriginalSize.y / downsample;

                    var texelSize = Vector2.one / new Vector2(sizeX, sizeY);
                    var halfPixel = texelSize * 0.5f;

                    bloomMPB.SetVector(_dualFilterOffsetID,
                        new Vector4(halfPixel.x * offset, halfPixel.y * offset, 1, 1));
                    bloomMPB.SetTexture(BSRPShaderIDs.SourceTextureID, src);
                    
                    cmd.SetRenderTarget( dst, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
                    cmd.DrawProcedural(Matrix4x4.identity, data.BloomMaterial, 2, MeshTopology.Triangles, 3, 1, bloomMPB);
                    context.renderContext.ExecuteCommandBuffer(cmd);
                    cmd.Clear();
                }
            }
            
            cmd.SetGlobalTexture(BSRPShaderIDs.BloomTextureID, data.BloomPassTexture);
            cmd.SetGlobalTexture(BSRPShaderIDs._customBloomLensDirtTextureID, data.LensDirtTexture);
            cmd.SetGlobalVector(BSRPShaderIDs._customBloomParamsID, data.BloomParams);
            
            context.renderContext.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }
    }
}