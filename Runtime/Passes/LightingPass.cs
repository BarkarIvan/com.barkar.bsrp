using System.Runtime.InteropServices;
using Barkar.BSRP.CameraRenderer;
using Barkar.BSRP.Settings.Shadows;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace Barkar.BSRP.Passes
{
    public class LightingPassData
    {
        public TextureHandle ShadowMap;
        public BufferHandle DirectionalLightDataBuffer;
    }

    struct DirectionalLightShadowData
    {
        public int visibleLightIndex;
        public float shadowBias;
        public float shadowNearPlane;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct DirectionalLightData
    {
        public const int stride = 4 * 4 * 3;
        public Vector4 Color;
        public Vector4 DirectionAndMask;
        public Vector4 ShadowData; //light.strenght, normalBias,

        public DirectionalLightData(ref VisibleLight visibleLight, int layerMask, Vector4 shadowData)
        {
            Color = visibleLight.finalColor;
            DirectionAndMask = -visibleLight.localToWorldMatrix.GetColumn(2);
            DirectionAndMask.w = (float)layerMask;
            ShadowData = shadowData;
        }
    }


    public class LightingPass
    {
        private const int maxDirectionalLightsCount = 1;
        private const int maxPointLightCount = 1024;
        private  readonly ProfilingSampler _profilingSampler = new ProfilingSampler("LightingPass");

        private  int _directionalLightsCount;
        private  DirectionalLightShadowData _directionalLightShadowData;

        private  DirectionalLightData[] _directionalLightData =
            new DirectionalLightData[maxDirectionalLightsCount];

        private  CullingResults _cullingResults;
        private  ShadowSettings _shadowSettings;
        private  Vector4 _mainLightShadowMapSize = Vector4.zero;

        private  BaseRenderFunc<LightingPassData, RenderGraphContext> _renderFunc;

       
         public LightingPass()
        {
            _renderFunc = RenderFunction;
        }

       

        public  LightingResources ExecuteLightngPass(RenderGraph renderGraph, CullingResults cullingResults,
            ShadowSettings shadowSettings)
        {
            _cullingResults = cullingResults;
            _shadowSettings = shadowSettings;

            using (var builder = renderGraph.AddRenderPass<LightingPassData>(_profilingSampler.name,
                       out var lightingPassData, _profilingSampler))
            {

                var directionalLightDataBufferDescriptor = new BufferDesc();
                directionalLightDataBufferDescriptor.name = "Main Light Data Buffer";
                directionalLightDataBufferDescriptor.count = maxDirectionalLightsCount;
                directionalLightDataBufferDescriptor.stride = DirectionalLightData.stride;
                directionalLightDataBufferDescriptor.target = GraphicsBuffer.Target.Constant;

                lightingPassData.DirectionalLightDataBuffer =
                    builder.WriteBuffer(renderGraph.CreateBuffer(directionalLightDataBufferDescriptor));


                NativeArray<VisibleLight> visibleLights = _cullingResults.visibleLights;
                int i;
                _directionalLightsCount = 0;
                for (i = 0; i < visibleLights.Length; i++)
                {
                    VisibleLight visibleLight = visibleLights[i];
                    Light light = visibleLight.light;

                    switch (visibleLight.lightType)
                    {
                        case LightType.Directional:
                            if (_directionalLightsCount < maxDirectionalLightsCount &&
                                light.shadows != LightShadows.None &&
                                light.shadowStrength > 0f && _cullingResults.GetShadowCasterBounds(i, out _))
                            {
                                _directionalLightShadowData = new DirectionalLightShadowData()
                                {
                                    visibleLightIndex = i,
                                    shadowNearPlane = light.shadowNearPlane,
                                    shadowBias = light.shadowBias
                                };

                                _directionalLightData[i] =
                                    new DirectionalLightData(ref visibleLight, light.renderingLayerMask,
                                        new Vector4(light.shadowStrength, light.shadowNormalBias, light.shadowBias,
                                            0f));

                                _directionalLightsCount++;
                            }

                            break;

                        case LightType.Point:

                            break;

                    }
                }

                var textureDescriptor = new TextureDesc((int)_shadowSettings.Direcrional.MapSize,
                    (int)_shadowSettings.Direcrional.MapSize);
                textureDescriptor.depthBufferBits = DepthBits.Depth32;
                textureDescriptor.isShadowMap = true;
                textureDescriptor.name = "Main Light ShadowMap";

                lightingPassData.ShadowMap = builder.ReadWriteTexture(_directionalLightsCount > 0
                    ? renderGraph.CreateTexture(textureDescriptor)
                    : renderGraph.defaultResources.defaultShadowTexture);

                builder.SetRenderFunc(_renderFunc);


                return new LightingResources(lightingPassData.DirectionalLightDataBuffer, lightingPassData.ShadowMap);
            }
        }

        private  void RenderFunction(LightingPassData lightingPassData, RenderGraphContext context)
        {
                var cmd = context.cmd;

                cmd.SetRenderTarget(lightingPassData.ShadowMap, RenderBufferLoadAction.DontCare,
                    RenderBufferStoreAction.Store);
                cmd.ClearRenderTarget(true, false, Color.clear);
                cmd.BeginSample("Main Light Directional Shadow");
                context.renderContext.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                if (_directionalLightsCount > 0)
                {
                    _cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
                        _directionalLightShadowData.visibleLightIndex,
                        0, 1, new Vector3(1.0f, 0.0f, 0.0f),
                        (int)_shadowSettings.Direcrional.MapSize, _directionalLightShadowData.shadowNearPlane,
                        out Matrix4x4 shadowViewMatrix,
                        out Matrix4x4 shadowProjectionMatrix, out ShadowSplitData splitData);

                    Matrix4x4 shadowMatrix = ApplyBiasMatrix(shadowProjectionMatrix, shadowViewMatrix);

                    cmd.SetGlobalMatrix("_MainLightMatrix", shadowMatrix);
                    cmd.SetViewProjectionMatrices(shadowViewMatrix, shadowProjectionMatrix);
                    cmd.SetGlobalDepthBias(0, _directionalLightShadowData.shadowBias);
                    context.renderContext.ExecuteCommandBuffer(cmd);
                    cmd.Clear();

                    var shadowSeettings =
                        new ShadowDrawingSettings(_cullingResults, _directionalLightShadowData.visibleLightIndex);
                    shadowSeettings.useRenderingLayerMaskTest = true;
                    shadowSeettings.objectsFilter = ShadowObjectsFilter.AllObjects;
                    var directionalLightRendererListHandle =
                        context.renderContext.CreateShadowRendererList(ref shadowSeettings);

                    cmd.DrawRendererList(directionalLightRendererListHandle);
                }

                //Buffers data
                cmd.SetBufferData(lightingPassData.DirectionalLightDataBuffer, _directionalLightData);
                cmd.SetGlobalConstantBuffer(lightingPassData.DirectionalLightDataBuffer, "MainLightDataBuffer",
                    0, DirectionalLightData.stride);
                cmd.SetGlobalTexture("_MainLightShadowMap", lightingPassData.ShadowMap);

                //map size
                _mainLightShadowMapSize.x = (float)_shadowSettings.Direcrional.MapSize;
                _mainLightShadowMapSize.y = 1f / _mainLightShadowMapSize.x;
                cmd.SetGlobalVector("_MainLightShadowMapSize", _mainLightShadowMapSize);

                //distance
                cmd.SetGlobalVector("_MainLightShadowDistanceFade",
                    new Vector4(_shadowSettings.ShadowMaxDistance, _shadowSettings.ShadowDistanceFade,
                        1f / _shadowSettings.ShadowMaxDistance,
                        1f / _shadowSettings.ShadowDistanceFade)); //z - cascades fade

                //keyWords

                cmd.EndSample("Main Light Directional Shadow");
                context.renderContext.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                cmd.SetGlobalDepthBias(0f, 0f);
                context.renderContext.ExecuteCommandBuffer(cmd);
                cmd.Clear();
            
        }


        private static Matrix4x4 ApplyBiasMatrix(Matrix4x4 proj, Matrix4x4 view)
        {
            if (SystemInfo.usesReversedZBuffer)
            {
                proj.m20 = -proj.m20;
                proj.m21 = -proj.m21;
                proj.m22 = -proj.m22;
                proj.m23 = -proj.m23;
            }

            Matrix4x4 worldToShadow = proj * view;

            var textureScaleAndBias = Matrix4x4.identity;
            textureScaleAndBias.m00 = 0.5f;
            textureScaleAndBias.m11 = 0.5f;
            textureScaleAndBias.m22 = 0.5f;
            textureScaleAndBias.m03 = 0.5f;
            textureScaleAndBias.m23 = 0.5f;
            textureScaleAndBias.m13 = 0.5f;

            return textureScaleAndBias * worldToShadow;
        }
    }
}