namespace ShiftMagicSpell.Core
{
    /// <summary>시드를 정할 수 있는 난수(테스트 재현용).</summary>
    public class GameRandom
    {
        private readonly System.Random _random;

        public GameRandom(int seed)
        {
            _random = new System.Random(seed);
        }

        public float Value()
        {
            return (float)_random.NextDouble();
        }

        public float Range(float min, float max)
        {
            return min + Value() * (max - min);
        }

        /// <summary>min 이상 max 이하 정수</summary>
        public int RangeInclusive(int min, int max)
        {
            return _random.Next(min, max + 1);
        }
    }
}
