using System.Runtime.InteropServices;
using Barkar.BSRP.CameraRenderer;
using Barkar.BSRP.Settings.Shadows;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

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
        private static readonly ProfilingSampler _profilingSampler = new ProfilingSampler("LightingPass");

        private static int _directionalLightsCount;
        private static DirectionalLightShadowData _directionalLightShadowData;

        private static DirectionalLightData[] _directionalLightData =
            new DirectionalLightData[maxDirectionalLightsCount];

        private static CullingResults _cullingResults;
        private static ShadowSettings _shadowSettings;

        private static BaseRenderFunc<LightingPassData, RenderGraphContext> _renderFunc;

        static LightingPass()
        {
            _renderFunc = RenderFunction;
        }

        public static LightingResources ExecuteLightngPass(RenderGraph renderGraph, CullingResults cullingResults,
            ShadowSettings shadowSettings)
        {
            _cullingResults = cullingResults;
            _shadowSettings = shadowSettings;
            using RenderGraphBuilder builder = renderGraph.AddRenderPass<LightingPassData>(_profilingSampler.name,
                out var lightingPassData, _profilingSampler);


            var textureDescriptor = new TextureDesc((int)_shadowSettings.Direcrional.MapSize,
                (int)_shadowSettings.Direcrional.MapSize);
            textureDescriptor.depthBufferBits = DepthBits.Depth32;
            textureDescriptor.isShadowMap = true;
            textureDescriptor.name = "Directional Light ShadowMap";

            //TODO: default res
            lightingPassData.ShadowMap = builder.ReadWriteTexture(renderGraph.CreateTexture(textureDescriptor));

            var directionalLightDataBufferDescriptor = new BufferDesc();
            directionalLightDataBufferDescriptor.name = "Directional Light Data Buffer";
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
                        if (_directionalLightsCount < maxDirectionalLightsCount && light.shadows != LightShadows.None &&
                            light.shadowStrength > 0f)
                        {
                            _directionalLightShadowData = new DirectionalLightShadowData()
                            {
                                visibleLightIndex = i,
                                shadowNearPlane = light.shadowNearPlane,
                                shadowBias = light.shadowBias
                            };


                            _directionalLightData[i] =
                                new DirectionalLightData(ref visibleLight, -1,
                                    new Vector4(light.shadowStrength, light.shadowBias, 0f, 0f));


                            _directionalLightsCount++;
                        }
                        break;
                }
            }

            builder.SetRenderFunc(_renderFunc);

            return new LightingResources(lightingPassData.DirectionalLightDataBuffer, lightingPassData.ShadowMap);
        }

        private static void RenderFunction(LightingPassData lightingPassData, RenderGraphContext context)
        {
            if (_directionalLightsCount > 0)
            {
                var cmd = context.cmd;

                cmd.SetRenderTarget(lightingPassData.ShadowMap, RenderBufferLoadAction.DontCare,
                    RenderBufferStoreAction.Store);
                cmd.ClearRenderTarget(true, false, Color.clear);
                cmd.BeginSample("Directional Shadow");
                context.renderContext.ExecuteCommandBuffer(cmd);
                cmd.Clear();


                _cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
                    _directionalLightShadowData.visibleLightIndex,
                    0, 1, Vector3.zero,
                    (int)_shadowSettings.Direcrional.MapSize, _directionalLightShadowData.shadowNearPlane,
                    out Matrix4x4 shadowViewMatrix,
                    out Matrix4x4 shadowProjectionMatrix, out ShadowSplitData splitData);

                Matrix4x4 shadowMatrix = ApplyBiasMatrix(shadowProjectionMatrix * shadowViewMatrix);

                cmd.SetGlobalMatrix("_DirectionalLightMatrix", shadowMatrix);
                cmd.SetViewProjectionMatrices(shadowViewMatrix, shadowProjectionMatrix);
                cmd.SetGlobalDepthBias(0f, _directionalLightShadowData.shadowBias);
                context.renderContext.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                var shadowSeettings =
                    new ShadowDrawingSettings(_cullingResults, _directionalLightShadowData.visibleLightIndex);
                shadowSeettings.useRenderingLayerMaskTest = false;
                shadowSeettings.objectsFilter = ShadowObjectsFilter.AllObjects;
                var directionalRendererListHandle = context.renderContext.CreateShadowRendererList(ref shadowSeettings);

                cmd.DrawRendererList(directionalRendererListHandle);

                //Buffers
                cmd.SetBufferData(lightingPassData.DirectionalLightDataBuffer, _directionalLightData);
                cmd.SetGlobalConstantBuffer(lightingPassData.DirectionalLightDataBuffer, "DirectionalLightDataBuffer",
                    0, DirectionalLightData.stride);
                cmd.SetGlobalTexture("_ShadowMap", lightingPassData.ShadowMap);

                cmd.EndSample("Directional Shadow");
                context.renderContext.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                cmd.SetGlobalDepthBias(0f, 0f);
                context.renderContext.ExecuteCommandBuffer(cmd);
                cmd.Clear();
            }
        }


        private static Matrix4x4 ApplyBiasMatrix(Matrix4x4 m)
        {
            if (SystemInfo.usesReversedZBuffer)
            {
                m.m20 = -m.m20;
                m.m21 = -m.m21;
                m.m22 = -m.m22;
                m.m23 = -m.m23;
            }

            Matrix4x4 biasMatrix = Matrix4x4.TRS(new Vector3(0.5f, 0.5f, 0.5f), Quaternion.identity,
                new Vector3(0.5f, 0.5f, 0.5f));

            return biasMatrix * m;
        }
    }
}