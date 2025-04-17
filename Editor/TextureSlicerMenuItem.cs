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
            return IsValidForOverride(Selection.objects);
        }

        private static bool IsValid(Object[] objects)
        {
            if (objects == null || objects.Length == 0)
            {
                return false;
            }

            bool areTextures = objects.All(o => o is Texture2D);
            return areTextures;
        }

        private static bool IsValidForOverride(Object[] objects)
        {
            if (!IsValid(objects))
            {
                return false;
            }
            
            var textures = objects
                .Where(o => o is Texture2D)
                .Cast<Texture2D>()
                .ToArray();

            for (int i = 0; i < textures.Length; i++)
            {
                var texture = textures[i];
                if (!ValidateTexturePath(texture, out string texturePath))
                {
                    return false;
                }
                
                if (!ValidateTextureImporter(texturePath, out TextureImporter textureImporter))
                {
                    return false;
                }

                if (textureImporter.spriteImportMode == SpriteImportMode.Multiple 
                    && textureImporter.spritesheet.Length != 0)
                {
                    return false;
                }
            }

            return true;
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

            SliceTextureByBorders(texture, isOverride,
                inTexturePath, sourceTextureImporter);
        }

        private static void SliceTextureByBorders(Texture2D sourceTexture, bool isOverride,
            string sourceTexturePath, TextureImporter sourceTextureImporter)
        {
            string textureName = sourceTexture.name;
            
            MarkReadableStatus(sourceTextureImporter, true, out bool readable);
            
            if (sourceTextureImporter.spriteImportMode == SpriteImportMode.Single 
                || sourceTextureImporter.spritesheet.Length == 0)
            {
                var spriteBorder = sourceTextureImporter.spriteBorder;
                
                var spriteRectInt = new RectInt(0, 0, sourceTexture.width, sourceTexture.height);
                var outTexture = SliceTexture(sourceTexture, 
                    spriteRectInt, spriteBorder);
                
                string outTexturePath = SaveTexture(sourceTexturePath, 
                    outTexture, textureName, 
                    isOverride);
                CopyTextureSettings(sourceTexturePath, outTexturePath);
                ApplyTextureBorders(outTexturePath, spriteBorder);
                    
                Object.DestroyImmediate(outTexture);
            }
            else
            {
                var spritesheet = sourceTextureImporter.spritesheet;
                for (int i = 0; i < spritesheet.Length; i++)
                {
                    var sprite = spritesheet[i];
                    string spriteName = sprite.name;
                    var spriteBorder = sprite.border;

                    var spriteRect = sprite.rect;
                    var spriteRectInt = new RectInt(
                        (int)spriteRect.x, (int)spriteRect.y,
                        (int)spriteRect.width, (int)spriteRect.height);
                    var outTexture = SliceTexture(sourceTexture,
                        spriteRectInt, spriteBorder);
                    
                    string outTexturePath = SaveTexture(sourceTexturePath,
                        outTexture, spriteName,
                        isOverride);
                    CopyTextureSettings(sourceTexturePath, outTexturePath);
                    ApplyTextureBorders(outTexturePath, spriteBorder);

                    Object.DestroyImmediate(outTexture);
                }
            }

            MarkReadableStatus(sourceTextureImporter, readable, out _);

            Debug.LogFormat("TextureSlicerMenuItem->SliceTexture: sourceTexturePath='{0}'", sourceTexturePath);
        }

        private static Texture2D SliceTexture(Texture2D sourceTexture, RectInt rect, Vector4 bounds)
        {
            var outTexture = TextureSlicer.Slice(sourceTexture, rect, bounds);
            return outTexture;
        }

        private static bool ValidateTexture(Texture2D texture, out TextureImporter textureImporter,
            out string texturePath)
        {
            textureImporter = null;

            if (!ValidateTexturePath(texture, out texturePath))
            {
                return false;
            }
            
            if (!ValidateTextureImporter(texturePath, out textureImporter))
            {
                return false;
            }
            
            if (!ValidateBorders(textureImporter))
            {
                return false;
            }

            return true;
        }

        private static bool ValidateTexturePath(Texture2D texture, out string texturePath)
        {
            texturePath = AssetDatabase.GetAssetPath(texture);
            
            if (string.IsNullOrEmpty(texturePath))
            {
                Debug.LogWarningFormat("TextureSlicerMenuItem->ValidateTexturePath: path='{0}'",
                    texturePath);
                return false;
            }

            return true;
        }
        
        private static bool ValidateTextureImporter(string texturePath, out TextureImporter textureImporter)
        {
            textureImporter = AssetImporter.GetAtPath(texturePath) as TextureImporter;

            if (textureImporter == null)
            {
                Debug.LogWarningFormat("TextureSlicerMenuItem->ValidateTextureImporter: importer='{0}'",
                    textureImporter);
                return false;
            }

            return true;
        }

        private static bool ValidateBorders(TextureImporter textureImporter)
        {
            if (textureImporter.spriteImportMode == SpriteImportMode.Single 
                || textureImporter.spritesheet.Length == 0)
            {
                if (!ValidateBorders(textureImporter.spriteBorder))
                {
                    return false;
                }
            }
            else
            {
                var spritesheet = textureImporter.spritesheet;
                for (int i = 0; i < spritesheet.Length; i++)
                {
                    var sheet = spritesheet[i];
                    if (!ValidateBorders(sheet.border))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool ValidateBorders(Vector4 rect)
        {
            if (rect.normalized == Vector4.zero)
            {
                Debug.LogWarningFormat("TextureSlicerMenuItem->ValidateBorders: border='{0}'",
                    rect);
                return false;
            }

            return true;
        }

        private static string SaveTexture(string sourceTexturePath, 
            Texture2D outTexture, string outTextureName,
            bool isOverride)
        {
            if (!isOverride)
            {
                outTextureName += "_sliced";
            }
            
            string directoryPath = Path.GetDirectoryName(sourceTexturePath);
            string extension = Path.GetExtension(sourceTexturePath);

            string outTexturePath = SaveTexture(outTexture, directoryPath, outTextureName, extension,
                isOverride);

            return outTexturePath;
        }

        private static string SaveTexture(Texture2D texture, 
            string directoryPath, string fileName, string extension,
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

        private static void MarkReadableStatus(TextureImporter textureImporter,
            bool newStatus, out bool oldStatus)
        {
            oldStatus = textureImporter.isReadable;
            textureImporter.isReadable = newStatus;
            AssetDatabase.ImportAsset(textureImporter.assetPath, ImportAssetOptions.ForceUpdate);
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
                Debug.LogErrorFormat("TextureSlicerMenuItem->CopyTextureSettings: [{0}]",
                    exception.Message);
                Debug.LogException(exception);
            }
        }
        
        private static void ApplyTextureBorders(string outTexturePath, Vector4 borders)
        {
            try
            {
                var textureImporter = AssetImporter.GetAtPath(outTexturePath) as TextureImporter;
                textureImporter.spriteBorder = borders;
                textureImporter.SaveAndReimport();
            }
            catch (Exception exception)
            {
                Debug.LogErrorFormat("TextureSlicerMenuItem->ApplyTextureBorders: [{0}]",
                    exception.Message);
                Debug.LogException(exception);
            }
        }

        const string MENU_ITEM = "Assets/Tools: OnionRing->9-Slice";
        const int ORDER_PRIORITY = int.MinValue + 100;
        const string MENU_ITEM_OVERRIDE = "Assets/Tools: OnionRing->9-Slice & Overrider";
        const int ORDER_PRIORITY_OVERRIDE = int.MinValue + 101;
    }
}
