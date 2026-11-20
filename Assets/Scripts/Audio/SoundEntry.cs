using System;
using UnityEngine;

namespace ShiftMagicSpell.Audio
{
    /// <summary>이벤트 하나의 소리 설정. 같은 클립을 피치·볼륨만 바꿔 여러 이벤트에 쓸 수 있다.</summary>
    [Serializable]
    public class SoundEntry
    {
        public SoundKey Key;
        public AudioClip Clip;
        [Range(0f, 1f)] public float Volume = 0.6f;
        [Range(0.25f, 3f)] public float Pitch = 1f;
        [Tooltip("재생할 때마다 피치를 ±이만큼 흔든다(같은 소리가 반복될 때 덜 거슬리게)")]
        [Range(0f, 0.5f)] public float PitchJitter;
        [Tooltip("같은 이벤트를 이 시간(초) 안에 다시 내지 않는다")]
        [Min(0f)] public float MinInterval;
    }
}
