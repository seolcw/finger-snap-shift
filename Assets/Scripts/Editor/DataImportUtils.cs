using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ShiftMagicSpell.Data;
using UnityEditor;
using UnityEngine;

namespace ShiftMagicSpell.Editor
{
    /// <summary>
    /// 웹 프로토타입 원본(WebPrototype/src)에서 수치·배치를 읽어 ScriptableObject 에셋에 덮어쓴다.
    /// 웹에서 수치를 바꾼 뒤 이 메뉴를 다시 실행하면 Unity 쪽도 같아진다(인스펙터에서 바꾼 값은 덮어씀).
    /// </summary>
    public static class DataImportUtils
    {
        public const string ATTACK_PATTERNS_PATH = "Assets/Data/AttackPatterns.asset";
        public const string BOARD_LAYOUTS_PATH = "Assets/Data/BoardLayouts.asset";
        private const string SOURCE_DIR = "WebPrototype/src";

        [MenuItem("Tools/Shift Magic Spell/Import Prototype Data")]
        public static void ImportAll()
        {
            Directory.CreateDirectory("Assets/Data");
            ImportAttackPatterns();
            ImportBoardLayouts();
            AssetDatabase.SaveAssets();
            Debug.Log("[INFO] DataImportUtils::ImportAll() - Prototype data imported.");
        }

        private static void ImportAttackPatterns()
        {
            var root = (Dictionary<string, object>)MiniJsonUtils.Parse(File.ReadAllText(Path.Combine(SOURCE_DIR, "attackPatterns.json")));
            AttackPatternsData data = LoadOrCreate<AttackPatternsData>(ATTACK_PATTERNS_PATH);

            var config = (Dictionary<string, object>)root["config"];
            data.Config = new ConfigValues();
            foreach (var pair in config)
            {
                SetConfigField(data.Config, pair.Key, (double)pair.Value);
            }

            data.Sliders = ((List<object>)root["sliders"]).Cast<Dictionary<string, object>>().Select(slider => new SliderDef
            {
                Key = (string)slider["key"],
                Label = (string)slider["label"],
                Min = GetFloat(slider, "min", 0f),
                Max = GetFloat(slider, "max", 0f),
                Step = GetFloat(slider, "step", 0f),
            }).ToList();

            data.Phases = ((List<object>)root["phases"]).Cast<Dictionary<string, object>>().Select(phase => new PhaseDef
            {
                Words = ((List<object>)phase["words"]).Cast<string>().ToList(),
                Attacks = ((List<object>)phase["attacks"]).Cast<string>().ToList(),
                HandHp = (int)GetFloat(phase, "handHp", 20f),
                SpeedMul = GetFloat(phase, "speedMul", 1f),
                IntervalMul = GetFloat(phase, "intervalMul", 1f),
                WarnMul = GetFloat(phase, "warnMul", 1f),
            }).ToList();

            data.FinisherWord = (string)root["finisherWord"];

            // 웹 코드의 기본값(spell.count || 1, spell.size || 10)을 그대로 채운다
            data.Spells = ((Dictionary<string, object>)root["spells"]).Select(pair =>
            {
                var spell = (Dictionary<string, object>)pair.Value;
                ColorUtility.TryParseHtmlString((string)spell["color"], out Color color);
                return new SpellDef
                {
                    Word = pair.Key,
                    Ko = (string)spell["ko"],
                    Fx = (string)spell["fx"],
                    Color = color,
                    Size = GetFloat(spell, "size", 10f),
                    Count = (int)GetFloat(spell, "count", 1f),
                    Icon = (string)spell["icon"],
                };
            }).ToList();

            data.Attacks = ((Dictionary<string, object>)root["attacks"]).Select(pair =>
            {
                var attack = (Dictionary<string, object>)pair.Value;
                return new AttackDef
                {
                    Id = pair.Key,
                    Name = (string)attack["name"],
                    Kind = (string)attack["kind"],
                    Base = GetFloat(attack, "base", 0f),
                    Cooldown = GetFloat(attack, "cooldown", 0f),
                    WarnMul = GetFloat(attack, "warnMul", 1f),
                    ShiftMin = (int)GetFloat(attack, "shiftMin", 1f),
                    ShiftMax = (int)GetFloat(attack, "shiftMax", 1f),
                    BulletSpeed = GetFloat(attack, "bulletSpeed", 0f),
                    ChargeSpeed = GetFloat(attack, "chargeSpeed", 0f),
                    ChainMin = GetPhaseValues(attack, "chainMin").Select(v => (int)v).ToArray(),
                    ChainMax = GetPhaseValues(attack, "chainMax").Select(v => (int)v).ToArray(),
                    ChainWarnMul = GetFloat(attack, "chainWarnMul", 1f),
                    TrackTime = GetFloat(attack, "trackTime", 0f),
                    FollowSpeed = GetFloat(attack, "followSpeed", 0f),
                    LockMul = GetFloat(attack, "lockMul", 1f),
                    DoubleChance = GetPhaseValues(attack, "doubleChance").ToArray(),
                    DoubleGap = GetFloat(attack, "doubleGap", 0f),
                };
            }).ToList();

            EditorUtility.SetDirty(data);
        }

        private static void ImportBoardLayouts()
        {
            var layouts = (Dictionary<string, object>)MiniJsonUtils.Parse(File.ReadAllText(Path.Combine(SOURCE_DIR, "layouts.json")));
            string rules = File.ReadAllText(Path.Combine(SOURCE_DIR, "layoutRules.js"));
            BoardLayoutData data = LoadOrCreate<BoardLayoutData>(BOARD_LAYOUTS_PATH);

            // layoutRules.js: 오른쪽 마스크·슬롯은 왼쪽을 좌우로 뒤집은 것
            Match maskMatch = Regex.Match(rules, @"MASK_LEFT\s*=\s*\[([^\]]*)\]");
            data.LeftMask = Regex.Matches(maskMatch.Groups[1].Value, "'([^']*)'").Cast<Match>().Select(m => m.Groups[1].Value).ToArray();
            data.RightMask = data.LeftMask.Select(row => new string(row.Reverse().ToArray())).ToArray();
            int columnCount = data.LeftMask[0].Length;

            Match slotMatch = Regex.Match(rules, @"SLOTS_LEFT\s*=\s*\[([^\]]*)\]");
            data.LeftSlots = Regex.Matches(slotMatch.Groups[1].Value, @"r:\s*(-?\d+),\s*c:\s*(-?\d+)").Cast<Match>()
                .Select(m => new Vector2Int(int.Parse(m.Groups[2].Value), int.Parse(m.Groups[1].Value))).ToArray();
            data.RightSlots = data.LeftSlots.Select(slot => new Vector2Int(columnCount - 1 - slot.x, slot.y)).ToArray();

            data.LeftLayout = ((List<object>)layouts["left"]).Cast<string>().ToArray();
            data.RightLayout = ((List<object>)layouts["right"]).Cast<string>().ToArray();
            EditorUtility.SetDirty(data);
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static float GetFloat(Dictionary<string, object> source, string key, float fallback)
        {
            return source.TryGetValue(key, out object value) && value is double number ? (float)number : fallback;
        }

        // {"1": 0.2, "2": 0.4, "3": 0.55} → [0.2, 0.4, 0.55] (0번 = 1페이즈)
        private static List<float> GetPhaseValues(Dictionary<string, object> source, string key)
        {
            var values = new List<float>();
            if (!source.TryGetValue(key, out object raw) || raw is not Dictionary<string, object> byPhase)
            {
                return values;
            }

            for (int phase = 1; byPhase.TryGetValue(phase.ToString(), out object value); phase++)
            {
                values.Add((float)(double)value);
            }

            return values;
        }

        // JSON 키(camelCase)를 같은 이름의 PascalCase 필드에 넣는다
        private static void SetConfigField(ConfigValues config, string key, double value)
        {
            string fieldName = char.ToUpperInvariant(key[0]) + key.Substring(1);
            System.Reflection.FieldInfo field = typeof(ConfigValues).GetField(fieldName);
            if (field == null)
            {
                Debug.LogWarning($"[WARNING] DataImportUtils::SetConfigField() - Unknown config key. Details: [Key={key}]");
                return;
            }

            field.SetValue(config, field.FieldType == typeof(int) ? (object)(int)value : (float)value);
        }
    }
}
