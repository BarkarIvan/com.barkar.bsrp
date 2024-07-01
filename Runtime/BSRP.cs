using System.Collections.Generic;
using Barkar.BSRP.CameraRenderer;
using Barkar.BSRP.Data;
using Barkar.BSRP.Passes;
using Barkar.BSRP.Passes.Bloom;
using Barkar.BSRP.Passes.Setup;
using Barkar.BSRP.Settings.Shadows;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

public class BSRP : RenderPipeline
{
    private bool _hdr;
    private float _renderScale;

    private  RenderGraph RenderGraph = new RenderGraph("BSRP Render Graph");

    private readonly ShaderTagId[] _commonShaderTags = { new ShaderTagId("BSRPLightMode"), new ShaderTagId("SRPDefaultUnlit") };

    private Vector2Int textureSize = default;
    private RenderGraphParameters renderGraphParameters;

    private ShadowSettings _shadowSettings;
    private BloomSettings _bloomSettings;
    
    private Material _finalPassMaterial;
    private Material _postEffectsMaterial;
    //private Material _dualFilterBlurMaterial;
    private LocalKeyword _useLensDirtKeyword;
    private LocalKeyword _useBloomKeyword;

    private LightingPass _lightingPass = new LightingPass();
    private SetupPass _setupPass = new SetupPass();
    private DrawGeometryPass _drawGeometryPass = new DrawGeometryPass();
    private PostEffectsPass _postEffectsPass = new PostEffectsPass();
    public BSRP(bool hdr, float renderScale, ShadowSettings shadowSettings, BloomSettings bloomSettings)
    {
        _hdr = hdr;
        _renderScale = renderScale;
        _shadowSettings = shadowSettings;
        _bloomSettings = bloomSettings;
        
        //materials and keywords
        _finalPassMaterial = CoreUtils.CreateEngineMaterial("Hidden/FinalPass");
        _postEffectsMaterial = CoreUtils.CreateEngineMaterial("Hidden/PostEffectPasses");
        
        QualitySettings.shadows = ShadowQuality.All;
        GraphicsSettings.useScriptableRenderPipelineBatching = true;
        
    }

    
    protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
    {
        BeginContextRendering(context, cameras);

       
        foreach (Camera camera in cameras)
        {

            BeginCameraRendering(context, camera);

            //destination buffer size
            textureSize.x = (int)(camera.pixelWidth * _renderScale);
            textureSize.y = (int)(camera.pixelHeight * _renderScale);
            
            if (!camera.TryGetCullingParameters(out ScriptableCullingParameters cullingParameters)) continue;
            cullingParameters.shadowDistance = Mathf.Min(camera.farClipPlane, _shadowSettings.ShadowMaxDistance);
            CullingResults cullingResults = context.Cull(ref cullingParameters);

#if UNITY_EDITOR
            if (camera.cameraType == CameraType.SceneView)
            {
                ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
                textureSize.x = camera.pixelWidth;
                textureSize.y = camera.pixelHeight;
            }
#endif
            renderGraphParameters = new RenderGraphParameters
            {
                commandBuffer = CommandBufferPool.Get(),
                currentFrameIndex = Time.frameCount,
                executionName = "Camera Render Graph",
                rendererListCulling = true,
                scriptableRenderContext = context
            };

            RenderGraph.BeginRecording(renderGraphParameters);
          
            LightingResources lightingResources =
                _lightingPass.ExecuteLightngPass(RenderGraph, cullingResults, _shadowSettings);
           
            RenderDestinationTextures destinationTextures =
                _setupPass.SetupDestinationTextures(RenderGraph, textureSize, camera, _hdr);
            
            _drawGeometryPass.DrawGeometry(RenderGraph, _commonShaderTags, camera, cullingResults,
                destinationTextures, camera.cullingMask, true, lightingResources);
           
            if (camera.clearFlags == CameraClearFlags.Skybox)
                DrawSkyboxPass.DrawSkybox(RenderGraph, destinationTextures, camera);
           
            _drawGeometryPass.DrawGeometry(RenderGraph, _commonShaderTags, camera, cullingResults,
                destinationTextures, camera.cullingMask, false, lightingResources);
           
           
            _postEffectsPass.DrawBloom(RenderGraph, _bloomSettings, destinationTextures,
                    camera, _postEffectsMaterial, _finalPassMaterial);
            

          //  FinalPass.DrawFinalPass(RenderGraph, destinationTextures, camera, _finalPassMaterial, bloomData);

            RenderGraph.EndRecordingAndExecute();
        }

        context.ExecuteCommandBuffer(renderGraphParameters.commandBuffer);
        context.Submit();
        CommandBufferPool.Release(renderGraphParameters.commandBuffer);

        RenderGraph.EndFrame();
    }
    
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        CoreUtils.Destroy(_finalPassMaterial);
        CoreUtils.Destroy(_postEffectsMaterial);
        RenderGraph.Cleanup();
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
    }
}