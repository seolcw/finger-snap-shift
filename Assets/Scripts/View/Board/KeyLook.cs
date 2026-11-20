namespace ShiftMagicSpell.View
{
    /// <summary>키 하나를 이번 프레임에 어떻게 보일지(줄 돌리기 테, 단어 힌트 점멸, 투명도)</summary>
    public struct KeyLook
    {
        public bool IsMoving;
        public bool IsOnBand;
        public bool IsOk;
        public bool IsInWord;
        public float Hint;
        public float Alpha;
    }
}
