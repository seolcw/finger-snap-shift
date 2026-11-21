using System.Collections.Generic;
using UnityEngine;

namespace ShiftMagicSpell.View
{
    /// <summary>
    /// 컴포넌트가 스스로 만드는 숨은 자식 SpriteRenderer 묶음(9-slice 조각, HP 칸 등).
    /// 씬에 저장하지 않고(HideAndDontSave), 컴포넌트가 꺼지면 지운다.
    /// </summary>
    public class HiddenRendererPool
    {
        private readonly List<SpriteRenderer> _renderers = new List<SpriteRenderer>();

        public SpriteRenderer Get(Transform parent, int index)
        {
            while (_renderers.Count <= index)
            {
                var child = new GameObject("Part") { hideFlags = HideFlags.HideAndDontSave };
                child.transform.SetParent(parent, false);
                _renderers.Add(child.AddComponent<SpriteRenderer>());
            }

            SpriteRenderer renderer = _renderers[index];
            renderer.enabled = true;
            return renderer;
        }

        /// <summary>used개 뒤의 조각은 숨긴다</summary>
        public void HideFrom(int used)
        {
            for (int i = used; i < _renderers.Count; i++)
            {
                if (_renderers[i] != null)
                {
                    _renderers[i].enabled = false;
                }
            }
        }

        public void Clear()
        {
            foreach (SpriteRenderer renderer in _renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Object.Destroy(renderer.gameObject);
                }
                else
                {
                    Object.DestroyImmediate(renderer.gameObject);
                }
            }

            _renderers.Clear();
        }
    }
}
