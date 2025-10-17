using UnityEngine;

namespace LethalInternship.SharedAbstractions.Constants
{
    /// <summary>
    /// Class of constants, used in various places in the plugin code
    /// </summary>
    public class Const
    {
        public const string CSYNC_GUID = "com.sigurd.csync";

        public const string MORECOMPANY_GUID = "me.swipez.melonloader.morecompany";
        public const string LETHALCOMPANYINPUTUTILS_GUID = "com.rune580.LethalCompanyInputUtils";
        public const string BETTER_EXP_GUID = "Swaggies.BetterEXP";
        public const string MOREEMOTES_GUID = "MoreEmotes";
        public const string BETTEREMOTES_GUID = "BetterEmotes";
        public const string MODELREPLACEMENT_GUID = "meow.ModelReplacementAPI";
        public const string LETHALPHONES_GUID = "LethalPhones";
        public const string FASTERITEMDROPSHIP_GUID = "FlipMods.FasterItemDropship";
        public const string SHOWCAPACITY_GUID = "Piggy.ShowCapacity";
        public const string REVIVECOMPANY_GUID = "OpJosMod.ReviveCompany";
        public const string BUNKBEDREVIVE_GUID = "viviko.BunkbedRevive";
        public const string ZAPRILLATOR_GUID = "Zaprillator";
        public const string TOOMANYEMOTES_GUID = "FlipMods.TooManyEmotes";
        public const string RESERVEDITEMSLOTCORE_GUID = "FlipMods.ReservedItemSlotCore";
        public const string LETHALPROGRESSION_GUID = "Stoneman.LethalProgression";
        public const string QUICKBUYMENU_GUID = "QuickBuyMenu";
        public const string CUSTOMITEMBEHAVIOURLIBRARY_GUID = "com.github.WhiteSpike.CustomItemBehaviourLibrary";
        public const string LCALWAYSHEARWALKIEMOD_GUID = "suskitech.LCAlwaysHearActiveWalkie";
        public const string BUTTERYFIXES_GUID = "butterystancakes.lethalcompany.butteryfixes";
        public const string PEEPERS_GUID = "x753.Peepers";
        public const string LETHALMIN_GUID = "NoteBoxz.LethalMin";
        public const string HOTDOGMODEL_GUID = "hotdogModel";
        public const string MIPA_GUID = "Mipa";
        public const string USUALSCRAP_GUID = "Emil.UsualScrap";

        public const string ADDITIONALNETWORKING_DLLFILENAME = "AdditionalNetworking_Preloader.dll";
        public const string MONOPROFILERLOADER_DLLFILENAME = "MonoProfilerLoader.dll";

        public static readonly float EPSILON = 0.01f;
        public static readonly bool DISABLE_ORIGINAL_GAME_DEBUG_LOGS = true;
        public static readonly ulong INTERN_ACTUAL_ID_OFFSET = 100000ul;

        // Interns
        public static readonly float INTERN_FOV = 90f;
        public static readonly int INTERN_ENTITIES_RANGE = 40;
        public static readonly float INTERN_OBJECT_AWARNESS = 5f;
        public static readonly float INTERN_OBJECT_RANGE = 15f;
        public static readonly float WAIT_TIME_FOR_GRAB_DROPPED_OBJECTS = 10f;
        public static readonly float CLIMB_SPEED = 8f;
        public static readonly int INIT_RAGDOLL_ID = -2;

        public static readonly float BASE_MAX_SPEED = 0.9f;
        public static readonly float BODY_TURNSPEED = 6f;
        public static readonly float CAMERA_TURNSPEED = 4f;

        public static readonly float DISTANCE_CLOSE_ENOUGH_TO_DESTINATION = 1f;
        public static readonly float DISTANCE_ITEMS_TO_ENTRANCE = 6f;

        public static readonly int COMPANY_BUILDING_MOON_ID = 3;

        public static readonly float DISTANCE_NPCBODY_FROM_DOOR = 2.5f;
        public static readonly float TIMER_CHECK_DOOR = 0.9f;

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

        // Idle
        // -1437577361
        // -1904134370,
        // -1204949837,
        // 1942734694,
        // -291778088,
        // -822567509};

        public static readonly int IDLE_STATE_HASH = -1437577361;
        public static readonly int WALKING_STATE_HASH = 81563449;
        public static readonly int SPRINTING_STATE_HASH = -350224702;
        public static readonly int CROUCHING_IDLE_STATE_HASH = 1917280335;
        public static readonly int CROUCHING_WALKING_STATE_HASH = -483816927;

        // Looking for player 
        public static readonly float MIN_TIME_SPRINT_SEARCH_WANDER = 1f;
        public static readonly float MAX_TIME_SPRINT_SEARCH_WANDER = 3f;

        // Go to position
        public static readonly float DISTANCE_START_RUNNING = 2f;
        public static readonly float DISTANCE_STOP_RUNNING = 7f;
        public static readonly float DISTANCE_CLOSE_ENOUGH_HOR = 4f;
        public static readonly float DISTANCE_CLOSE_ENOUGH_VER = 2f;
        public static readonly float DISTANCE_TO_ENTRANCE = 1f;
        public static readonly float OUTSIDE_INSIDE_DISTANCE_LIMIT = 100f;

        // Update last known pos
        public static readonly float DISTANCE_AWARENESS_HOR = 50f;
        public static readonly float DISTANCE_AWARENESS_VER = 50f;

        // Looking around
        public static readonly float TIMER_LOOKING_AROUND = 6f;
        public static readonly float MIN_TIME_FREEZE_LOOKING_AROUND = 0.5f;
        public static readonly float MAX_TIME_FREEZE_LOOKING_AROUND = 2f;

        // Cruiser vehicle
        public static readonly float DISTANCE_TO_CRUISER = 6f;

        // Front
        public static readonly Vector3 LEFT_FRONT_POS_CRUISER = new Vector3(-0.75f, -1f, 5.5f);
        public static readonly Vector3 RIGHT_FRONT_POS_CRUISER = new Vector3(1.2f, -1f, 5.5f);
        // Center
        public static readonly Vector3 LEFT_CENTER_POS_CRUISER = new Vector3(-2f, -1f, 0f);
        public static readonly Vector3 RIGHT_CENTER_POS_CRUISER = new Vector3(2.5f, -1f, 0f);
        // Back
        public static readonly Vector3 LEFT_BACK_POS_CRUISER = new Vector3(-0.75f, -1f, -5.5f);
        public static readonly Vector3 RIGHT_BACK_POS_CRUISER = new Vector3(1.2f, -1f, -5.5f);
        // Inside cruiser
        public static readonly Vector3 FIRST_CORNER_INSIDE_CRUISER = new Vector3(-0.5f, -0.5f, -0.4f);
        public static readonly Vector3 SECOND_CORNER_INSIDE_CRUISER = new Vector3(0.9f, -0.5f, -2.5f);

        // Panik
        public static readonly float DISTANCE_FLEEING_NO_LOS = 5f;

        // Tips
        public static readonly string TOOLTIP_DROP_ITEM = "Drop your item : [{0}]";
        public static readonly string TOOLTIP_TAKE_ITEM = "Take my item : [{0}]";
        public static readonly string TOOLTIP_FOLLOW_ME = "Follow me: [{0}]";
        public static readonly string TOOLTIP_GRAB_INTERNS = "Grab intern: [{0}]";
        public static readonly string TOOLTIP_RELEASE_INTERNS = "Release grabbed interns : [{0}]";
        public static readonly string TOOLTIP_CHANGE_SUIT_INTERNS = "Change suit : [{0}]";
        public static readonly string TOOLTIP_COMMANDS = "Commands : [{0}]";
        public static readonly string TOOLTIP_MAKE_INTERN_LOOK = "Make interns look : [{0}]";
        public static readonly string TOOLTIPS_ORDER_1 = "order 1 : [{0}]";
    }
}
