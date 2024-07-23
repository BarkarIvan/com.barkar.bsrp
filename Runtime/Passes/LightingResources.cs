

using UnityEngine.Rendering.RenderGraphModule;

namespace Barkar.BSRP.CameraRenderer
{
    public readonly ref struct LightingResources
    {
        public readonly BufferHandle DirectionalLightBuffer;
        public readonly TextureHandle DirectionalShadowMap;


        public LightingResources(BufferHandle directionalLightBuffer,
            TextureHandle directionalShadowMap)
        {
            DirectionalLightBuffer = directionalLightBuffer;
            DirectionalShadowMap = directionalShadowMap;
        }
    }
}
