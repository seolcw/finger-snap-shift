using ShiftMagicSpell.Data;
using ShiftMagicSpell.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.U2D;

namespace ShiftMagicSpell.Editor
{
    /// <summary>
    /// Main 씬을 처음 만들 때 쓰는 도구. 만든 뒤에는 씬과 프리팹을 에디터에서 직접 고친다.
    /// "Recreate Main Scene"은 씬을 새로 만들어 덮어쓰므로 확인을 한 번 받는다(프리팹은 이미 있으면 그대로 쓴다).
    /// </summary>
    public static class SceneSetupUtils
    {
        public const string SCENE_PATH = "Assets/Scenes/Main.unity";

        [MenuItem("Tools/Shift Magic Spell/Recreate Main Scene (overwrites scene edits)")]
        public static void RecreateFromMenu()
        {
            bool isConfirmed = EditorUtility.DisplayDialog(
                "Recreate Main Scene",
                "Main 씬을 처음 상태로 다시 만듭니다. 씬에서 직접 고친 내용은 사라집니다(프리팹·에셋은 그대로).",
                "다시 만들기",
                "취소");
            if (isConfirmed)
            {
                SetupAll();
            }
        }

        /// <summary>배치 실행용: 임포트 설정 → 데이터 → 스프라이트 목록 → 화면 에셋 → 프리팹 → 씬</summary>
        public static void SetupAll()
        {
            AsepriteImportUtils.ApplyAll();
            DataImportUtils.ImportAll();
            ArtCatalogBuildUtils.Build();
            ViewAssetSetupUtils.Setup();
            PrefabSetupUtils.CreateMissing();
            BuildMainScene();
        }

        private static void BuildMainScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildCamera();

            var world = new GameObject("World");
            BoardView left = BuildBoard(world, "Board_Left", 0);
            BoardView right = BuildBoard(world, "Board_Right", 1);
            PixelCanvas under = BuildCanvas(world, "Canvas_BoardUnder", RenderOrderUtils.BOARD_UNDER_FX);
            PixelCanvas over = BuildCanvas(world, "Canvas_BoardOver", RenderOrderUtils.BOARD_OVER_FX);
            PixelCanvas warning = BuildCanvas(world, "Canvas_Warning", RenderOrderUtils.WARNING);
            var player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabSetupUtils.PLAYER_PATH), world.transform);
            var hand = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabSetupUtils.HAND_PATH), world.transform);
            PixelCanvas fx = BuildCanvas(world, "Canvas_Fx", RenderOrderUtils.FX);
            var pools = new GameObject("FxPools");
            pools.transform.SetParent(world.transform, false);
            HudView hud = HudSetupUtils.BuildHud();
            ScreensView screens = HudSetupUtils.BuildScreens();

            var game = new GameObject("Game");
            GameRenderer renderer = game.AddComponent<GameRenderer>();
            GameRunner runner = game.AddComponent<GameRunner>();
            PrefabSetupUtils.SetRefs(renderer,
                ("_worldRoot", world.transform),
                ("_player", player.GetComponent<PlayerView>()),
                ("_hand", hand.GetComponent<HandView>()),
                ("_underCanvas", under),
                ("_overCanvas", over),
                ("_warningCanvas", warning),
                ("_fxCanvas", fx),
                ("_poolRoot", pools.transform),
                ("_warningCellPrefab", AssetDatabase.LoadAssetAtPath<WarningCellView>(PrefabSetupUtils.WARNING_CELL_PATH)),
                ("_bulletPrefab", AssetDatabase.LoadAssetAtPath<SpriteRenderer>(PrefabSetupUtils.BULLET_PATH)),
                ("_floatTextPrefab", AssetDatabase.LoadAssetAtPath<PixelText>(PrefabSetupUtils.FLOAT_TEXT_PATH)),
                ("_hud", hud),
                ("_screens", screens));
            var serialized = new SerializedObject(renderer);
            SerializedProperty boards = serialized.FindProperty("_boards");
            boards.arraySize = 2;
            boards.GetArrayElementAtIndex(0).objectReferenceValue = left;
            boards.GetArrayElementAtIndex(1).objectReferenceValue = right;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabSetupUtils.SetRefs(runner,
                ("_attackPatterns", AssetDatabase.LoadAssetAtPath<AttackPatternsData>(DataImportUtils.ATTACK_PATTERNS_PATH)),
                ("_boardLayouts", AssetDatabase.LoadAssetAtPath<BoardLayoutData>(DataImportUtils.BOARD_LAYOUTS_PATH)),
                ("_renderer", renderer));

            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            Debug.Log($"[INFO] SceneSetupUtils::BuildMainScene() - Scene saved. Details: [Path={SCENE_PATH}]");
        }

        private static void BuildCamera()
        {
            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = ViewUtils.SCREEN_H / ViewUtils.PPU * 0.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(0x14, 0x16, 0x1c, 0xff);
            camera.transform.position = new Vector3(ViewUtils.SCREEN_W / ViewUtils.PPU * 0.5f, ViewUtils.SCREEN_H / ViewUtils.PPU * 0.5f, -10f);
            PixelPerfectCamera pixelPerfect = cameraObject.AddComponent<PixelPerfectCamera>();
            pixelPerfect.assetsPPU = (int)ViewUtils.PPU;
            pixelPerfect.refResolutionX = (int)ViewUtils.SCREEN_W;
            pixelPerfect.refResolutionY = (int)ViewUtils.SCREEN_H;
            pixelPerfect.upscaleRT = true;
        }

        private static BoardView BuildBoard(GameObject world, string objectName, int boardIndex)
        {
            var boardObject = new GameObject(objectName);
            boardObject.transform.SetParent(world.transform, false);
            BoardView board = boardObject.AddComponent<BoardView>();
            PrefabSetupUtils.SetInt(board, "_boardIndex", boardIndex);
            PrefabSetupUtils.SetRefs(board,
                ("_layoutData", AssetDatabase.LoadAssetAtPath<BoardLayoutData>(DataImportUtils.BOARD_LAYOUTS_PATH)),
                ("_keyPrefab", AssetDatabase.LoadAssetAtPath<KeyView>(PrefabSetupUtils.KEY_PATH)),
                ("_homePrefab", AssetDatabase.LoadAssetAtPath<GameObject>(PrefabSetupUtils.HOME_PATH)),
                ("_shiftKeyPrefab", AssetDatabase.LoadAssetAtPath<ShiftKeyView>(PrefabSetupUtils.SHIFT_KEY_PATH)),
                ("_slotPrefab", AssetDatabase.LoadAssetAtPath<SlotView>(PrefabSetupUtils.SLOT_PATH)));
            board.RebuildCells();
            return board;
        }

        private static PixelCanvas BuildCanvas(GameObject parent, string objectName, int baseSortingOrder)
        {
            var canvasObject = new GameObject(objectName);
            if (parent != null)
            {
                canvasObject.transform.SetParent(parent.transform, false);
            }

            PixelCanvas canvas = canvasObject.AddComponent<PixelCanvas>();
            PrefabSetupUtils.SetInt(canvas, "_baseSortingOrder", baseSortingOrder);
            return canvas;
        }
    }
}
