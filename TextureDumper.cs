using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Splawft
{
    internal class TextureDumper
    {
        private readonly string outputDirectory;

        private static Dictionary<(int, int), (RenderTexture RenderTexture, Texture2D TargetTexture)> textures = new Dictionary<(int, int), (RenderTexture, Texture2D)>();
        private static Dictionary<Texture, string> guids = new Dictionary<Texture, string>();

        public TextureDumper(string outputPath)
        {
            this.outputDirectory = outputPath;
            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);
        }

        public string DumpTexture(Texture2D texture)
        {
            if (guids.TryGetValue(texture, out var result))
                return result;

            // We could argue that if the texture is readable, just read it
            // but that messes up if the format is compressed, and I don't feel like
            // finding out when THAT happens.
            byte[] bytes;

            // If we can't just read it, bad things happen
            var oldMode = texture.filterMode;

            texture.filterMode = FilterMode.Point;

            if (!textures.TryGetValue((texture.width, texture.height), out var tuple))
            {
                tuple = (RenderTexture.GetTemporary(texture.width, texture.height, 24, RenderTextureFormat.ARGB32), new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false));
                tuple.RenderTexture.hideFlags = tuple.TargetTexture.hideFlags = HideFlags.HideAndDontSave;
                textures.Add((texture.width, texture.height), tuple);
            }

            var newTex = tuple.TargetTexture;
            RenderTexture.active = tuple.RenderTexture;
            Graphics.Blit(texture, tuple.RenderTexture);
            newTex.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            newTex.Apply();

            // Not reliable - various other textures may be DXT5, without being normal maps.
            ////if (texture.format == TextureFormat.DXT5)
            ////{
            ////    // Likely a normal map, which means Unity's got the channels mixed up.
            ////    var colors = newTex.GetPixels();
            ////    for (int i = 0; i < colors.Length; i++)
            ////    {
            ////        ref var color = ref colors[i];
            ////        colors[i] = new Color(color.g, color.b, color.a, 1);
            ////    }

            ////    newTex.SetPixels(colors);
            ////}

            bytes = newTex.EncodeToPNG();
            texture.filterMode = oldMode;

            result = bytes.ToMd5Guid();
            var metaFile = Path.Combine(outputDirectory, result + ".png.meta");
            guids.Add(texture, result);

            if (!File.Exists(metaFile))
            {
                File.WriteAllBytes(Path.Combine(outputDirectory, result + ".png"), bytes);
                File.WriteAllText(metaFile, $@"fileFormatVersion: 2
guid: {result}
TextureImporter:
  serializedVersion: 7");
            }

            return result;
        }

        ////[MenuItem("Splawft/ConvertNormals")]
        ////public static void Foo()
        ////{
        ////    var textures = AssetDatabase.FindAssets("t:texture2d", new[] { "Assets/splawft/textures" });
        ////    foreach (var texture in textures)
        ////    {
        ////        var path = AssetDatabase.GUIDToAssetPath(texture);
        ////        var importer = (TextureImporter)AssetImporter.GetAtPath(path);

        ////        if (importer.textureType != TextureImporterType.NormalMap)
        ////            continue;

        ////        importer.textureType = TextureImporterType.Default;
        ////        importer.isReadable = true;
        ////        importer.SaveAndReimport();
        ////        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        ////        if (AssetDatabase.GetLabels(tex).Contains("splawft-nconv", StringComparer.OrdinalIgnoreCase))
        ////        {
        ////            Debug.Log($"{path} is already converted; skip.");
        ////            continue;
        ////        }

        ////        Debug.Log($"Convert {path}");
        ////        AssetDatabase.SetLabels(tex, new[] { "splawft-nconv" });
        ////        var newTex = new Texture2D(tex.width, tex.height);
        ////        var colors = tex.GetPixels();
        ////        importer.textureType = TextureImporterType.NormalMap;
        ////        importer.isReadable = false;

        ////        for (int i = 0; i < colors.Length; i++)
        ////        {
        ////            ref var color = ref colors[i];
        ////            color = new Color(color.a, Mathf.Sqrt(1 - color.a * color.a - color.g * color.g), color.g, 1);
        ////        }

        ////        newTex.SetPixels(colors);
        ////        newTex.Apply();
        ////        File.WriteAllBytes(path, newTex.EncodeToPNG());
        ////        importer.SaveAndReimport();
        ////    }
        ////}
    }
}
