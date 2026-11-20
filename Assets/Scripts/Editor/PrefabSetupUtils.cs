using System;
using System.IO;
using ShiftMagicSpell.Core;
using ShiftMagicSpell.Data;
using ShiftMagicSpell.View;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ShiftMagicSpell.Editor
{
    /// <summary>
    /// 처음 한 번 프리팹(키, 빈 홈, SHIFT 칸, 슬롯, 플레이어, 손)을 만든다. 이미 있는 프리팹은 덮어쓰지 않는다(에디터에서 고친 내용 유지).
    /// 위치 값은 픽셀(16px = 1유닛) 기준이고, 스프라이트 피벗은 캔버스 왼쪽 아래다.
    /// </summary>
    public static class PrefabSetupUtils
    {
        public const string PREFAB_DIR = "Assets/Prefabs";
        public const string KEY_PATH = PREFAB_DIR + "/Board/Key.prefab";
        public const string HOME_PATH = PREFAB_DIR + "/Board/KeyHome.prefab";
        public const string SHIFT_KEY_PATH = PREFAB_DIR + "/Board/ShiftKey.prefab";
        public const string SLOT_PATH = PREFAB_DIR + "/Board/Slot.prefab";
        public const string PLAYER_PATH = PREFAB_DIR + "/Actors/Player.prefab";
        public const string HAND_PATH = PREFAB_DIR + "/Actors/Hand.prefab";
        public const string WARNING_CELL_PATH = PREFAB_DIR + "/Fx/WarningCell.prefab";
        public const string BULLET_PATH = PREFAB_DIR + "/Fx/Bullet.prefab";
        public const string FLOAT_TEXT_PATH = PREFAB_DIR + "/Fx/FloatText.prefab";

        private static ViewAssets Assets => ViewAssets.Instance;
        private static ArtCatalog Catalog => ViewAssets.Instance.Catalog;

        public static void CreateMissing()
        {
            LoadOrCreate(KEY_PATH, BuildKey);
            LoadOrCreate(HOME_PATH, BuildHome);
            LoadOrCreate(SHIFT_KEY_PATH, BuildShiftKey);
            LoadOrCreate(SLOT_PATH, BuildSlot);
            LoadOrCreate(PLAYER_PATH, BuildPlayer);
            LoadOrCreate(HAND_PATH, BuildHand);
            LoadOrCreate(WARNING_CELL_PATH, BuildWarningCell);
            LoadOrCreate(BULLET_PATH, BuildBullet);
            LoadOrCreate(FLOAT_TEXT_PATH, BuildFloatText);
        }

        public static GameObject LoadOrCreate(string path, Func<GameObject> build)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                return existing;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            GameObject temp = build();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, path);
            Object.DestroyImmediate(temp);
            return prefab;
        }

        private static GameObject BuildKey()
        {
            var root = new GameObject("Key");
            SpriteRenderer body = AddRenderer(root, Catalog.GetFrame("Keycap", "Base", 0).Sprite, RenderOrderUtils.KEY);
            SpriteRenderer band = AddChildRenderer(root, "Band", Catalog.GetFrame("KeyRowBand", "Band", 0).Sprite, Vector2.zero, Vector2.one, RenderOrderUtils.KEY + 1);
            band.enabled = false;
            SpriteRenderer hint = AddChildRenderer(root, "Hint", Assets.WhitePixel, new Vector2(2f, 6f), new Vector2(12f, 12f), RenderOrderUtils.KEY + 2);
            hint.enabled = false;
            SpriteRenderer matched = AddChildRenderer(root, "Matched", Catalog.GetFrame("KeyMatched", "Glow", 0).Sprite, Vector2.zero, Vector2.one, RenderOrderUtils.KEY + 3);
            matched.enabled = false;

            // 글자 5x7: 키 왼쪽 위에서 (5, 5) → 왼쪽 아래 기준 (5, 20 - 5 - 7)
            PixelFontUtils.TryGetGlyph(Catalog, 'A', out ArtFrame glyph);
            SpriteRenderer letter = AddChildRenderer(root, "Letter", glyph.Sprite, new Vector2(5f, 8f), Vector2.one, RenderOrderUtils.KEY + 4);
            KeyView view = root.AddComponent<KeyView>();
            SetRefs(view, ("_body", body), ("_band", band), ("_hint", hint), ("_matched", matched), ("_letter", letter));
            return root;
        }

        private static GameObject BuildHome()
        {
            var root = new GameObject("KeyHome");
            SpriteRenderer fill = AddChildRenderer(root, "Fill", Assets.WhitePixel, new Vector2(1f, 1f), new Vector2(14f, 18f), RenderOrderUtils.BOARD_BASE);
            fill.color = Assets.Colors.KeyHome;
            return root;
        }

        private static GameObject BuildShiftKey()
        {
            var root = new GameObject("ShiftKey");
            SpriteRenderer body = AddRenderer(root, Catalog.GetFrame("Keycap", "Shift", 0).Sprite, RenderOrderUtils.BOARD_BASE);
            SpriteRenderer cooldown = AddChildRenderer(root, "Cooldown", Assets.WhitePixel, new Vector2(0f, 4f), new Vector2(16f, 16f), RenderOrderUtils.BOARD_BASE + 1);
            cooldown.enabled = false;
            ShiftKeyView view = root.AddComponent<ShiftKeyView>();
            SetRefs(view, ("_body", body), ("_cooldown", cooldown));
            return root;
        }

        private static GameObject BuildSlot()
        {
            var root = new GameObject("Slot");
            SpriteRenderer slotBase = AddRenderer(root, Catalog.GetFrame("AttackSlot", "Empty", 0).Sprite, RenderOrderUtils.BOARD_BASE);
            var pivot = new GameObject("BlockPivot");
            pivot.transform.SetParent(root.transform, false);
            pivot.transform.localPosition = Px(8f, 10f);
            SpriteRenderer block = AddChildRenderer(pivot, "Block", Catalog.GetFrame("AttackBlock", "Idle", 0).Sprite, new Vector2(-8f, -10f), Vector2.one, RenderOrderUtils.BLOCK);
            SpriteRenderer icon = AddChildRenderer(pivot, "Icon", Catalog.GetFrame("SpellIcon", "Fire", 0).Sprite, new Vector2(-6f, -4f), Vector2.one, RenderOrderUtils.BLOCK + 1);
            pivot.SetActive(false);
            SlotView view = root.AddComponent<SlotView>();
            SetRefs(view, ("_base", slotBase), ("_blockPivot", pivot.transform), ("_block", block), ("_icon", icon));
            return root;
        }

        private static GameObject BuildPlayer()
        {
            var root = new GameObject("Player");

            // 16x16 몸을 칸 왼쪽 아래에서 10px 위에(키 윗면에 서 있게)
            SpriteRenderer body = AddChildRenderer(root, "Body", Catalog.GetFrame("Player", "Idle", 0).Sprite, new Vector2(0f, 10f), Vector2.one, RenderOrderUtils.PLAYER);
            SpriteRenderer shiftOutline = AddChildRenderer(root, "ShiftOutline", Assets.Outline, new Vector2(-1f, 9f), Vector2.one, RenderOrderUtils.PLAYER + 1);
            shiftOutline.enabled = false;
            SpriteRenderer debugOutline = AddChildRenderer(root, "DebugOutline", Assets.Outline, new Vector2(-2f, 8f), Vector2.one * (20f / 18f), RenderOrderUtils.PLAYER + 1);
            debugOutline.enabled = false;
            PlayerView view = root.AddComponent<PlayerView>();
            SetRefs(view, ("_body", body), ("_shiftOutline", shiftOutline), ("_debugOutline", debugOutline));
            return root;
        }

        private static GameObject BuildHand()
        {
            var root = new GameObject("Hand");
            SpriteRenderer shadow = AddChildRenderer(root, "Shadow", Assets.Shadow, new Vector2(0f, -2f), new Vector2(14f / 32f, 6f / 16f), RenderOrderUtils.HAND - 1);
            shadow.color = new Color(0f, 0f, 0f, 0.45f);
            shadow.enabled = false;

            // 32x32 손 그림 가운데가 루트(손의 로직 위치)에 오게
            SpriteRenderer body = AddChildRenderer(root, "Body", Catalog.GetFrame("Hand", "Idle", 0).Sprite, new Vector2(-16f, -16f), Vector2.one, RenderOrderUtils.HAND);
            HandView view = root.AddComponent<HandView>();
            SetRefs(view, ("_body", body), ("_shadow", shadow));
            return root;
        }

        private static GameObject BuildWarningCell()
        {
            var root = new GameObject("WarningCell");
            SpriteRenderer frame = AddRenderer(root, Catalog.GetFrame("KeyWarning", "Blink", 0).Sprite, RenderOrderUtils.WARNING + 1);
            SpriteRenderer fill = AddChildRenderer(root, "Fill", Assets.WhitePixel, new Vector2(1f, 5f), new Vector2(14f, 14f), RenderOrderUtils.WARNING);
            fill.color = Assets.Colors.Warn.WithAlpha(0.28f);
            fill.enabled = false;

            // 줄 섞기 미는 양 표시: 칸 오른쪽 위에 걸친 빨간 딱지
            var badge = new GameObject("Badge");
            badge.transform.SetParent(root.transform, false);
            SpriteRenderer badgeBack = AddChildRenderer(badge, "Back", Assets.WhitePixel, new Vector2(4f, 15f), new Vector2(13f, 9f), RenderOrderUtils.WARNING + 2);
            badgeBack.color = Assets.Colors.Warn;
            var textObject = new GameObject("Text");
            textObject.transform.SetParent(badge.transform, false);
            textObject.transform.localPosition = Px(5f, 23f);
            PixelText text = textObject.AddComponent<PixelText>();
            HudSetupUtils.SetFields(text, ("_text", "→1"), ("_color", Color.white), ("_sortingOrder", RenderOrderUtils.WARNING + 3));
            badge.SetActive(false);
            WarningCellView view = root.AddComponent<WarningCellView>();
            SetRefs(view, ("_frame", frame), ("_fill", fill), ("_badge", badge), ("_badgeText", text));
            return root;
        }

        private static GameObject BuildBullet()
        {
            var root = new GameObject("Bullet");
            AddRenderer(root, Catalog.GetFrame("SummonBullet", "Spin", 0).Sprite, RenderOrderUtils.WARNING + 10);
            return root;
        }

        private static GameObject BuildFloatText()
        {
            var root = new GameObject("FloatText");
            PixelText text = root.AddComponent<PixelText>();
            HudSetupUtils.SetFields(text, ("_text", "-1"), ("_align", (int)TextAlign.Center), ("_hasShadow", true), ("_sortingOrder", RenderOrderUtils.FX + 2000));
            return root;
        }

        public static SpriteRenderer AddRenderer(GameObject target, Sprite sprite, int sortingOrder)
        {
            SpriteRenderer renderer = target.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        public static SpriteRenderer AddChildRenderer(GameObject parent, string childName, Sprite sprite, Vector2 positionPx, Vector2 scale, int sortingOrder)
        {
            var child = new GameObject(childName);
            child.transform.SetParent(parent.transform, false);
            child.transform.localPosition = Px(positionPx.x, positionPx.y);
            child.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            return AddRenderer(child, sprite, sortingOrder);
        }

        public static Vector3 Px(float x, float y)
        {
            return new Vector3(x / ViewUtils.PPU, y / ViewUtils.PPU, 0f);
        }

        public static void SetRefs(Object target, params (string Field, Object Value)[] refs)
        {
            var serialized = new SerializedObject(target);
            foreach ((string field, Object value) in refs)
            {
                SerializedProperty property = serialized.FindProperty(field);
                if (property == null)
                {
                    Debug.LogError($"[ERROR] PrefabSetupUtils::SetRefs() - Field not found. Details: [Target={target.GetType().Name}, Field={field}]");
                    continue;
                }

                property.objectReferenceValue = value;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetInt(Object target, string field, int value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
