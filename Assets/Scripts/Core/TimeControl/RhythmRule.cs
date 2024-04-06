using AD.BASE;

namespace RhythmGame.Time
{
    public class RhythmRule : ADModel
    {
        public const int Tempo = 60;
        public const int HitPerBar = 4;
        public const int Legalet = 1;
        public const int SongMinestBeat = 32;
        public float Speed = 1;
        public float TimeScale = 1;

        public override void Init()
        {

        }
    }
}
