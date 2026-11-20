namespace ShiftMagicSpell.Core
{
    /// <summary>키 하나. Letter가 '\0'이면 빈 키.</summary>
    public class KeyTile
    {
        public readonly int Id;
        public readonly char Letter;
        public SlideAnim Slide;

        public KeyTile(int id, char letter)
        {
            Id = id;
            Letter = letter;
        }

        public bool HasLetter => Letter != '\0';
    }
}
