using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace fd.OnionRing
{
    public class TextureSlicerMenuItem
    {
        [MenuItem(itemName: MENU_ITEM, isValidateFunction: false, priority: ORDER_PRIORITY)]
        private static void SliceTextureMenuItem()
        {
            SliceTextures(Selection.objects, false);
        }

        [MenuItem(itemName: MENU_ITEM, isValidateFunction: true, priority: ORDER_PRIORITY)]
        private static bool SliceTextureMenuItemValidation()
        {
            return IsValid(Selection.objects);
        }
        
        [MenuItem(itemName: MENU_ITEM_OVERRIDE, isValidateFunction: false, priority: ORDER_PRIORITY_OVERRIDE)]
        private static void SliceTextureAndOverrideMenuItem()
        {
            SliceTextures(Selection.objects, true);
        }

        [MenuItem(itemName: MENU_ITEM_OVERRIDE, isValidateFunction: true, priority: ORDER_PRIORITY_OVERRIDE)]
        private static bool SliceTextureAndOverrideMenuItemValidation()
        {
            return IsValid(Selection.objects);
        }

        private static bool IsValid(Object[] objects)
        {
            if (objects == null || objects.Length == 0)
            {
                return false;
            }

            return objects.All(o => o is Texture2D);
        }

        private static void SliceTextures(Object[] objects, bool isOverride)
        {
            var textures = objects
                .Where(o=> o is Texture2D)
                .Cast<Texture2D>()
                .ToArray();
            
            SliceTextures(textures, isOverride);
        }

        private static void SliceTextures(Texture2D[] textures, bool isOverride)
        {
            for (int i = 0; i < textures.Length; i++)
            {
                var texture = textures[i];
                SliceTexture(texture, isOverride);
            }
        }

        private static void SliceTexture(Texture2D texture, bool isOverride)
        {
            TextureImporter sourceTextureImporter = null;
            string inTexturePath = null;
            bool valid = ValidateTexture(texture, out sourceTextureImporter, out inTexturePath);

            if (!valid)
            {
                Debug.LogWarningFormat("TextureSlicerMenuItem -> SlicedTexture: valid = '{0}'", valid);
                return;
            }

            string directoryPath = Path.GetDirectoryName(inTexturePath);

            string extension = Path.GetExtension(inTexturePath);
            string textureName = texture.name;

            bool readable = sourceTextureImporter.isReadable;
            sourceTextureImporter.isReadable = true;
            AssetDatabase.ImportAsset(inTexturePath, ImportAssetOptions.ForceUpdate);

            var spriteBorder = sourceTextureImporter.spriteBorder;

            int width = texture.width;
            int height = texture.height;

            int xStart = (int)spriteBorder.x;
            int yStart = (int)spriteBorder.y;

            int xEnd = (int)spriteBorder.z;
            int yEnd = (int)spriteBorder.w;
            if (xEnd != 0)
            {
                xEnd = width - xEnd;
            }

            if (yEnd != 0)
            {
                yEnd = height - yEnd;
            }

            var outTexture = TextureSlicer.Slice(texture, xStart, xEnd, yStart, yEnd);

            sourceTextureImporter.isReadable = readable;
            AssetDatabase.ImportAsset(inTexturePath, ImportAssetOptions.ForceUpdate);

            string outTextureName = textureName;

            if (isOverride)
            {
                outTextureName += "_sliced";
            }

            outTexture.name = outTextureName;

            string outTexturePath = SaveTexture(outTexture, directoryPath, outTextureName, extension,
                isOverride);

            CopyTextureSettings(inTexturePath, outTexturePath);

            GameObject.DestroyImmediate(outTexture);

            if (string.IsNullOrEmpty(outTexturePath))
            {
                Debug.LogErrorFormat("TextureSlicerMenuItem -> SlicedTexture: outTextureName = '{0}'", outTextureName);
                return;
            }

            var outTextureImporter = AssetImporter.GetAtPath(outTexturePath) as TextureImporter;
            if (outTextureImporter == null)
            {
                Debug.LogErrorFormat("TextureSlicerMenuItem -> SlicedTexture: outTextureImporter = '{0}'",
                    outTextureImporter);
                return;
            }

            outTextureImporter.spriteBorder = spriteBorder;
            AssetDatabase.ImportAsset(outTexturePath, ImportAssetOptions.ForceUpdate);

            Debug.LogFormat("TextureSlicerMenuItem -> SliceTexture -> tempTexturePath = '{0}'", outTexturePath);
        }

        private static bool ValidateTexture(Texture2D texture, out TextureImporter textureImporter,
            out string texturePath)
        {
            textureImporter = null;

            texturePath = AssetDatabase.GetAssetPath(texture);
            if (string.IsNullOrEmpty(texturePath))
            {
                Debug.LogWarningFormat("TextureSlicerMenuItem -> ValidateTexture: texturePath = '{0}'",
                    texturePath);
                return false;
            }

            textureImporter = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (textureImporter == null)
            {
                Debug.LogWarningFormat("TextureSlicerMenuItem -> ValidateTexture: sourceTextureImporter = '{0}'",
                    textureImporter);
                return false;
            }

            if (textureImporter.spritesheet.Length > 1)
            {
                Debug.LogWarningFormat("TextureSlicerMenuItem -> ValidateTexture: spritesheet = '{0}'",
                    textureImporter.spritesheet.Length);
                return false;
            }

            if (textureImporter.spriteBorder.normalized == Vector4.zero)
            {
                Debug.LogWarningFormat("TextureSlicerMenuItem -> ValidateTexture: spriteBorder = '{0}'",
                    textureImporter.spriteBorder);
                return false;
            }

            string directoryPath = Path.GetDirectoryName(texturePath);

            if (string.IsNullOrEmpty(directoryPath))
            {
                Debug.LogWarningFormat("TextureSlicerMenuItem -> ValidateTexture: directoryPath = '{0}'",
                    directoryPath);
                return false;
            }

            return true;
        }

        private static string SaveTexture(Texture2D texture, string directoryPath, string fileName, string extension,
            bool isOverride)
        {
            if (!extension.StartsWith("."))
            {
                extension = $".{extension}";
            }

            string outTexturePath = Path.Combine(directoryPath, $"{fileName}{extension}");
            
            if (!isOverride)
            {
                outTexturePath = AssetDatabase.GenerateUniqueAssetPath(outTexturePath);
            }

            var bytes = texture.EncodeToPNG();
            File.WriteAllBytes(outTexturePath, bytes);

            AssetDatabase.ImportAsset(outTexturePath, ImportAssetOptions.ForceUpdate);

            return outTexturePath;
        }

        private static void CopyTextureSettings(string sourcePath, string outPath) 
        {
            if (string.Equals(sourcePath, outPath))
            {
                return;
            }
            
            try
            {
                var sourceImporter = AssetImporter.GetAtPath(sourcePath) as TextureImporter;
                var outImporter = AssetImporter.GetAtPath(outPath) as TextureImporter;
                outImporter.textureType = sourceImporter.textureType;
                outImporter.spriteImportMode = sourceImporter.spriteImportMode;
                outImporter.SaveAndReimport();
            }
            catch (Exception exception)
            {
                Debug.LogError("TextureSlicerMenuItem -> CopyTextureSettings: " + exception.Message);
                Debug.LogException(exception);
            }
        }

        const string MENU_ITEM = "Assets/Tools: OnionRing->9-Slice";
        const int ORDER_PRIORITY = int.MinValue + 100;
        const string MENU_ITEM_OVERRIDE = "Assets/Tools: OnionRing->9-Slice & Overrider";
        const int ORDER_PRIORITY_OVERRIDE = int.MinValue + 101;
    }
}
