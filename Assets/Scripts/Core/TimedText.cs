namespace ShiftMagicSpell.Core
{
    /// <summary>잠깐 떠 있는 안내 문구(토스트, 페이즈 배너).</summary>
    public class TimedText
    {
        public string Text;
        public float T;

        public TimedText(string text, float time)
        {
            Text = text;
            T = time;
        }
    }
}
