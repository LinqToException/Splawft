using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Splawft
{
    /// <summary>
    /// A semi-singleton responsible for the dumping of material and textures.
    /// </summary>
    internal class MaterialDumper
    {
        private static readonly Serilog.ILogger logger = Serilog.Log.ForContext<MaterialDumper>();

        private enum ShaderPropertyType
        {
            Color = 0,
            Vector = 1,
            Float = 2,
            Range = 2, // we don't really care in THAT detail.
            TexEnv = 4
        }

        /// <summary>
        /// A magic array that tries to map the built-in shader names to the file ids.
        /// By no means complete and quite frankly disappointing.
        /// </summary>
        private static readonly Dictionary<string, int> shaderFileIds = new Dictionary<string, int>
        {
            { "Standard", 46 },
            { "Standard (Specular setup)", 45 },
            { "Unlit/Color", 10755 },
            { "Unlit/Texture", 10752 }
        };

        // Magic code that made this enum:
        ////var propertyCount = ShaderUtil.GetPropertyCount(shader);
        ////Properties = this.Properties.Union(Enumerable.Range(0, propertyCount).Select(i => new
        ////{
        ////    Type = ShaderUtil.GetPropertyType(shader, i),
        ////    Name = ShaderUtil.GetPropertyName(shader, i),
        ////    Description = ShaderUtil.GetPropertyDescription(shader, i)
        ////}).Select(a => $"(ShaderPropertyType.{a.Type}, \"{a.Name}\")")).ToArray();
        ////Debug.Log(string.Join(", ", Properties));
        /// <summary>
        /// A list of properties that shaders are known to have.
        /// </summary>
        private static readonly (ShaderPropertyType Type, string Name)[] properties = new[] 
        {
            (ShaderPropertyType.Color, "_Color"),
            (ShaderPropertyType.TexEnv, "_MainTex"), 
            (ShaderPropertyType.Range, "_Cutoff"), 
            (ShaderPropertyType.Range, "_Glossiness"), 
            (ShaderPropertyType.Range, "_GlossMapScale"), 
            (ShaderPropertyType.Float, "_SmoothnessTextureChannel"), 
            (ShaderPropertyType.Range, "_Metallic"), 
            (ShaderPropertyType.TexEnv, "_MetallicGlossMap"), 
            (ShaderPropertyType.Float, "_SpecularHighlights"), 
            (ShaderPropertyType.Float, "_GlossyReflections"), 
            (ShaderPropertyType.Float, "_BumpScale"), 
            (ShaderPropertyType.TexEnv, "_BumpMap"), 
            (ShaderPropertyType.Range, "_Parallax"), 
            (ShaderPropertyType.TexEnv, "_ParallaxMap"), 
            (ShaderPropertyType.Range, "_OcclusionStrength"), 
            (ShaderPropertyType.TexEnv, "_OcclusionMap"), 
            (ShaderPropertyType.Color, "_EmissionColor"), 
            (ShaderPropertyType.TexEnv, "_EmissionMap"), 
            (ShaderPropertyType.TexEnv, "_DetailMask"), 
            (ShaderPropertyType.TexEnv, "_DetailAlbedoMap"), 
            (ShaderPropertyType.Float, "_DetailNormalMapScale"), 
            (ShaderPropertyType.TexEnv, "_DetailNormalMap"), 
            (ShaderPropertyType.Float, "_UVSec"), 
            (ShaderPropertyType.Float, "_Mode"), 
            (ShaderPropertyType.Float, "_SrcBlend"), 
            (ShaderPropertyType.Float, "_DstBlend"), 
            (ShaderPropertyType.Float, "_ZWrite"), 
            (ShaderPropertyType.Color, "_SpecColor"), 
            (ShaderPropertyType.TexEnv, "_SpecGlossMap")
        };

        private static Dictionary<Material, string> materialGuids = new Dictionary<Material, string>();

        private readonly string outputDirectory;
        private readonly UnityDumper unityDumper;
        private readonly TextureDumper textureDumper;

        public MaterialDumper(UnityDumper unityDumper, string outputDirectory, TextureDumper textureDumper)
        {
            this.unityDumper = unityDumper;
            this.textureDumper = textureDumper;

            this.outputDirectory = outputDirectory;
            if (!Directory.Exists(outputDirectory))
                Directory.CreateDirectory(outputDirectory);
        }

        public string DumpMaterial(Material material)
        {
            if (material == null)
                return null;

            if (materialGuids.TryGetValue(material, out var result))
                return result;

            var valuesByList = properties.Select(prop => new { prop.Type, prop.Name, Value = GetValue(material, prop.Type, prop.Name) })
                .Where(a => a.Value != null)
                .ToLookup(p => p.Type);

            Dictionary<string, object> GetValues(ShaderPropertyType type) => valuesByList[type].ToDictionary(p => p.Name, p => p.Value);

            var savedProperties = new
            {
                serializedVersion = 3,
                m_Floats = GetValues(ShaderPropertyType.Float),
                m_Colors = GetValues(ShaderPropertyType.Color),
                m_TexEnvs = valuesByList[ShaderPropertyType.TexEnv].ToDictionary(p => p.Name, p => GetTextureData(material, p.Name, p.Value as Texture))
            };

            var materialText = $@"%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!21 &2100000
Material:
  serializedVersion: 6
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: {material.name}
  m_Shader: {{fileID: {GetShaderId(material.shader)}, guid: 0000000000000000f000000000000000, type: 0}}
  m_ShaderKeywords: {string.Join(" ", material.shaderKeywords)}
  m_LightmapFlags: {(int)material.globalIlluminationFlags}
  m_EnableInstancingVariants: {(material.enableInstancing ? 1 : 0)}
  m_DoubleSidedGI: {(material.doubleSidedGI ? 1 : 0)}
  m_CustomRenderQueue: {material.renderQueue}
  stringTagMap: {{}}
  disabledShaderPasses: []
  m_SavedProperties: {unityDumper.Jsonify(savedProperties)}";

            var guid = materialText.ToMd5Guid();
            materialGuids.Add(material, guid);
            if (File.Exists(Path.Combine(outputDirectory, guid + ".mat.meta")))
                return guid;

            File.WriteAllText(Path.Combine(outputDirectory, guid + ".mat.meta"), $@"fileFormatVersion: 2
guid: {guid}
NativeFormatImporter:
  externalObjects: {{}}
  mainObjectFileID: 2100000
  userData: 
  assetBundleName: 
  assetBundleVariant: 
");
            File.WriteAllText(Path.Combine(outputDirectory, guid + ".mat"), materialText);
            return guid;
        }

        private static int GetShaderId(Shader shader)
        {
            if (shaderFileIds.TryGetValue(shader.name, out var value))
                return value;

            logger.Warning("Could not find fileId for shader {Shader} ({ShaderName})!", shader.ToString(), shader.name);
            // Default with 46. Because I think that's standard. Or was. Or will be. Or must be.
            return 46;
        }

        private object GetTextureData(Material material, string name, Texture texture)
        {
            object texObj;

            if (!(texture is Texture2D tex2d) || tex2d == null)
                texObj = new { fileID = 0 };
            else
                texObj = new { fileID = 2800000, guid = this.textureDumper.DumpTexture(tex2d), type = 3 };

            return new
            {
                m_Texture = texObj,
                m_Scale = material.GetTextureScale(name),
                m_Offset = material.GetTextureOffset(name)
            };
        }

        private static object GetValue(Material material, ShaderPropertyType type, string name)
        {
            switch (type)
            {
                case ShaderPropertyType.Color:
                    return material.GetColor(name);
                case ShaderPropertyType.Float:
                    return material.GetFloat(name);
                ////case ShaderPropertyType.Range:
                ////    return material.GetFloat(name);
                case ShaderPropertyType.TexEnv:
                    return material.GetTexture(name);
                case ShaderPropertyType.Vector:
                    return material.GetVector(name);
                default:
                    return null;
            }
        }
    }
}
