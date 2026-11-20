using System.Collections.Generic;
using UnityEngine;

namespace ShiftMagicSpell.Audio
{
    /// <summary>효과음 목록과 배경음. 인스펙터에서 이벤트별 클립·볼륨·피치를 바꾼다.</summary>
    [CreateAssetMenu(fileName = "SoundLibrary", menuName = "Shift Magic Spell/Sound Library")]
    public class SoundLibrary : ScriptableObject
    {
        [SerializeField] private AudioClip _music;
        [Range(0f, 1f)] [SerializeField] private float _musicVolume = 0.35f;
        [Range(0f, 1f)] [SerializeField] private float _sfxVolume = 1f;
        [SerializeField] private List<SoundEntry> _entries = new List<SoundEntry>();

        private Dictionary<SoundKey, SoundEntry> _entryDict;

        public AudioClip Music
        {
            get => _music;
            set => _music = value;
        }

        public float MusicVolume
        {
            get => _musicVolume;
            set => _musicVolume = value;
        }

        public float SfxVolume
        {
            get => _sfxVolume;
            set => _sfxVolume = value;
        }

        public List<SoundEntry> Entries => _entries;

        public bool TryGetEntry(SoundKey key, out SoundEntry entry)
        {
            if (_entryDict == null)
            {
                RebuildDict();
            }

            return _entryDict.TryGetValue(key, out entry) && entry.Clip != null;
        }

        private void OnValidate()
        {
            _entryDict = null;
        }

        private void RebuildDict()
        {
            _entryDict = new Dictionary<SoundKey, SoundEntry>();
            foreach (SoundEntry entry in _entries)
            {
                // 같은 키가 두 번 있으면 앞의 것을 쓴다
                _entryDict.TryAdd(entry.Key, entry);
            }
        }
    }
}
