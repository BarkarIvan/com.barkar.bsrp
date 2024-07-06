Shader "Hidden/TestFinalPass"
{
   
    SubShader
    {
        Tags
        {
            "RenderPipeline"="BSRP" "Queue"="Geometry"
        }

        Cull Off
     //   Blend One One, Zero One
       // BlendOp Add, Add
        ZWrite On
        ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex DefaultPassVertex
            #pragma fragment LightPassFragment

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SOFT_SHADOWS_LOW _SOFT_SHADOWS_MEDIUM _SOFT_SHADOWS_HIGH

            #pragma multi_compile_fog

            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x

            #include "Packages/com.barkar.bsrp/ShaderLibrary/Common.hlsl"
            #include "Packages/com.barkar.bsrp/ShaderLibrary/Surface.hlsl"
            #include "Packages/com.barkar.bsrp/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"
            #include "Packages/com.barkar.bsrp/ShaderLibrary/CustomBRDF.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/AmbientProbe.hlsl"
    #include "Packages/com.barkar.bsrp/ShaderLibrary/CameraRendererPasses.hlsl"

            //to inputs include

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_AdditionalMap);
            SAMPLER(sampler_AdditionalMap);
            TEXTURE2D(_EmissionMap);
            SAMPLER(sampler_EmissionMap);
            TEXTURE2D(_BrushTexture);
            SAMPLER(sampler_BrushTexture);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _BaseMap_ST;
                half4 _AdditionalMap_ST;
                half3 _EmissionColor;
                half _Brightness;

                half _Metallic;
                half _Smoothness;
                half _AlphaClip;
                half _NormalMapScale;

                half3 _RimColor;
                half _RimSmooth;

                half3 _MediumColor;
                half _MediumThreshold;

                half3 _ShadowColor;
                half _ShadowThreshold;

                half3 _ReflectColor;
                half _ReflectThreshold;

                half _MediumSmooth;
                half _ShadowSmooth;
                half _ReflectSmooth;
                half _RimThreshold;

                half4 _BrushTexture_ST;
                half _MedBrushStrength;
                half _ShadowBrushStrength;
                half _ReflectBrushStrength;
            CBUFFER_END

            

            struct Attributes
            {
                float3 positionOS : POSITION;
                half3 normalOS : NORMAL;
                half4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };
            
TEXTURE2D_HALF(_GBuffer0); //albedo, smoothness
TEXTURE2D_HALF(_GBuffer1); //radiance, metallic
TEXTURE2D_HALF(_GBuffer2); //normal
TEXTURE2D_HALF(_GBuffer3); //emission
            TEXTURE2D_HALF(_CameraDepth);

            

           

           

            half4 LightPassFragment(Varyings IN): SV_Target
            {

                half4 g0 = SAMPLE_TEXTURE2D(_GBuffer0,sampler_linear_clamp, IN.uv);
                half4 g1 = SAMPLE_TEXTURE2D(_GBuffer1,sampler_linear_clamp, IN.uv);
                float4 g2 = SAMPLE_TEXTURE2D(_GBuffer2,sampler_linear_clamp, IN.uv);
                float4 g3 = SAMPLE_TEXTURE2D(_GBuffer3,sampler_linear_clamp, IN.uv);

                half3 albedo = g0.rgb;
                half smoothness = g0.a;
                half3 radiance = g1.rgb;
                half metallic = g1.a;
                float3 normal = g2.rgb * 2 - 1;
                half3 emission = g3;
               // half3 indirectDiffuse = IN.SH;

                float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepth, sampler_linear_clamp, IN.uv);
                float linearDepth = Linear01Depth(depth, _ZBufferParams);

                float4 positionNDC = float4(IN.uv * 2 - 1, depth, 1);
                float4 positionWS = mul(unity_MatrixIVP, positionNDC);//float4(IN.positionCS.xy, depth, 1.0));// positionNDC);
                positionWS *= rcp(positionWS.w);


                //Light light = GetMainLight(shadowCoord, IN.positionWS);
                half NoL = dot(normal, MainLightDirectionaAndMask.xyz);

                
                half3 lightColor = MainLightColor;

                //radiance
                Surface surface;
                surface.albedo = albedo;
                surface.normal = (normal);
                surface.metallic = metallic;
                surface.smoothness = smoothness;
                surface.viewDir = SafeNormalize(_WorldSpaceCameraPos - positionWS.xyz);
                //brdf
                Light light = GetMainLight(positionWS);
                BRDF brdf = GetBRDF(surface);
                lightColor *= DirectBRDF(surface, brdf, light); //* radiance;// * albedo.a;
                half3 indirectDiffuse = 1.0;// SampleSH(surface.normal);
                half3 go = EnvironmentBRDF(surface, brdf, indirectDiffuse, lightColor, radiance);

                //reflectionProbe
                half3 envirReflection = GetReflectionProbe(surface);
                //envirReflection *= surface.metallic + MIN_REFLECTIVITY;
                envirReflection *= albedo.rgb;
                half4 result;
                result.rgb = (go + envirReflection) * radiance;
              //  result.a = surface.alpha;

               

                //rim
              //  #if defined (_RIM)
              //  half NoV = dot(surface.viewDir, surface.normal);
              //  half3 rim = smoothstep(1 - _RimThreshold, 1 - _RimThreshold - _RimSmooth, saturate(NoV)) * _RimColor;
               // result.rgb += rim;
               /// result.rgb = saturate(result.rgb);
               // #endif

                //Emission
               // half3 emissionColor = emission;

               
               // result.rgb += saturate(emission);

                //LOD
                //  #ifdef LOD_FADE_CROSSFADE
                //  LODFadeCrossFade(IN.positionCS);
                // #endif

                //FOG
              //  #if (defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2))
             //   result.rgb = CalculateFog(result, IN.positionWS);
             //   #endif

               //result = half4(albedo,1.0);
               // result.rgb = AcesTonemap(result.rgb);
                return half4(result.rgb, 1.0);
            }
            ENDHLSL
        }
    }
}