using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ShiftMagicSpell.Data;
using UnityEditor;
using UnityEngine;

namespace ShiftMagicSpell.Editor
{
    /// <summary>
    /// Assets/Art의 .aseprite를 모두 읽어 ArtCatalog 에셋을 만든다.
    /// 태그와 프레임 시간은 .aseprite 파일(헤더·프레임·태그 청크)에서 직접 읽어서, 그림을 고쳐도 다시 구우면 맞는다.
    /// </summary>
    public static class ArtCatalogBuildUtils
    {
        public const string CATALOG_PATH = "Assets/Data/ArtCatalog.asset";
        private const string ART_ROOT = "Assets/Art";
        private const ushort FILE_MAGIC = 0xA5E0;
        private const ushort FRAME_MAGIC = 0xF1FA;
        private const ushort TAGS_CHUNK = 0x2018;

        [MenuItem("Tools/Shift Magic Spell/Build Art Catalog")]
        public static void Build()
        {
            ArtCatalog catalog = AssetDatabase.LoadAssetAtPath<ArtCatalog>(CATALOG_PATH);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<ArtCatalog>();
                AssetDatabase.CreateAsset(catalog, CATALOG_PATH);
            }

            catalog.Sheets = new List<ArtSheet>();
            foreach (string guid in AssetDatabase.FindAssets(string.Empty, new[] { ART_ROOT }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".aseprite"))
                {
                    catalog.Sheets.Add(BuildSheet(path));
                }
            }

            catalog.Sheets.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"[INFO] ArtCatalogBuildUtils::Build() - Art catalog built. Details: [SheetCount={catalog.Sheets.Count}]");
        }

        private static ArtSheet BuildSheet(string path)
        {
            AsepriteInfo info = ReadInfo(path);
            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
            var frames = new Sprite[info.Durations.Count];
            foreach (Sprite sprite in sprites)
            {
                int underscore = sprite.name.LastIndexOf('_');
                if (underscore >= 0 && int.TryParse(sprite.name.Substring(underscore + 1), out int index) && index < frames.Length)
                {
                    frames[index] = sprite;
                }
            }

            // 프레임이 하나뿐인 파일은 스프라이트 이름이 파일 이름으로 들어온다
            if (frames.Length == 1 && frames[0] == null && sprites.Length > 0)
            {
                frames[0] = sprites[0];
            }

            for (int i = 0; i < frames.Length; i++)
            {
                if (frames[i] == null)
                {
                    Debug.LogWarning($"[WARNING] ArtCatalogBuildUtils::BuildSheet() - Missing frame sprite. Details: [Path={path}, Frame={i}]");
                }
            }

            return new ArtSheet
            {
                Name = Path.GetFileNameWithoutExtension(path),
                CanvasSize = info.CanvasSize,
                Frames = frames,
                Durations = info.Durations.ToArray(),
                Tags = info.Tags,
            };
        }

        private sealed class AsepriteInfo
        {
            public Vector2Int CanvasSize;
            public readonly List<float> Durations = new List<float>();
            public readonly List<ArtTag> Tags = new List<ArtTag>();
        }

        // 형식: https://github.com/aseprite/aseprite/blob/main/docs/ase-file-specs.md
        private static AsepriteInfo ReadInfo(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            var info = new AsepriteInfo();
            if (BitConverter.ToUInt16(bytes, 4) != FILE_MAGIC)
            {
                throw new InvalidDataException($"Not an aseprite file: {path}");
            }

            int frameCount = BitConverter.ToUInt16(bytes, 6);
            info.CanvasSize = new Vector2Int(BitConverter.ToUInt16(bytes, 8), BitConverter.ToUInt16(bytes, 10));
            int position = 128;
            for (int frame = 0; frame < frameCount; frame++)
            {
                int frameStart = position;
                int frameBytes = BitConverter.ToInt32(bytes, frameStart);
                if (BitConverter.ToUInt16(bytes, frameStart + 4) != FRAME_MAGIC)
                {
                    throw new InvalidDataException($"Bad frame header: {path} frame {frame}");
                }

                int oldChunkCount = BitConverter.ToUInt16(bytes, frameStart + 6);
                info.Durations.Add(BitConverter.ToUInt16(bytes, frameStart + 8) / 1000f);
                int newChunkCount = BitConverter.ToInt32(bytes, frameStart + 12);
                int chunkCount = newChunkCount != 0 ? newChunkCount : oldChunkCount;
                int chunkPosition = frameStart + 16;
                for (int chunk = 0; chunk < chunkCount; chunk++)
                {
                    int chunkSize = BitConverter.ToInt32(bytes, chunkPosition);
                    if (BitConverter.ToUInt16(bytes, chunkPosition + 4) == TAGS_CHUNK)
                    {
                        ReadTags(bytes, chunkPosition + 6, info.Tags);
                    }

                    chunkPosition += chunkSize;
                }

                position = frameStart + frameBytes;
            }

            return info;
        }

        private static void ReadTags(byte[] bytes, int position, List<ArtTag> tags)
        {
            int tagCount = BitConverter.ToUInt16(bytes, position);
            position += 10;
            for (int i = 0; i < tagCount; i++)
            {
                int from = BitConverter.ToUInt16(bytes, position);
                int to = BitConverter.ToUInt16(bytes, position + 2);
                position += 17;
                int nameLength = BitConverter.ToUInt16(bytes, position);
                string name = Encoding.UTF8.GetString(bytes, position + 2, nameLength);
                position += 2 + nameLength;
                tags.Add(new ArtTag { Name = name, From = from, To = to });
            }
        }
    }
}
