using System.IO;
using UnityEditor;
using UnityEngine;

namespace OnionRing {
    public class TextureSlicerMenuItem {
        [MenuItem(MenuItemName, priority = MenuItemPriority)]
        private static void SliceTextureMenuItem() {
            for (int i = 0; i < Selection.objects.Length; i++) {
                if (Selection.objects[i] is Texture2D texture2D) {
                    SlicedTexture(texture2D);
                }
            }
        }

        [MenuItem(MenuItemName, true, priority = MenuItemPriority)]
        private static bool SliceTextureMenuItemValidation() {
            for (int i = 0; i < Selection.objects.Length; i++) {
                if (!(Selection.objects[i] is Texture2D)) {
                    return false;
                }
            }

            return true;
        }

        private static void SlicedTexture(Texture2D texture) {
            TextureImporter sourceTextureImporter = null;
            string inTexturePath = null;
            bool valid = ValidateTexture(texture, out sourceTextureImporter, out inTexturePath);

            if (!valid) {
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

            int xStart = (int) spriteBorder.x;
            int yStart = (int) spriteBorder.y;

            int xEnd = (int) spriteBorder.z;
            int yEnd = (int) spriteBorder.w;
            if (xEnd != 0) {
                xEnd = width - xEnd;
            }

            if (yEnd != 0) {
                yEnd = height - yEnd;
            }

            var outTexture = TextureSlicer.Slice(texture, xStart, xEnd, yStart, yEnd);

            sourceTextureImporter.isReadable = readable;
            AssetDatabase.ImportAsset(inTexturePath, ImportAssetOptions.ForceUpdate);

            string outTextureName = $"{textureName}_sliced";
            outTexture.name = outTextureName;

            string outTexturePath = SaveTexture(outTexture, directoryPath, outTextureName, extension);
            
            GameObject.DestroyImmediate(outTexture);

            if (string.IsNullOrEmpty(outTexturePath)) {
                Debug.LogErrorFormat("TextureSlicerMenuItem -> SlicedTexture: outTextureName = '{0}'", outTextureName);
                return;
            }

            var outTextureImporter = AssetImporter.GetAtPath(outTexturePath) as TextureImporter;
            if (outTextureImporter == null) {
                Debug.LogErrorFormat("TextureSlicerMenuItem -> SlicedTexture: outTextureImporter = '{0}'",
                                     outTextureImporter);
                return;
            }

            outTextureImporter.spriteBorder = spriteBorder;
            AssetDatabase.ImportAsset(outTexturePath, ImportAssetOptions.ForceUpdate);

            Debug.LogFormat("TextureSlicerMenuItem -> SliceTexture -> tempTexturePath = '{0}'", outTexturePath);
        }

        private static bool ValidateTexture(Texture2D texture, out TextureImporter textureImporter, out string texturePath) {
            textureImporter = null;
            
            texturePath = AssetDatabase.GetAssetPath(texture);
            if (string.IsNullOrEmpty(texturePath)) {
                Debug.LogWarningFormat("TextureSlicerMenuItem -> ValidateTexture: texturePath = '{0}'",
                                       texturePath);
                return false;
            }

            textureImporter = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (textureImporter == null) {
                Debug.LogWarningFormat("TextureSlicerMenuItem -> ValidateTexture: sourceTextureImporter = '{0}'",
                                       textureImporter);
                return false;
            }

            if (textureImporter.spritesheet.Length != 0) {
                Debug.LogWarningFormat("TextureSlicerMenuItem -> ValidateTexture: spritesheet = '{0}'",
                                       textureImporter.spritesheet.Length);
                return false;
            }

            if (textureImporter.spriteBorder.normalized == Vector4.zero) {
                Debug.LogWarningFormat("TextureSlicerMenuItem -> ValidateTexture: spriteBorder = '{0}'",
                                       textureImporter.spriteBorder);
                return false;
            }

            string directoryPath = Path.GetDirectoryName(texturePath);

            if (string.IsNullOrEmpty(directoryPath)) {
                Debug.LogWarningFormat("TextureSlicerMenuItem -> ValidateTexture: directoryPath = '{0}'", directoryPath);
                return false;
            }

            return true;
        }

        private static string SaveTexture(Texture2D texture, string directoryPath, string fileName, string extension) {
            if (!extension.StartsWith(".")) {
                extension = $".{extension}";
            }

            string outTexturePath = Path.Combine(directoryPath, $"{fileName}{extension}");
            outTexturePath = AssetDatabase.GenerateUniqueAssetPath(outTexturePath);

            var bytes = texture.EncodeToPNG();
            File.WriteAllBytes(outTexturePath, bytes);

            AssetDatabase.ImportAsset(outTexturePath, ImportAssetOptions.ForceUpdate);

            return outTexturePath;
        }


        private const string MenuItemName = "Assets/OnionRing -> 9-Slice Texture";
        private const int MenuItemPriority = 1000;
    }
}
