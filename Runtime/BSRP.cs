using System.Collections.Generic;
using Barkar.BSRP;
using Barkar.BSRP.CameraRenderer;
using Barkar.BSRP.Passes;
using Barkar.BSRP.Passes.Bloom;
using Barkar.BSRP.Passes.Setup;
using Barkar.BSRP.Settings;
using Barkar.BSRP.Settings.GTAO;
using Barkar.BSRP.Settings.Shadows;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

public class BSRP : RenderPipeline
{
    private float _renderScale;

    private RenderGraph RenderGraph = new RenderGraph("BSRP Render Graph");
    private TextureHandle _backbuffer;

    private readonly ShaderTagId[] _commonShaderTags =
        { new ShaderTagId("BSRPGBuffer"), new ShaderTagId("SRPDefaultUnlit") };
    private readonly ShaderTagId[] _depthNormalsShaderTag = {new ShaderTagId("DepthNormalsOnly")};

    private readonly ShaderTagId[] _ppllShaderTagId = { new ShaderTagId("BSRPPPLL") };
    private ContextContainer _container;

    private Vector2Int _textureSize = default;
    private RenderGraphParameters _renderGraphParameters;

    private ShadowSettings _shadowSettings;
    private BloomSettings _bloomSettings;

    private Material _defferedLightingMaterial;
    private Material _deferredFinalPassMaterial;
    private Material _screenSpaceShadowMaterial;
    private Material _postFXMaterial;
    private Material _gtaoMaterial;

    private GlobalKeyword _useLensDirtKeyword;
    private GlobalKeyword _useBloomKeyWord;

    private LightingSetupPass _lightingSetupPass = new LightingSetupPass();
    private SetupPass _setupPass = new SetupPass();
    private DepthNormalsOnlyPass _depthNormalsOnlyPass = new DepthNormalsOnlyPass();
    private DrawOpaquePass _drawOpaquePass = new DrawOpaquePass();
    private CreatePerPixelLinkedListPass _createPerPixelLinkedListPass = new CreatePerPixelLinkedListPass();
    private RenderPerPixelLinkedListPass _renderPerPixelLinkedListPass = new RenderPerPixelLinkedListPass();
    private DirectionalLightPass _directionalLightPass = new DirectionalLightPass();
    private DeferredFinalPass _deferredFinalPass = new DeferredFinalPass();
    private ScreenSpaceShadowPass _screenSpaceShadowPass = new ScreenSpaceShadowPass();
    private PointLightsPass _pointLightsPass = new PointLightsPass();
    private CopyDepthPass _copyDepthPass = new CopyDepthPass();
    private CopyLightTexturePass _copyLightTexturePass = new CopyLightTexturePass();
    private PointLightTileCullingPass _pointLightTileCullingPass = new PointLightTileCullingPass();
    private DrawSkyboxPass _drawSkyboxPass = new DrawSkyboxPass();
    private BloomPass _bloomPass = new BloomPass();

    //GTAO
    private GTAOPass _gtaoPass = new GTAOPass();
    private GTAOSettings _GTAOsettings;
    
    //TONEMAP
    private TonemappingSettings _tonemapSettings;

    Matrix4x4 _matrixVP;
    Matrix4x4 _matrixVPI;
    Matrix4x4 _matrixVPprev;
    Matrix4x4 _matrixVPIprev;

    public BSRP(float renderScale, ShadowSettings shadowSettings, BloomSettings bloomSettings, GTAOSettings gtaoSettings, TonemappingSettings tonemapsettings)
    {
        _renderScale = renderScale;
        _shadowSettings = shadowSettings;
        _tonemapSettings = tonemapsettings;

        _deferredFinalPassMaterial = CoreUtils.CreateEngineMaterial("Hidden/DeferredFinalPass");
        _defferedLightingMaterial = CoreUtils.CreateEngineMaterial("Hidden/DeferredLights");
        _screenSpaceShadowMaterial = CoreUtils.CreateEngineMaterial("Hidden/ScreenSpaceShadow");
        _postFXMaterial = CoreUtils.CreateEngineMaterial("Hidden/PostEffectPasses");
        _gtaoMaterial = CoreUtils.CreateEngineMaterial("Hidden/GTAO");

        QualitySettings.shadows = ShadowQuality.All;
        GraphicsSettings.useScriptableRenderPipelineBatching = true;
        _bloomSettings = bloomSettings;
        _container = new ContextContainer();

        _GTAOsettings = gtaoSettings;

        _useLensDirtKeyword = GlobalKeyword.Create("_USE_LENSDIRT");
        _useBloomKeyWord = GlobalKeyword.Create("_USE_BLOOM");
    }


    protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
    {
        BeginContextRendering(context, cameras);

        foreach (Camera camera in cameras)
        {
            context.SetupCameraProperties(camera, false);
            Shader.SetKeyword(_useBloomKeyWord, _bloomSettings.BloomEnable);
            Shader.SetKeyword(_useLensDirtKeyword, _bloomSettings.UseLensDirt);

            _textureSize.x = (int)(camera.pixelWidth * _renderScale);
            _textureSize.y = (int)(camera.pixelHeight * _renderScale);

            if (!camera.TryGetCullingParameters(out ScriptableCullingParameters cullingParameters)) continue;
            cullingParameters.shadowDistance = Mathf.Min(camera.farClipPlane, _shadowSettings.ShadowMaxDistance);
            CullingResults cullingResults = context.Cull(ref cullingParameters);

#if UNITY_EDITOR
            if (camera.cameraType == CameraType.SceneView)
            {
                ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
                _textureSize.x = camera.pixelWidth;
                _textureSize.y = camera.pixelHeight;
            }
#endif
            _renderGraphParameters = new RenderGraphParameters
            {
                commandBuffer = CommandBufferPool.Get(),
                currentFrameIndex = Time.frameCount,
                executionName = "Camera Render Graph",
                rendererListCulling = true,
                scriptableRenderContext = context
            };

            RenderGraph.BeginRecording(_renderGraphParameters);

            BeginCameraRendering(context, camera);

            Matrix4x4 viewMatrix = camera.worldToCameraMatrix;
            Matrix4x4 projectionMatrix = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
            _matrixVP = projectionMatrix * viewMatrix;
            _matrixVPI = _matrixVP.inverse;

            Shader.SetGlobalMatrix(BSRPShaderIDs.UnityMatrixIvpID, _matrixVPI);

            //Setup
            _lightingSetupPass.ExecutePass(RenderGraph, cullingResults, _shadowSettings, _container, camera);
            _setupPass.ExecutePass(RenderGraph, _textureSize, camera, _container);

            //depth normals
            _depthNormalsOnlyPass.ExecutePass(RenderGraph, _depthNormalsShaderTag,camera, cullingResults, _container, camera.cullingMask);
            
            //GTAO
            _gtaoPass.ExecutePass(RenderGraph, _container, _GTAOsettings, camera, _gtaoMaterial);

            //Opaque
            _drawOpaquePass.ExecutePass(RenderGraph, _commonShaderTags, camera, cullingResults,
                _container, camera.cullingMask);
           
            //Skybox
            if (camera.clearFlags == CameraClearFlags.Skybox)
                _drawSkyboxPass.ExecutePass(RenderGraph, _container, camera);
           
       
            //Directional
            _directionalLightPass.ExecutePass(RenderGraph, _container, _defferedLightingMaterial);
 
            //Point lights
            
            _pointLightTileCullingPass.ExecutePass(RenderGraph, _container);

            _pointLightsPass.ExecutePass(RenderGraph, _container,_defferedLightingMaterial);

            //PPLL
            _createPerPixelLinkedListPass.ExecutePass(RenderGraph, _ppllShaderTagId, camera, cullingResults, _container,
                camera.cullingMask);
            _renderPerPixelLinkedListPass.ExecutePass(RenderGraph, _container);
           
            //Copy colors
            _copyLightTexturePass.ExecutePass(RenderGraph, _container);

            //Bloom
            if (_bloomSettings.BloomEnable)
            {
                _bloomPass.ExecutePass(RenderGraph, _bloomSettings, _container, _postFXMaterial);
            }

            //Final compose
            _deferredFinalPass.ExecutePass(RenderGraph, _tonemapSettings, _deferredFinalPassMaterial);

            RenderGraph.EndRecordingAndExecute();
        }

        context.ExecuteCommandBuffer(_renderGraphParameters.commandBuffer);
        context.Submit();
        RenderGraph.EndFrame();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        CoreUtils.Destroy(_screenSpaceShadowMaterial);
        CoreUtils.Destroy(_defferedLightingMaterial);
        CoreUtils.Destroy(_deferredFinalPassMaterial);
        CoreUtils.Destroy(_postFXMaterial);
        CoreUtils.Destroy(_gtaoMaterial);
        _container.Dispose();
        RenderGraph.Cleanup();
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
    }
}