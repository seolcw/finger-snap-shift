namespace ShiftMagicSpell.View
{
    /// <summary>
    /// 그리기 순서(SpriteRenderer.sortingOrder) 기본값. 각 컴포넌트의 인스펙터 값이 이 값을 기본으로 쓴다.
    /// 즉시 그리기 캔버스(PixelCanvas)는 시작 값부터 그린 순서대로 1씩 올라가므로 띠 사이를 넉넉히 둔다.
    /// </summary>
    public static class RenderOrderUtils
    {
        public const int BOARD_BASE = 500;           // 빈 홈, SHIFT 칸, 슬롯 받침
        public const int BOARD_UNDER_FX = 700;       // 줄 돌리기 잔상, 단어 자리 점선
        public const int KEY = 1000;                 // 키(+1 테, +2 힌트, +3 맞음, +4 글자). 움직이는 키는 +10
        public const int BLOCK = 2000;               // 슬롯의 공격 블록
        public const int BOARD_OVER_FX = 2500;       // 줄 돌리기 화살표, 맞춤 번쩍임, 순간이동 고리, 블록 날아감
        public const int WARNING = 3000;             // 손 공격 예고, 소환 탄
        public const int PLAYER = 4000;
        public const int HAND = 5000;
        public const int FX = 6000;                  // 마법, 떠오르는 글자, 클릭 고리
        public const int HUD = 10000;
        public const int SCREEN_PANEL = 20000;       // 시작·일시정지·쓰러짐·클리어·승리 화면
    }
}
