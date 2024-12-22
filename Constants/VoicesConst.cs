using LethalInternship.Enums;

namespace LethalInternship.Constants
{
    internal class VoicesConst
    {
        public static readonly float DEFAULT_VOLUME = 1f;
        public static EnumTalkativeness DEFAULT_CONFIG_ENUM_TALKATIVENESS = EnumTalkativeness.Normal;

        public static readonly float DISTANCE_HEAR_OTHER_INTERNS = 10f;
        public static readonly float FADE_IN_TIME = 0.1f;
        public static readonly float FADE_OUT_TIME = 0.2f;
        public static readonly string SWEAR_KEYWORD = "_cuss";
        public static readonly string INSIDE_KEYWORD = "_inside";
        public static readonly string OUTSIDE_KEYWORD = "_outside";

        public static readonly int MIN_COOLDOWN_PLAYVOICE_SHY = 10;
        public static readonly int MAX_COOLDOWN_PLAYVOICE_SHY = 40;

        public static readonly int MIN_COOLDOWN_PLAYVOICE_NORMAL = 5;
        public static readonly int MAX_COOLDOWN_PLAYVOICE_NORMAL = 20;

        public static readonly int MIN_COOLDOWN_PLAYVOICE_TALKATIVE = 2;
        public static readonly int MAX_COOLDOWN_PLAYVOICE_TALKATIVE = 10;

        public static readonly int MIN_COOLDOWN_PLAYVOICE_CANTSTOPTALKING = 0;
        public static readonly int MAX_COOLDOWN_PLAYVOICE_CANTSTOPTALKING = 0;
    }
}
