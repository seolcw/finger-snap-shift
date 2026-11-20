using System.Collections.Generic;
using ShiftMagicSpell.Data;
using UnityEngine;

namespace ShiftMagicSpell.View
{
    /// <summary>
    /// 320x200 픽셀 화면에 매 프레임 스프라이트를 "그리는" 즉시 모드 캔버스(웹 canvas 렌더 코드를 그대로 옮기기 위함).
    /// 좌표는 화면 픽셀(왼쪽 위 원점, y 아래). 그리는 순서가 곧 앞뒤 순서다.
    /// SpriteRenderer를 풀로 재사용하므로 프레임마다 GameObject를 만들지 않는다.
    /// </summary>
    public class PixelCanvas : MonoBehaviour
    {
        public const int SCREEN_W = 320;
        public const int SCREEN_H = 200;
        public const float PPU = 16f;
        private const int MAX_RADIUS = 220;

        [SerializeField] private Material _spriteMaterial;

        private readonly List<SpriteRenderer> _pool = new List<SpriteRenderer>();
        private readonly Dictionary<int, Sprite> _circleCache = new Dictionary<int, Sprite>();
        private readonly Dictionary<(Sprite, int), Sprite> _sliceCache = new Dictionary<(Sprite, int), Sprite>();
        private int _usedCount;
        private Sprite _pixelSprite;

        /// <summary>화면 흔들림 등으로 전체를 옮기는 양(px)</summary>
        public Vector2 Offset { get; set; }

        public int DrawCount => _usedCount;

        private void Awake()
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            _pixelSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.zero, PPU);
        }

        public void BeginFrame()
        {
            _usedCount = 0;
        }

        public void EndFrame()
        {
            for (int i = _usedCount; i < _pool.Count; i++)
            {
                if (_pool[i].enabled)
                {
                    _pool[i].enabled = false;
                }
            }
        }

        /// <summary>캔버스 왼쪽 위를 (x, y)에 맞춰 그린다. scale은 왼쪽 위 기준.</summary>
        public void DrawFrame(ArtFrame frame, float x, float y, Color tint, float scale = 1f)
        {
            if (!frame.IsValid)
            {
                return;
            }

            SpriteRenderer renderer = Next(frame.Sprite, tint);
            Transform tr = renderer.transform;
            tr.localPosition = ToWorld(x, y + frame.CanvasSize.y * scale);
            tr.localRotation = Quaternion.identity;
            tr.localScale = new Vector3(scale, scale, 1f);
        }

        /// <summary>캔버스 가운데를 (cx, cy)에 맞춰 그린다. 회전(도, 시계 반대)·배율·좌우 뒤집기는 가운데 기준.</summary>
        public void DrawFrameCentered(ArtFrame frame, float cx, float cy, Color tint, float rotationDeg = 0f, float scale = 1f, bool isFlipped = false)
        {
            if (!frame.IsValid)
            {
                return;
            }

            SpriteRenderer renderer = Next(frame.Sprite, tint);
            renderer.flipX = isFlipped;
            Vector2 halfCanvas = new Vector2(isFlipped ? -frame.CanvasSize.x : frame.CanvasSize.x, frame.CanvasSize.y) * (0.5f * scale / PPU);
            Quaternion rotation = Quaternion.Euler(0f, 0f, rotationDeg);
            Transform tr = renderer.transform;
            tr.localPosition = ToWorld(cx, cy) - rotation * halfCanvas;
            tr.localRotation = rotation;
            tr.localScale = new Vector3(scale, scale, 1f);
        }

        /// <summary>9-slice: 테두리 border px는 그대로 두고 가운데만 늘려서 (x, y, width, height)를 채운다.</summary>
        public void DrawNineSlice(ArtFrame frame, float x, float y, float width, float height, int border, Color tint)
        {
            if (!frame.IsValid)
            {
                return;
            }

            Rect source = frame.Sprite.textureRect;
            float[] sourceX = { 0f, border, source.width - border, source.width };
            float[] sourceY = { 0f, border, source.height - border, source.height };   // 위에서부터
            float[] targetX = { x, x + border, x + width - border, x + width };
            float[] targetY = { y, y + border, y + height - border, y + height };
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    int sliceKey = row * 3 + col;
                    if (!_sliceCache.TryGetValue((frame.Sprite, sliceKey), out Sprite slice))
                    {
                        var rect = new Rect(source.x + sourceX[col], source.y + source.height - sourceY[row + 1], sourceX[col + 1] - sourceX[col], sourceY[row + 1] - sourceY[row]);
                        slice = Sprite.Create(frame.Sprite.texture, rect, Vector2.zero, PPU, 0, SpriteMeshType.FullRect);
                        _sliceCache[(frame.Sprite, sliceKey)] = slice;
                    }

                    float targetWidth = targetX[col + 1] - targetX[col];
                    float targetHeight = targetY[row + 1] - targetY[row];
                    if (targetWidth <= 0f || targetHeight <= 0f)
                    {
                        continue;
                    }

                    SpriteRenderer renderer = Next(slice, tint);
                    Transform tr = renderer.transform;
                    tr.localPosition = ToWorld(targetX[col], targetY[row] + targetHeight);
                    tr.localRotation = Quaternion.identity;
                    tr.localScale = new Vector3(targetWidth / slice.rect.width, targetHeight / slice.rect.height, 1f);
                }
            }
        }

        public void DrawRect(float x, float y, float width, float height, Color color)
        {
            if (width <= 0f || height <= 0f || color.a <= 0f)
            {
                return;
            }

            SpriteRenderer renderer = Next(_pixelSprite, color);
            Transform tr = renderer.transform;
            tr.localPosition = ToWorld(x, y + height);
            tr.localRotation = Quaternion.identity;
            tr.localScale = new Vector3(width, height, 1f);
        }

        public void DrawRectOutline(float x, float y, float width, float height, float thickness, Color color)
        {
            DrawRect(x, y, width, thickness, color);
            DrawRect(x, y + height - thickness, width, thickness, color);
            DrawRect(x, y + thickness, thickness, height - thickness * 2f, color);
            DrawRect(x + width - thickness, y + thickness, thickness, height - thickness * 2f, color);
        }

        public void DrawLine(Vector2 from, Vector2 to, float thickness, Color color)
        {
            Vector2 delta = to - from;
            float length = delta.magnitude;
            if (length < 0.01f || color.a <= 0f)
            {
                return;
            }

            // 화면 y가 아래로 커서 월드 각도는 부호가 반대
            float angle = -Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
            SpriteRenderer renderer = Next(_pixelSprite, color);
            Transform tr = renderer.transform;
            tr.localPosition = ToWorld(from.x, from.y) - rotation * new Vector3(0f, thickness * 0.5f / PPU, 0f);
            tr.localRotation = rotation;
            tr.localScale = new Vector3(length, thickness, 1f);
        }

        /// <summary>점선(웹 setLineDash)</summary>
        public void DrawDashedRect(float x, float y, float width, float height, float dash, Color color)
        {
            for (float t = 0f; t < width; t += dash * 2f)
            {
                DrawRect(x + t, y, Mathf.Min(dash, width - t), 1f, color);
                DrawRect(x + t, y + height - 1f, Mathf.Min(dash, width - t), 1f, color);
            }

            for (float t = 0f; t < height; t += dash * 2f)
            {
                DrawRect(x, y + t, 1f, Mathf.Min(dash, height - t), color);
                DrawRect(x + width - 1f, y + t, 1f, Mathf.Min(dash, height - t), color);
            }
        }

        public void DrawDisc(Vector2 center, float radius, Color color)
        {
            DrawCircle(center, radius, 0, color);
        }

        public void DrawRing(Vector2 center, float radius, int thickness, Color color)
        {
            DrawCircle(center, radius, Mathf.Max(1, thickness), color);
        }

        /// <summary>꽉 찬 타원(그림자 등). 반지름 16px 원을 늘려서 그린다.</summary>
        public void DrawEllipse(Vector2 center, float radiusX, float radiusY, Color color)
        {
            if (color.a <= 0f || radiusX <= 0f || radiusY <= 0f)
            {
                return;
            }

            const int baseRadius = 16;
            SpriteRenderer renderer = Next(GetCircleSprite(baseRadius, 0), color);
            Transform tr = renderer.transform;
            tr.localPosition = ToWorld(center.x, center.y);
            tr.localRotation = Quaternion.identity;
            tr.localScale = new Vector3(radiusX / baseRadius, radiusY / baseRadius, 1f);
        }

        private void DrawCircle(Vector2 center, float radius, int thickness, Color color)
        {
            int r = Mathf.Clamp(Mathf.RoundToInt(radius), 1, MAX_RADIUS);
            if (color.a <= 0f)
            {
                return;
            }

            SpriteRenderer renderer = Next(GetCircleSprite(r, thickness), color);
            Transform tr = renderer.transform;
            tr.localPosition = ToWorld(center.x, center.y);
            tr.localRotation = Quaternion.identity;
            tr.localScale = Vector3.one;
        }

        // thickness 0 = 꽉 찬 원. 픽셀 원을 텍스처로 한 번 만들어 둔다.
        private Sprite GetCircleSprite(int radius, int thickness)
        {
            int cacheKey = radius * 64 + Mathf.Min(thickness, 63);
            if (_circleCache.TryGetValue(cacheKey, out Sprite cached))
            {
                return cached;
            }

            int size = radius * 2 + 1;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Mathf.Sqrt((x - radius) * (x - radius) + (y - radius) * (y - radius));
                    bool isInside = dist <= radius + 0.3f && (thickness == 0 || dist > radius - thickness + 0.3f);
                    pixels[y * size + x] = isInside ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), PPU);
            _circleCache[cacheKey] = sprite;
            return sprite;
        }

        private Vector3 ToWorld(float x, float y)
        {
            return new Vector3((x + Offset.x) / PPU, (SCREEN_H - y - Offset.y) / PPU, 0f);
        }

        private SpriteRenderer Next(Sprite sprite, Color color)
        {
            if (_usedCount == _pool.Count)
            {
                var child = new GameObject("Sprite");
                child.transform.SetParent(transform, false);
                SpriteRenderer created = child.AddComponent<SpriteRenderer>();
                if (_spriteMaterial != null)
                {
                    created.sharedMaterial = _spriteMaterial;
                }

                _pool.Add(created);
            }

            SpriteRenderer renderer = _pool[_usedCount];
            renderer.sortingOrder = _usedCount;
            _usedCount++;
            renderer.enabled = true;
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.flipX = false;
            return renderer;
        }
    }
}
