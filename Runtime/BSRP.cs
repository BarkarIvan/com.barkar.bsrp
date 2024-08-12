using System.Collections.Generic;
using Barkar.BSRP;
using Barkar.BSRP.CameraRenderer;
using Barkar.BSRP.Passes;
using Barkar.BSRP.Passes.Bloom;
using Barkar.BSRP.Passes.Setup;
using Barkar.BSRP.Settings.Shadows;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

public class BSRP : RenderPipeline
{
    private float _renderScale;

    private RenderGraph RenderGraph = new RenderGraph("BSRP Render Graph");

    private readonly ShaderTagId[] _commonShaderTags =
        { new ShaderTagId("BSRPGBuffer"), new ShaderTagId("SRPDefaultUnlit") };

    private ContextContainer _container;

    private Vector2Int textureSize = default;
    private RenderGraphParameters renderGraphParameters;

    private ShadowSettings _shadowSettings;
    private BloomSettings _bloomSettings;

    private Material _defferedLightingMaterial;
    private Material _deferredFinalPassMaterial;
    private Material _screenSpaceShadowMaterial;
    private Material _postFXMaterial;

    private ComputeShader _pointLightTileCullingCompute;

    private GlobalKeyword _useLensDirtKeyword;
    private GlobalKeyword _useBloomKeyWord;

    private LightingSetupPass _lightingSetupPass = new LightingSetupPass();
    private SetupPass _setupPass = new SetupPass();
    private DrawGeometryPass _drawGeometryPass = new DrawGeometryPass();
    private DirectionalLightPass _directionalLight = new DirectionalLightPass();
    private DeferredFinalPass _deferredFinalPass = new DeferredFinalPass();
    private ScreenSpaceShadowPass _screenSpaceShadowPass = new ScreenSpaceShadowPass();
    private PointLightsPass _pointLightsPass = new PointLightsPass();
    private CopyDepthPass _copyDepthPass = new CopyDepthPass();
    private CopyLightTexturePass _copyLightTexturePass = new CopyLightTexturePass();
    private PointLightTileCullingPass _pointLightTileCullingPass = new PointLightTileCullingPass();
    private DrawSkyboxPass _drawSkyboxPass = new DrawSkyboxPass();
    private PostEffectsPass _postEffectsPass = new PostEffectsPass();
    
    Matrix4x4 _matrixVP;
    Matrix4x4 _matrixVPI;
    Matrix4x4 _matrixVPprev;
    Matrix4x4 _matrixVPIprev;

    public BSRP(bool hdr, float renderScale, ShadowSettings shadowSettings, BloomSettings bloomSettings,
        ComputeShader pointLightTileCullingCompute)
    {
        _renderScale = renderScale;
        _shadowSettings = shadowSettings;

        _pointLightTileCullingCompute = pointLightTileCullingCompute;
        _deferredFinalPassMaterial = CoreUtils.CreateEngineMaterial("Hidden/DeferredFinalPass");
        _defferedLightingMaterial = CoreUtils.CreateEngineMaterial("Hidden/DeferredLights");
        _screenSpaceShadowMaterial = CoreUtils.CreateEngineMaterial("Hidden/ScreenSpaceShadow");
        _postFXMaterial = CoreUtils.CreateEngineMaterial("Hidden/PostEffectPasses");
        
        QualitySettings.shadows = ShadowQuality.All;
        GraphicsSettings.useScriptableRenderPipelineBatching = true;
        _bloomSettings = bloomSettings;
        _container = new ContextContainer();
        
        _useLensDirtKeyword = GlobalKeyword.Create("_USE_LENSDIRT");
        _useBloomKeyWord = GlobalKeyword.Create("_USE_BLOOM");
    }


    protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
    {
        BeginContextRendering(context, cameras);

        foreach (Camera camera in cameras)
        {
            
            Shader.SetKeyword(_useBloomKeyWord, _bloomSettings.BloomEnable);
            Shader.SetKeyword(_useLensDirtKeyword, _bloomSettings.UseLensDirt);
            
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

            BeginCameraRendering(context, camera);

            Matrix4x4 viewMatrix = camera.worldToCameraMatrix;
            Matrix4x4 projectionMatrix = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
            _matrixVP = projectionMatrix * viewMatrix;
            _matrixVPI = _matrixVP.inverse;

            Shader.SetGlobalMatrix(BSRPResources.UnityMatrixIvpID, _matrixVPI);

            LightingResources lightingResources =
                _lightingSetupPass.ExecuteLightngPass(RenderGraph, cullingResults, _shadowSettings);

            _setupPass.SetupDestinationTextures(RenderGraph, textureSize, camera, _container);

            _drawGeometryPass.DrawGeometry(RenderGraph, _commonShaderTags, camera, cullingResults,
                _container, camera.cullingMask, true);

            if (camera.clearFlags == CameraClearFlags.Skybox)
                _drawSkyboxPass.DrawSkybox(RenderGraph, _container, camera);

            _copyDepthPass.ExecuteCopyDepthPass(RenderGraph, _container);

            _screenSpaceShadowPass.DrawScreenSpaceShadow(RenderGraph, _container, lightingResources,
                _shadowSettings, _screenSpaceShadowMaterial);

            _directionalLight.DrawDirectinalLight(RenderGraph, _container, _defferedLightingMaterial);

            var pointLightCullingData =
                _pointLightTileCullingPass.ExecuteTileCullingPass(RenderGraph, _container,
                    _pointLightTileCullingCompute);

            _pointLightsPass.ExecutePointLightPass(RenderGraph, _container, pointLightCullingData,
                _defferedLightingMaterial);
            
            _copyLightTexturePass.ExecuteCopyLightTexturePass(RenderGraph, _container);
            
            _postEffectsPass.ExecutePostFXPass(RenderGraph, _bloomSettings,_container, _postFXMaterial );

            _deferredFinalPass.DrawDeferredFinalPass(RenderGraph, _container, _deferredFinalPassMaterial);

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
        CoreUtils.Destroy(_screenSpaceShadowMaterial);
        CoreUtils.Destroy(_defferedLightingMaterial);
        CoreUtils.Destroy(_deferredFinalPassMaterial);
        _container.Dispose();
        RenderGraph.Cleanup();
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
    }
}