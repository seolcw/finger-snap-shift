using System.IO;
using ShiftMagicSpell.Data;
using ShiftMagicSpell.View;
using UnityEditor;
using UnityEngine;

namespace ShiftMagicSpell.Editor
{
    /// <summary>
    /// 화면 공용 에셋 준비: 코드로 만드는 단순 스프라이트(흰 점, 그림자 타원, 테두리)를 PNG로 저장하고,
    /// `Assets/Resources/ViewAssets.asset`이 없으면 만들어 참조를 채운다. 이미 있으면 인스펙터 값은 건드리지 않는다.
    /// </summary>
    public static class ViewAssetSetupUtils
    {
        public const string GENERATED_DIR = "Assets/Art/Generated";
        public const string VIEW_ASSETS_PATH = "Assets/Resources/ViewAssets.asset";

        [MenuItem("Tools/Shift Magic Spell/Create View Assets (keeps edits)")]
        public static void Setup()
        {
            Directory.CreateDirectory(GENERATED_DIR);
            Directory.CreateDirectory("Assets/Resources");
            Sprite whitePixel = WriteSprite("WhitePixel", 1, 1, (x, y) => true, SpriteAlignment.BottomLeft);
            Sprite shadow = WriteSprite("Shadow", 32, 16, (x, y) =>
            {
                float dx = (x + 0.5f - 16f) / 16f;
                float dy = (y + 0.5f - 8f) / 8f;
                return dx * dx + dy * dy <= 1f;
            }, SpriteAlignment.Center);
            Sprite outline = WriteSprite("Outline", 18, 18, (x, y) => x == 0 || y == 0 || x == 17 || y == 17, SpriteAlignment.BottomLeft);

            ViewAssets assets = AssetDatabase.LoadAssetAtPath<ViewAssets>(VIEW_ASSETS_PATH);
            if (assets == null)
            {
                assets = ScriptableObject.CreateInstance<ViewAssets>();
                AssetDatabase.CreateAsset(assets, VIEW_ASSETS_PATH);
            }

            assets.Catalog ??= AssetDatabase.LoadAssetAtPath<ArtCatalog>(ArtCatalogBuildUtils.CATALOG_PATH);
            assets.WhitePixel ??= whitePixel;
            assets.Shadow ??= shadow;
            assets.Outline ??= outline;
            EditorUtility.SetDirty(assets);
            AssetDatabase.SaveAssets();
            Debug.Log($"[INFO] ViewAssetSetupUtils::Setup() - View assets ready. Details: [Path={VIEW_ASSETS_PATH}]");
        }

        private static Sprite WriteSprite(string name, int width, int height, System.Func<int, int, bool> isFilled, SpriteAlignment pivot)
        {
            string path = $"{GENERATED_DIR}/{name}.png";
            if (!File.Exists(path))
            {
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        texture.SetPixel(x, y, isFilled(x, y) ? Color.white : Color.clear);
                    }
                }

                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);
            }

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ViewUtils.PPU;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)pivot;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
