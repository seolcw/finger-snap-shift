using System.Collections.Generic;
using UnityEngine;

namespace ShiftMagicSpell.View
{
    /// <summary>
    /// 매 프레임 개수가 바뀌는 연출용 프리팹 묶음. BeginFrame → 필요한 만큼 Next → EndFrame(안 쓴 것은 끔).
    /// 한 번 만든 인스턴스는 지우지 않고 다시 쓴다.
    /// </summary>
    public class PrefabPool<T> where T : Component
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly List<T> _items = new List<T>();
        private int _usedCount;

        public PrefabPool(T prefab, Transform parent)
        {
            _prefab = prefab;
            _parent = parent;
        }

        public void BeginFrame()
        {
            _usedCount = 0;
        }

        public T Next()
        {
            if (_usedCount == _items.Count)
            {
                _items.Add(Object.Instantiate(_prefab, _parent));
            }

            T item = _items[_usedCount++];
            if (!item.gameObject.activeSelf)
            {
                item.gameObject.SetActive(true);
            }

            return item;
        }

        public void EndFrame()
        {
            for (int i = _usedCount; i < _items.Count; i++)
            {
                if (_items[i].gameObject.activeSelf)
                {
                    _items[i].gameObject.SetActive(false);
                }
            }
        }
    }
}
