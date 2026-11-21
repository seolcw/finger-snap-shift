using System;
using UnityEngine;

namespace ShiftMagicSpell.View
{
    [Serializable]
    public class ViewColors
    {
        public Color Text = new Color32(0xe6, 0xe8, 0xee, 0xff);
        public Color Dim = new Color32(0x8a, 0x90, 0xa0, 0xff);
        public Color Warn = new Color32(0xff, 0x4d, 0x4d, 0xff);
        public Color Glow = new Color32(0xff, 0xd2, 0x4a, 0xff);
        public Color Shift = new Color32(0x3d, 0x7b, 0xff, 0xff);
        public Color ShiftHi = new Color32(0x7f, 0xa8, 0xff, 0xff);
        public Color Ok = new Color32(0x5f, 0xe0, 0x8a, 0xff);
        public Color Hp = new Color32(0xff, 0x5d, 0x73, 0xff);
        [Tooltip("키가 미끄러질 때 보이는 빈 홈")]
        public Color KeyHome = new Color32(0x0f, 0x11, 0x16, 0xff);
        [Tooltip("SHIFT 칸 쿨다운 덮개")]
        public Color CooldownShade = new Color32(0x0a, 0x0c, 0x12, 0x99);
        [Tooltip("시작·일시정지·쓰러짐 화면 덮개")]
        public Color Overlay = new Color32(0x0c, 0x0d, 0x11, 0xe0);
        public Color HandCharge = new Color32(0xff, 0x8a, 0x8a, 0xff);
        public Color HandStun = new Color32(0x8f, 0x98, 0xb0, 0xff);
        public Color Smoke = new Color32(0x9a, 0xa0, 0xad, 0xff);
    }
}
