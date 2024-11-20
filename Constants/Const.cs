using LethalInternship.Enums;
using LethalInternship.NetworkSerializers;
using UnityEngine;

namespace LethalInternship.Constants
{
    /// <summary>
    /// Class of constants, used in various places in the plugin code
    /// </summary>
    internal class Const
    {
        public const string CSYNC_GUID = "com.sigurd.csync";

        public const string MORECOMPANY_GUID = "me.swipez.melonloader.morecompany";
        public const string BETTER_EXP_GUID = "Swaggies.BetterEXP";
        public const string MOREEMOTES_GUID = "MoreEmotes";
        public const string BETTEREMOTES_GUID = "BetterEmotes";
        public const string MODELREPLACEMENT_GUID = "meow.ModelReplacementAPI";
        public const string LETHALPHONES_GUID = "LethalPhones";
        public const string FASTERITEMDROPSHIP_GUID = "FlipMods.FasterItemDropship";
        public const string SHOWCAPACITY_GUID = "Piggy.ShowCapacity";
        public const string REVIVECOMPANY_GUID = "OpJosMod.ReviveCompany";
        public const string TOOMANYEMOTES_GUID = "FlipMods.TooManyEmotes";
        public const string RESERVEDITEMSLOTCORE_GUID = "FlipMods.ReservedItemSlotCore";
        public const string LETHALPROGRESSION_GUID = "Stoneman.LethalProgression";
        public const string QUICKBUYMENU_GUID = "QuickBuyMenu";
        public const string CUSTOMITEMBEHAVIOURLIBRARY_GUID = "com.github.WhiteSpike.CustomItemBehaviourLibrary";

        public const string ADDITIONALNETWORKING_DLLFILENAME = "AdditionalNetworking_Preloader.dll";

        public static readonly float EPSILON = 0.01f;
        public static readonly bool DISABLE_ORIGINAL_GAME_DEBUG_LOGS = true;
        public static readonly ulong INTERN_ACTUAL_ID_OFFSET = 100000ul;

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

        public static EnumOptionNames DEFAULT_CONFIG_ENUM_INTERN_NAMES = EnumOptionNames.DefaultCustomList;
        public static readonly string DEFAULT_INTERN_NAME = "Intern #{0}";
        public static readonly string[] DEFAULT_LIST_CUSTOM_INTERN_NAMES = new string[] {"Amy Stake",
                                                                                         "Claire Annette",
                                                                                         "Clare Voyant",
                                                                                         "Ella Font",
                                                                                         "Felix Cited",
                                                                                         "Gerry Atrick",
                                                                                         "Harry Legg",
                                                                                         "Justin Case",
                                                                                         "Lee King",
                                                                                         "Luke Atmey",
                                                                                         "Manuel Labour",
                                                                                         "Mia Moore",
                                                                                         "Ophelia Pane",
                                                                                         "Paige Turner",
                                                                                         "Paul Atishon",
                                                                                         "Polly Esther",
                                                                                         "Robyn Banks",
                                                                                         "Terry Aki",
                                                                                         "Tim Burr",
                                                                                         "Toby Lerone",
                                                                                         "Uriel Lucky",
                                                                                         "Zoltan Pepper"};

        public static EnumOptionSuitChange DEFAULT_CONFIG_ENUM_INTERN_SUIT_CHANGE = EnumOptionSuitChange.Manual;

        public static readonly string FILE_NAME_CONFIG_IDENTITIES_DEFAULT = "ConfigIdentitiesDefault.json";
        public static readonly string FILE_NAME_CONFIG_IDENTITIES_USER = "ConfigIdentitiesUser.json";
        public static readonly ConfigIdentity DEFAULT_CONFIG_IDENTITY = new ConfigIdentity()
        {
            name = DEFAULT_INTERN_NAME,
            suitConfigOption = (int)EnumOptionSuitConfig.AutomaticSameAsPlayer,
            suitID = 0,
            voiceFolder = "RandomMale1"
        };

        // Interns
        public static readonly float INTERN_FOV = 90f;
        public static readonly int INTERN_ENTITIES_RANGE = 40;
        public static readonly float INTERN_OBJECT_AWARNESS = 3f;
        public static readonly float INTERN_OBJECT_RANGE = 15f;
        public static readonly float WAIT_TIME_FOR_GRAB_DROPPED_OBJECTS = 10f;
        public static readonly float COLLISION_RANGE = 2.8f;
        public static readonly float CLIMB_SPEED = 8f;
        public static readonly int INIT_RAGDOLL_ID = -2;

        public static readonly float AGENT_SPEED = 3.5f;
        public static readonly float AGENT_SPEED_CROUCH = 1.75f;
        public static readonly float BASE_MAX_SPEED = 0.9f;
        public static readonly float BASE_MIN_SPEED = 0.01f;
        public static readonly float BODY_TURNSPEED = 6f;
        public static readonly float CAMERA_TURNSPEED = 8f;

        public static readonly float DISTANCE_CLOSE_ENOUGH_TO_DESTINATION = 1f;
        public static readonly float DISTANCE_CHECK_FOR_HOLES = 2.5f;
        public static readonly float DISTANCE_ITEMS_TO_ENTRANCE = 6f;

        public static readonly int COMPANY_BUILDING_MOON_ID = 3;
        public static readonly float SHIP_EXPANDING_BOUNDS_DIFFERENCE = 10f;

        // NpcController
        public static readonly int PLAYER_MASK = 8;
        public static readonly string PLAYER_ANIMATION_WEIGHT_HOLDINGITEMSRIGHTHAND = "HoldingItemsRightHand";
        public static readonly string PLAYER_ANIMATION_WEIGHT_HOLDINGITEMSBOTHHANDS = "HoldingItemsBothHands";
        public static readonly string PLAYER_ANIMATION_WEIGHT_SPECIALANIMATIONS = "SpecialAnimations";
        public static readonly string PLAYER_ANIMATION_WEIGHT_EMOTESNOARMS = "EmotesNoArms";

        public static readonly string PLAYER_ANIMATION_BOOL_GRAB = "Grab";
        public static readonly string PLAYER_ANIMATION_BOOL_GRABVALIDATED = "GrabValidated";
        public static readonly string PLAYER_ANIMATION_BOOL_GRABINVALIDATED = "GrabInvalidated";
        public static readonly string PLAYER_ANIMATION_BOOL_CANCELHOLDING = "cancelHolding";
        public static readonly string PLAYER_ANIMATION_BOOL_WALKING = "Walking";
        public static readonly string PLAYER_ANIMATION_BOOL_SPRINTING = "Sprinting";
        public static readonly string PLAYER_ANIMATION_BOOL_SIDEWAYS = "Sideways";
        public static readonly string PLAYER_ANIMATION_BOOL_ANIMATIONSPEED = "animationSpeed";
        public static readonly string PLAYER_ANIMATION_BOOL_HINDEREDMOVEMENT = "hinderedMovement";
        public static readonly string PLAYER_ANIMATION_BOOL_CROUCHING = "crouching";
        public static readonly string PLAYER_ANIMATION_BOOL_FALLNOJUMP = "FallNoJump";
        public static readonly string PLAYER_ANIMATION_BOOL_SHORTFALLLANDING = "ShortFallLanding";
        public static readonly string PLAYER_ANIMATION_BOOL_LIMP = "Limp";

        public static readonly string PLAYER_ANIMATION_TRIGGER_THROW = "Throw";
        public static readonly string PLAYER_ANIMATION_TRIGGER_DAMAGE = "Damage";
        public static readonly string PLAYER_ANIMATION_TRIGGER_SHORTFALLLANDING = "ShortFallLanding";

        public static readonly string PLAYER_ANIMATION_FLOAT_ANIMATIONSPEED = "animationSpeed";
        public static readonly string PLAYER_ANIMATION_FLOAT_TIREDAMOUNT = "tiredAmount";

        public static readonly string MAPDOT_ANIMATION_BOOL_DEAD = "dead";

        // SearchingForPlayerState 
        public static readonly float MIN_TIME_SPRINT_SEARCH_WANDER = 1f;
        public static readonly float MAX_TIME_SPRINT_SEARCH_WANDER = 3f;

        // GetCloseToPlayerState
        public static readonly float DISTANCE_START_RUNNING = 8f;
        public static readonly float DISTANCE_STOP_RUNNING = 7f;
        public static readonly float DISTANCE_CLOSE_ENOUGH_HOR = 5f;
        public static readonly float DISTANCE_CLOSE_ENOUGH_VER = 2f;
        public static readonly float DISTANCE_AWARENESS_HOR = 25f;
        public static readonly float DISTANCE_AWARENESS_VER = 50f;

        // JustLostPlayerState
        public static readonly float TIMER_LOOKING_AROUND = 6f;
        public static readonly float DISTANCE_STOP_SPRINT_LAST_KNOWN_POSITION = 2f;
        public static readonly float WAIT_TIME_TO_TELEPORT = 1f;
        public static readonly float DISTANCE_TO_ENTRANCE = 4f;
        public static readonly float MIN_TIME_FREEZE_LOOKING_AROUND = 0.5f;
        public static readonly float MAX_TIME_FREEZE_LOOKING_AROUND = 2f;

        // Stuck
        public static readonly float TIMER_STUCK_TOO_MUCH = 2f;

        // Player in ShipState
        public static readonly float DISTANCE_TO_SHIP_BOUND_CLOSEST_POINT = 1f;
        public static readonly float DISTANCE_OF_DROPPED_OBJECT_SHIP_BOUND_CLOSEST_POINT = 2f;

        // Player in cruiser vehicle
        public static readonly Vector3 POS1_ENTRY_INTERN_CRUISER = new Vector3(-0.5f, -1f, -5.5f);
        public static readonly Vector3 POS2_ENTRY_INTERN_CRUISER = new Vector3(0.9f, -1f, -5.5f);

        public static readonly Vector3 FIRST_CORNER_INTERN_IN_CRUISER = new Vector3(-0.5f, -0.5f, -0.4f);
        public static readonly Vector3 SECOND_CORNER_INTERN_IN_CRUISER = new Vector3(0.9f, -0.5f, -2.5f);

        // PanikState
        public static readonly float DISTANCE_FLEEING = 20f;
        public static readonly float DISTANCE_FLEEING_NO_LOS = 5f;

        public static readonly float DISTANCE_NPCBODY_FROM_LADDER = 2;
        public static readonly float DISTANCE_NPCBODY_FROM_DOOR = 2.5f;
        public static readonly float TIMER_CHECK_DOOR = 0.9f;

        // Tips
        public static readonly string TOOLTIP_DROP_ITEM = "Drop your item : [{0}]";
        public static readonly string TOOLTIP_TAKE_ITEM = "Take my item : [{0}]";
        public static readonly string TOOLTIP_FOLLOW_ME = "Follow me: [{0}]";
        public static readonly string TOOLTIP_GRAB_INTERNS = "Grab intern: [{0}]";
        public static readonly string TOOLTIP_RELEASE_INTERNS = "Release grabbed interns : [{0}]";
        public static readonly string TOOLTIP_CHANGE_SUIT_INTERNS = "Change suit : [{0}]";
        public static readonly string TOOLTIP_MAKE_INTERN_LOOK = "Make interns look : [{0}]";
        public static readonly string TOOLTIPS_ORDER_1 = "order 1 : [{0}]";
        
    }
}
