using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

namespace Barkar.BSRP.Editor.ShaderEditor
{
    public class BSRPDiffractionLitShaderEditor : ShaderGUI
    {
        private MaterialEditor materialEditor;
        private MaterialProperty[] properties;

        private MaterialProperty _BaseMap;
        private MaterialProperty _BaseColor;
        private MaterialProperty _AdditionalMap;
        private MaterialProperty _UsingNormalMap;
        private MaterialProperty _NormalMapScale;
        private MaterialProperty _Metallic;
        private MaterialProperty _Roughness;
        private MaterialProperty _EmissionColor;
        private MaterialProperty _EmissionMap;
        private MaterialProperty _Brightness;
        private MaterialProperty _UseAlphaClip;
        private MaterialProperty _AlphaClip;
        private MaterialProperty _Cull;
        private MaterialProperty _Blend1;
        private MaterialProperty _Blend2;

        private MaterialProperty _ZWrite;

        // private analyticSpecularBRDFMode     = BaseShaderGUI.FindProperty("_AnalyticSpecularBRDFMode", properties, false);
        private MaterialProperty _NoiseImplementation;
        private MaterialProperty _DiffractionWidth;
        private MaterialProperty _DiffractionHeight;

        private MaterialProperty _DiffractionVarR;
        private MaterialProperty _DiffractionVarG;
        private MaterialProperty _DiffractionVarB;
        private MaterialProperty _DiffractionCovarRG;
        private MaterialProperty _DiffractionCovarRB;
        private MaterialProperty _DiffractionCovarGB;
        private MaterialProperty _DiffractionCovInitMatRow1;
        private MaterialProperty _DiffractionCovInitMatRow2;
        private MaterialProperty _DiffractionCovInitMatRow3;

        private MaterialProperty _DiffractionPattern;
        private MaterialProperty _DiffractionStereoSpecularity;
        private MaterialProperty _DiffractionZWScale;
        private MaterialProperty _DiffractionUVscalingX;
        private MaterialProperty _DiffractionUVscalingY;

        // Enum declarations
        public enum CullEnum
        {
            Off = 0,
            Front = 1,
            Back = 2
        }

        public enum BlendModes
        {
            Opaque = 0,
            Transparent = 1,
            Fade = 2,
        }

        public enum BlendModeEnum
        {
            Zero = 0,
            One = 1,
            DstColor = 2,
            SrcColor = 3,
            OneMinusDstColor = 4,
            SrcAlpha = 5,
            OneMinusSrcColor = 6,
            DstAlpha = 7,
            OneMinusDstAlpha = 8,
            SrcAlphaSaturate = 9,
            OneMinusSrcAlpha = 10
        }

        public enum ZWriteEnum
        {
            On = 1,
            Off = 0
        }

        private BlendModes _blendMode;


        private void UpdateCholeskyDecomposition()
        {
            Vector4 column1 = new Vector4(_DiffractionVarR.floatValue,
                _DiffractionCovarRG.floatValue,
                _DiffractionCovarRB.floatValue,
                0);

            Vector4 column2 = new Vector4(_DiffractionCovarRG.floatValue,
                _DiffractionVarG.floatValue,
                _DiffractionCovarGB.floatValue,
                0);

            Vector4 column3 = new Vector4(_DiffractionCovarRB.floatValue,
                _DiffractionCovarGB.floatValue,
                _DiffractionVarB.floatValue,
                0);

            Matrix4x4 cov_init = new Matrix4x4(column1, column2, column3, new Vector4(0, 0, 0, 0));
            Matrix4x4 L = Matrix4x4.zero;


            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < (i + 1); j++)
                {
                    float sum = 0;
                    for (int k = 0; k < j; k++)
                        sum += L[i, k] * L[j, k];

                    if (i == j)
                        L[i, j] = Mathf.Sqrt(cov_init[i, i] - sum);
                    else
                        L[i, j] = ((1.0f / L[j, j]) * (cov_init[i, j] - sum));
                }
            }

            /////////////////////
            _DiffractionCovInitMatRow1.vectorValue = new Vector4(L[0, 0], L[0, 1], L[0, 2], 0);
            _DiffractionCovInitMatRow2.vectorValue = new Vector4(L[1, 0], L[1, 1], L[1, 2], 0);
            _DiffractionCovInitMatRow3.vectorValue = new Vector4(L[2, 0], L[2, 1], L[2, 2], 0);
        }

        public override void ValidateMaterial(Material material)
        {
            UpdateCholeskyDecomposition();
        }


        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            this.materialEditor = materialEditor;
            this.properties = properties;

            FindProperties();

            EditorGUI.BeginChangeCheck();
            {
                EditorGUILayout.HelpBox("MAIN TEXTURE", MessageType.None);

                materialEditor.TextureProperty(_BaseMap, "Albedo");
                materialEditor.ColorProperty(_BaseColor, "Object Color");
                EditorGUILayout.Space(30);
                EditorGUILayout.HelpBox("AdditionalMap (Normals, Smoothness, metallic)", MessageType.None);
                materialEditor.TextureProperty(_AdditionalMap, "Additional Map");
                if (_AdditionalMap.textureValue != null)
                {
                    materialEditor.ShaderProperty(_UsingNormalMap, "Use Normal Map");
                    materialEditor.RangeProperty(_NormalMapScale, "Normal Map Scale");
                }
                else
                {
                    materialEditor.RangeProperty(_Metallic, "Metallic");
                    materialEditor.RangeProperty(_Roughness, "Roughness");
                    EditorGUILayout.Space(30);
                }

                EditorGUILayout.Space(30);

                EditorGUILayout.HelpBox("EMISSION", MessageType.None);
                materialEditor.TextureProperty(_EmissionMap, "EmissionMap");
                materialEditor.ColorProperty(_EmissionColor, "EmissionColor");
                EditorGUILayout.Space(30);

                materialEditor.RangeProperty(_Brightness, "Brightness");
                EditorGUILayout.Space(30);
                materialEditor.ShaderProperty(_UseAlphaClip, "Use Alpha Clip");
                if (_UseAlphaClip.floatValue == 1)
                {
                    materialEditor.RangeProperty(_AlphaClip, "Alpha Clip Threshold");
                }
                
                EditorGUILayout.HelpBox("DIFFRACTION", MessageType.None);
                materialEditor.TextureProperty(_EmissionMap, "EmissionMap");
                materialEditor.ColorProperty(_EmissionColor, "EmissionColor");
                EditorGUILayout.Space(30);
                
                materialEditor.ShaderProperty(_DiffractionPattern, "Diffraction Pattern");
                materialEditor.ShaderProperty(_DiffractionWidth,   "Diffraction Width");
                materialEditor.ShaderProperty(_DiffractionHeight,  "Diffraction Height");

                materialEditor.ShaderProperty(_DiffractionVarR, "Var_R");
                materialEditor.ShaderProperty(_DiffractionVarG, "Var_G");
                materialEditor.ShaderProperty(_DiffractionVarB, "Var_B");
                materialEditor.ShaderProperty(_DiffractionCovarRG, "Covar_RG");
                materialEditor.ShaderProperty(_DiffractionCovarRB, "Covar_RB");
                materialEditor.ShaderProperty(_DiffractionCovarGB, "Covar_GB");

                materialEditor.ShaderProperty(_DiffractionUVscalingX, "UV Correction X ");
                materialEditor.ShaderProperty(_DiffractionUVscalingY, "UV Correction Y");
                materialEditor.ShaderProperty(_DiffractionZWScale, "Spatio-Temporal Pattern-Shift factor");
                materialEditor.ShaderProperty(_DiffractionStereoSpecularity, "Diffraction Stereoscopic Specularity");



                _Cull.floatValue = (float)(CullEnum)EditorGUILayout.EnumPopup("Cull", (CullEnum)_Cull.floatValue);
                EditorGUILayout.Space();
                _blendMode = (BlendModes)EditorGUILayout.EnumPopup("Blend Mode", _blendMode);
                // 
                switch (_blendMode)
                {
                    case BlendModes.Opaque:
                        _Blend1.floatValue = (int)BlendModeEnum.One;
                        _Blend2.floatValue = (int)BlendModeEnum.Zero;
                        _ZWrite.floatValue = (int)ZWriteEnum.On;

                        break;
                    case BlendModes.Transparent:
                        _Blend1.floatValue = (int)BlendModeEnum.SrcAlpha;
                        _Blend2.floatValue = (int)BlendModeEnum.OneMinusSrcAlpha;
                        _ZWrite.floatValue = (int)ZWriteEnum.Off;

                        break;
                    case BlendModes.Fade:
                        _Blend1.floatValue = (int)BlendModeEnum.SrcAlpha;
                        _Blend2.floatValue = (int)BlendModeEnum.OneMinusSrcAlpha;
                        _ZWrite.floatValue = (int)ZWriteEnum.On;

                        break;
                }

                materialEditor.RenderQueueField();
            }
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var obj in materialEditor.targets)
                {
                    if (obj is Material material)
                    {
                        SetKeyword(material, "_DIRECT_LIGHT_BRDF_DIFFRACTION_PATTERN",
                            _DiffractionPattern.floatValue == 1.0f);
                        SetKeyword(material, "_DIFFRACTION_PATTERN_OPEN_SIMPLEX_2", true);//
                           //material.GetFloat("_NoiseImplementationDiffraction") == 1.0f);
                        SetKeyword(material, "_DIRECT_LIGHT_BRDF_DIFFRACTION", true);
                        SetKeyword(material, "_USEALPHACLIP", _UseAlphaClip.floatValue == 1);
                        SetKeyword(material, "_NORMALMAP",
                            _UsingNormalMap.floatValue == 1 && _AdditionalMap.textureValue != null);
                        SetKeyword(material, "_ADDITIONALMAP", _AdditionalMap.textureValue != null);
                        SetKeyword(material, "_EMISSION", _EmissionMap.textureValue != null);
                    }
                }
            }
        }

        private BlendModes GetBlendModeFromMaterialProperties()
        {
            if (_Blend1.floatValue == (int)BlendModeEnum.SrcAlpha &&
                _Blend2.floatValue == (int)BlendModeEnum.OneMinusSrcAlpha)
            {
                if (_ZWrite.floatValue == (int)ZWriteEnum.On) return BlendModes.Fade;

                return BlendModes.Transparent;
            }

            return BlendModes.Opaque;
        }

        /// <summary>
        /// Sets the keyword of a material.
        /// </summary>
        /// <param name="material">The material to set the keyword for.</param>
        /// <param name="keyword">The keyword to set.</param>
        /// <param name="enabled">Specifies whether the keyword should be enabled or disabled.</param>
        private void SetKeyword(Material material, string keyword, bool enabled)
        {
            if (enabled)
            {
                material.EnableKeyword(keyword);
            }
            else
            {
                material.DisableKeyword(keyword);
            }
        }

        /// <summary>
        /// Finds the properties of the material.
        /// </summary>
        private void FindProperties()
        {
            _BaseMap = FindProperty("_BaseMap");
            _BaseColor = FindProperty("_BaseColor");
            _AdditionalMap = FindProperty("_AdditionalMap");
            _UsingNormalMap = FindProperty("_UsingNormalMap");
            _NormalMapScale = FindProperty("_NormalMapScale");
            _UseAlphaClip = FindProperty("_UseAlphaClip");
            _AlphaClip = FindProperty("_AlphaClip");
            _Metallic = FindProperty("_Metallic");
            _Roughness = FindProperty("_Roughness");
            _Brightness = FindProperty("_Brightness");
            _EmissionMap = FindProperty("_EmissionMap");
            _EmissionColor = FindProperty("_EmissionColor");

            _NoiseImplementation = FindProperty("_NoiseImplementationDiffraction");
            _DiffractionWidth = FindProperty("_DiffractionWidth");
            _DiffractionHeight = FindProperty("_DiffractionHeight");

            _DiffractionVarR = FindProperty("_DiffractionVar_R");
            _DiffractionVarG = FindProperty("_DiffractionVar_G");
            _DiffractionVarB = FindProperty("_DiffractionVar_B");
            _DiffractionCovarRG = FindProperty("_DiffractionCovar_RG");
            _DiffractionCovarRB = FindProperty("_DiffractionCovar_RB");
            _DiffractionCovarGB = FindProperty("_DiffractionCovar_GB");
            _DiffractionCovInitMatRow1 = FindProperty("_DiffractionCovInit_Row_1");
            _DiffractionCovInitMatRow2 = FindProperty("_DiffractionCovInit_Row_2");
            _DiffractionCovInitMatRow3 = FindProperty("_DiffractionCovInit_Row_3");

            _DiffractionPattern = FindProperty("_DiffractionPatternToggle");
            _DiffractionStereoSpecularity = FindProperty("_DiffractionStereoSpecularity");
            _DiffractionZWScale = FindProperty("_DiffractionZW_Scale");
            _DiffractionUVscalingX = FindProperty("_DiffractionUV_ScaleX");
            _DiffractionUVscalingY = FindProperty("_DiffractionUV_ScaleY");

            _Cull = FindProperty("_Cull");
            _Blend1 = FindProperty("_Blend1");
            _Blend2 = FindProperty("_Blend2");
            _ZWrite = FindProperty("_ZWrite");
            _blendMode = _blendMode = GetBlendModeFromMaterialProperties();
        }

        /// <summary>
        /// Finds the properties of the material.
        /// </summary>
        /// <param name="propertyName">The name of the property to find.</param>
        /// <returns>The MaterialProperty object for the given property name.</returns>
        private MaterialProperty FindProperty(string propertyName)
        {
            MaterialProperty prop = FindProperty(propertyName, properties);

            if (prop == null)
            {
                Debug.LogError("Property " + propertyName + " not found");
                return null;
            }

            return prop;
        }
    }
}