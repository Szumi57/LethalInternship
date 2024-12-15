using LethalInternship.Enums;
using LethalInternship.NetworkSerializers;

namespace LethalInternship.Constants
{
    internal class ConfigConst
    {
        // Config
        public static readonly string ConfigSectionMain = "1. Internship program";
        public static readonly string ConfigSectionIdentities = "2. Intern identities";
        public static readonly string ConfigSectionBehaviour = "3. Behaviour";
        public static readonly string ConfigSectionTeleporters = "4. Teleporters";
        public static readonly string ConfigSectionVoices = "5. Voices";
        public static readonly string ConfigSectionDebug = "6. Debug";

        public static readonly int DEFAULT_MAX_INTERNS_AVAILABLE = 16;
        public static readonly int MIN_INTERNS_AVAILABLE = 1;
        public static readonly int MAX_INTERNS_AVAILABLE = 32;

        public static readonly int DEFAULT_PRICE_INTERN = 19;
        public static readonly int MIN_PRICE_INTERN = 0;
        public static readonly int MAX_PRICE_INTERN = 200;

        public static readonly int DEFAULT_INTERN_MAX_HEALTH = 51;
        public static readonly int MIN_INTERN_MAX_HEALTH = 1;
        public static readonly int MAX_INTERN_MAX_HEALTH = 200;

        public static readonly float DEFAULT_SIZE_SCALE_INTERN = 0.85f;
        public static readonly float MIN_SIZE_SCALE_INTERN = 0.3f;
        public static readonly float MAX_SIZE_SCALE_INTERN = 1f;

        public static readonly string DEFAULT_STRING_INTERNSHIP_PROGRAM_TITLE = "INTERNSHIP PROGRAM";
        public static readonly string DEFAULT_STRING_INTERNSHIP_PROGRAM_SUBTITLE = "Need some help ? Try our new workforce, ready to assist you and gain experience";

        public static readonly string DEFAULT_INTERN_NAME = "Intern #{0}";
        //  "Amy Stake",
        //  "Claire Annette",
        //  "Clare Voyant",
        //  "Ella Font",
        //  "Felix Cited",
        //  "Gerry Atrick",
        //  "Harry Legg",
        //  "Justin Case",
        //  "Lee King",
        //  "Luke Atmey",
        //  "Manuel Labour",
        //  "Mia Moore",
        //  "Ophelia Pane",
        //  "Paige Turner",
        //  "Paul Atishon",
        //  "Polly Esther",
        //  "Robyn Banks",
        //  "Terry Aki",
        //  "Tim Burr",
        //  "Toby Lerone",
        //  "Uriel Lucky",
        //  "Zoltan Pepper"

        public static EnumOptionSuitChange DEFAULT_CONFIG_ENUM_INTERN_SUIT_CHANGE = EnumOptionSuitChange.Manual;

        public static readonly int DEFAULT_MAX_IDENTITIES = 22;
        public static readonly int MIN_IDENTITIES = 10;
        public static readonly int MAX_IDENTITIES = 200;
        public static readonly string FILE_NAME_CONFIG_IDENTITIES_DEFAULT = "ConfigIdentitiesDefault.json";
        public static readonly string FILE_NAME_CONFIG_IDENTITIES_USER = "ConfigIdentitiesUser.json";
        public static readonly ConfigIdentity DEFAULT_CONFIG_IDENTITY = new ConfigIdentity()
        {
            name = DEFAULT_INTERN_NAME,
            suitConfigOption = (int)EnumOptionSuitConfig.AutomaticSameAsPlayer,
            suitID = 0,
            voiceFolder = "RandomMale1"
        };
    }
}
