using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShiftMagicSpell.Data
{
    /// <summary>
    /// Assets/Art의 .aseprite 스프라이트를 "파일 이름 + 태그 + 프레임"으로 찾는 목록.
    /// 에디터 메뉴(Tools/Shift Magic Spell/Build Art Catalog)로 굽는다. 태그·프레임 시간은 .aseprite 파일에서 읽는다.
    /// </summary>
    [CreateAssetMenu(fileName = "ArtCatalog", menuName = "Shift Magic Spell/Art Catalog")]
    public class ArtCatalog : ScriptableObject
    {
        public List<ArtSheet> Sheets = new List<ArtSheet>();

        private Dictionary<string, ArtSheet> _sheetDict;

        public ArtSheet GetSheetOrNull(string sheetName)
        {
            if (_sheetDict == null)
            {
                _sheetDict = new Dictionary<string, ArtSheet>();
                foreach (ArtSheet sheet in Sheets)
                {
                    _sheetDict[sheet.Name] = sheet;
                }
            }

            return _sheetDict.TryGetValue(sheetName, out ArtSheet found) ? found : null;
        }

        /// <summary>태그 안의 index번째 프레임. 태그가 없으면 파일 전체 프레임에서 센다.</summary>
        public ArtFrame GetFrame(string sheetName, string tagName, int index)
        {
            ArtSheet sheet = GetSheetOrNull(sheetName);
            if (sheet == null)
            {
                return default;
            }

            ArtTag tag = sheet.GetTagOrNull(tagName);
            int from = tag != null ? tag.From : 0;
            int to = tag != null ? tag.To : sheet.Frames.Length - 1;
            int frame = Mathf.Clamp(from + index, from, to);
            return new ArtFrame(sheet.Frames[frame], sheet.CanvasSize);
        }

        /// <summary>태그를 Aseprite 프레임 시간대로 재생했을 때 time초 시점의 프레임.</summary>
        public ArtFrame GetAnimated(string sheetName, string tagName, float time, bool isLooping = true)
        {
            ArtSheet sheet = GetSheetOrNull(sheetName);
            if (sheet == null)
            {
                return default;
            }

            ArtTag tag = sheet.GetTagOrNull(tagName);
            int from = tag != null ? tag.From : 0;
            int to = tag != null ? tag.To : sheet.Frames.Length - 1;
            float total = 0f;
            for (int i = from; i <= to; i++)
            {
                total += sheet.Durations[i];
            }

            float local = isLooping && total > 0f ? Mathf.Repeat(time, total) : Mathf.Min(time, total);
            int frame = from;
            while (frame < to && local >= sheet.Durations[frame])
            {
                local -= sheet.Durations[frame];
                frame++;
            }

            return new ArtFrame(sheet.Frames[frame], sheet.CanvasSize);
        }

        public int GetFrameCount(string sheetName, string tagName)
        {
            ArtSheet sheet = GetSheetOrNull(sheetName);
            ArtTag tag = sheet?.GetTagOrNull(tagName);
            if (sheet == null)
            {
                return 0;
            }

            return tag != null ? tag.To - tag.From + 1 : sheet.Frames.Length;
        }

        private void OnValidate()
        {
            _sheetDict = null;
        }
    }

    [Serializable]
    public class ArtSheet
    {
        public string Name;
        public Vector2Int CanvasSize;
        public Sprite[] Frames = new Sprite[0];
        public float[] Durations = new float[0];
        public List<ArtTag> Tags = new List<ArtTag>();

        public ArtTag GetTagOrNull(string tagName)
        {
            if (string.IsNullOrEmpty(tagName))
            {
                return null;
            }

            foreach (ArtTag tag in Tags)
            {
                if (tag.Name == tagName)
                {
                    return tag;
                }
            }

            return null;
        }
    }

    [Serializable]
    public class ArtTag
    {
        public string Name;
        public int From;
        public int To;
    }

    /// <summary>스프라이트 하나와 그 캔버스 크기(px). 스프라이트 피벗은 캔버스 왼쪽 아래.</summary>
    public readonly struct ArtFrame
    {
        public readonly Sprite Sprite;
        public readonly Vector2Int CanvasSize;

        public ArtFrame(Sprite sprite, Vector2Int canvasSize)
        {
            Sprite = sprite;
            CanvasSize = canvasSize;
        }

        public bool IsValid => Sprite != null;
    }
}
