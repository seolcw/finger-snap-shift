using System.Collections.Generic;

namespace ShiftMagicSpell.Core
{
    /// <summary>한 판(처음부터 승리까지) 동안 모으는 통계. 봇 테스트와 결과 확인에 쓴다.</summary>
    public class GameStats
    {
        public int Teleports;
        public int Moves;
        public int WordsDone;
        public int Deaths;
        public int Rotations;
        public int Casts;
        public int Combos;
        public int Stored;
        public int BlocksLost;
        public int DoubleClicks;
        public int AimDashes;
        public float PlayTime;
        public readonly Dictionary<string, int> Hits = new Dictionary<string, int>();
        public readonly Dictionary<string, int> Attacks = new Dictionary<string, int>();

        public static void Increment(Dictionary<string, int> counter, string key)
        {
            counter.TryGetValue(key, out int count);
            counter[key] = count + 1;
        }
    }
}
