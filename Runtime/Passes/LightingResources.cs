

namespace Barkar.BSRP.CameraRenderer
{
    public readonly ref struct LightingResources
    {
        public readonly UnityEngine.Rendering.RenderGraphModule.BufferHandle DirectionalLightBuffer;
        public readonly UnityEngine.Rendering.RenderGraphModule.TextureHandle DirectionalShadowMap;


        public LightingResources(UnityEngine.Rendering.RenderGraphModule.BufferHandle directionalLightBuffer,
            UnityEngine.Rendering.RenderGraphModule.TextureHandle directionalShadowMap)
        {
            DirectionalLightBuffer = directionalLightBuffer;
            DirectionalShadowMap = directionalShadowMap;
        }
    }
}
