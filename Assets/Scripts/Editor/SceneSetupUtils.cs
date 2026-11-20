using ShiftMagicSpell.Core;
using ShiftMagicSpell.Data;
using ShiftMagicSpell.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.U2D;

namespace ShiftMagicSpell.Editor
{
    /// <summary>
    /// Main 씬을 코드로 다시 만든다: 픽셀 퍼펙트 카메라(320x200, PPU 16) + 게임 오브젝트(Runner·Renderer·Canvas).
    /// 씬을 손으로 고치지 않고 이 메뉴로 만들면 설정이 항상 같다.
    /// </summary>
    public static class SceneSetupUtils
    {
        public const string SCENE_PATH = "Assets/Scenes/Main.unity";

        [MenuItem("Tools/Shift Magic Spell/Setup All (Import, Catalog, Scene)")]
        public static void SetupAll()
        {
            AsepriteImportUtils.ApplyAll();
            DataImportUtils.ImportAll();
            ArtCatalogBuildUtils.Build();
            ViewAssetSetupUtils.Setup();
            BuildMainScene();
        }

        [MenuItem("Tools/Shift Magic Spell/Build Main Scene")]
        public static void BuildMainScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = ViewUtils.SCREEN_H / ViewUtils.PPU * 0.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = PaletteUtils.Background;
            camera.transform.position = new Vector3(ViewUtils.SCREEN_W / ViewUtils.PPU * 0.5f, ViewUtils.SCREEN_H / ViewUtils.PPU * 0.5f, -10f);
            PixelPerfectCamera pixelPerfect = cameraObject.AddComponent<PixelPerfectCamera>();
            pixelPerfect.assetsPPU = (int)ViewUtils.PPU;
            pixelPerfect.refResolutionX = (int)ViewUtils.SCREEN_W;
            pixelPerfect.refResolutionY = (int)ViewUtils.SCREEN_H;
            pixelPerfect.upscaleRT = true;

            var gameObject = new GameObject("Game");
            PixelCanvas canvas = gameObject.AddComponent<PixelCanvas>();
            GameRenderer renderer = gameObject.AddComponent<GameRenderer>();
            GameRunner runner = gameObject.AddComponent<GameRunner>();

            SetReference(renderer, "_canvas", canvas);
            SetReference(renderer, "_catalog", AssetDatabase.LoadAssetAtPath<ArtCatalog>(ArtCatalogBuildUtils.CATALOG_PATH));
            SetReference(runner, "_attackPatterns", AssetDatabase.LoadAssetAtPath<AttackPatternsData>(DataImportUtils.ATTACK_PATTERNS_PATH));
            SetReference(runner, "_boardLayouts", AssetDatabase.LoadAssetAtPath<BoardLayoutData>(DataImportUtils.BOARD_LAYOUTS_PATH));
            SetReference(runner, "_renderer", renderer);

            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            Debug.Log($"[INFO] SceneSetupUtils::BuildMainScene() - Scene saved. Details: [Path={SCENE_PATH}]");
        }

        private static void SetReference(Object target, string fieldName, Object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);
            if (property == null)
            {
                Debug.LogError($"[ERROR] SceneSetupUtils::SetReference() - Field not found. Details: [Target={target.GetType().Name}, Field={fieldName}]");
                return;
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
