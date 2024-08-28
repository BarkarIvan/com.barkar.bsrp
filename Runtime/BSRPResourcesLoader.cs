using UnityEditor;
using UnityEngine;

namespace Barkar.BSRP
{
    public class BSRPResourcesLoader
    {
        public static ComputeShader PointLightsTileCullingComputeShader { get; private set; }
        public static ComputeShader RenderTransparencyCompute { get; private set; }
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void LoadStaticAssetsRuntime()
        {
            LoadStaticAssets();
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void LoadStaticAssetsEditor()
        {
            LoadStaticAssets();
        }
#endif

        private static void LoadStaticAssets()
        {
            PointLightsTileCullingComputeShader = Resources.Load<ComputeShader>("PointLightsTileCullingComputeShader");
            RenderTransparencyCompute = Resources.Load<ComputeShader>("PerPixelLInkedListCompute");
        }
    }
}

