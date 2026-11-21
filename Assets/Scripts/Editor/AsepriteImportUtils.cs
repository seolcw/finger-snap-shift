using UnityEditor;
using UnityEditor.U2D.Aseprite;
using UnityEngine;

namespace ShiftMagicSpell.Editor
{
    /// <summary>
    /// Assets/Art 아래 .aseprite 파일의 임포트 설정을 한 번에 맞춘다.
    /// 1칸(16px) = 1유닛, 점 필터, 무압축, 캔버스 왼쪽 아래 피벗(프레임마다 잘린 크기가 달라도 위치가 흔들리지 않게).
    /// </summary>
    public static class AsepriteImportUtils
    {
        public const int PIXELS_PER_UNIT = 16;
        private const string ART_ROOT = "Assets/Art";

        [MenuItem("Tools/Shift Magic Spell/Apply Aseprite Import Settings")]
        public static void ApplyAll()
        {
            string[] guids = AssetDatabase.FindAssets(string.Empty, new[] { ART_ROOT });
            int changedCount = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!path.EndsWith(".aseprite"))
                    {
                        continue;
                    }

                    if (AssetImporter.GetAtPath(path) is AsepriteImporter importer && Apply(importer))
                    {
                        importer.SaveAndReimport();
                        changedCount++;
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            Debug.Log($"[INFO] AsepriteImportUtils::ApplyAll() - Import settings applied. Details: [ChangedCount={changedCount}]");
        }

        private static bool Apply(AsepriteImporter importer)
        {
            bool isChanged = false;
            if (!Mathf.Approximately(importer.spritePixelsPerUnit, PIXELS_PER_UNIT))
            {
                importer.spritePixelsPerUnit = PIXELS_PER_UNIT;
                isChanged = true;
            }

            if (importer.filterMode != FilterMode.Point)
            {
                importer.filterMode = FilterMode.Point;
                isChanged = true;
            }

            if (importer.pivotSpace != PivotSpaces.Canvas)
            {
                importer.pivotSpace = PivotSpaces.Canvas;
                isChanged = true;
            }

            if (importer.pivotAlignment != SpriteAlignment.BottomLeft)
            {
                importer.pivotAlignment = SpriteAlignment.BottomLeft;
                isChanged = true;
            }

            // 태그·프레임 시간은 ArtCatalog를 구울 때 .aseprite 파일에서 직접 읽으므로 프리팹·클립은 만들지 않는다.
            if (importer.generateModelPrefab || importer.generateAnimationClips || importer.addSortingGroup)
            {
                importer.generateModelPrefab = false;
                importer.generateAnimationClips = false;
                importer.addSortingGroup = false;
                isChanged = true;
            }

            // WebGL을 따로 덮어쓰지 않았으면 기본 플랫폼 설정이 돌아온다.
            TextureImporterPlatformSettings settings = importer.GetImporterPlatformSettings(BuildTarget.WebGL);
            if (settings.textureCompression != TextureImporterCompression.Uncompressed)
            {
                settings.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SetImporterPlatformSettings(settings);
                isChanged = true;
            }

            return isChanged;
        }
    }
}
