using UnityEngine;

namespace LethalInternship.SharedAbstractions.Constants
{
    public class UIConst
    {
        public static Color UI_COLOR_DEFAULT = new Color(255 / 255f, 52 / 255f, 1 / 255f); // 255 52 1 orange/red kinda lethalcompany
        public static Color UI_COLOR_ORANGE = new Color(255 / 255f, 111 / 255f, 1 / 255f); // 255 111 1 orange
        public static Color UI_COLOR_BLACK = new Color(0f, 0f, 0f); // black

        public static string UI_INPUT_MESSAGE_KEYBOARD = "Press : [Mouse click] to confirm";
        public static string UI_INPUT_MESSAGE_CONTROLLER = "Press : [{0}] to confirm | [{1}] to close";

        public static string UI_CHOOSE_LOCATION = "Choose this location";

        public static string UI_TITLE_COMMANDS_ALL = "Commands :";
        public static string UI_TITLE_LIST_INTERNS = "Interns close :";
        public static string UI_TITLE_LIST_SINGLE_INTERN = "Managing intern :";

        // Cursor tooltips
        public static readonly string TOOLTIP_GIVE_ITEM = "Give item : [{0}]";
        public static readonly string TOOLTIP_MANAGE = "Manage : [{0}]";
        public static readonly string TOOLTIP_GRAB_INTERNS = "Grab intern: [{0}]";
        public static readonly string TOOLTIP_RELEASE_INTERNS = "Release grabbed interns : [{0}]";
        public static readonly string TOOLTIP_COMMANDS_ALL = "Commands for all interns : [{0}]";
        public static readonly string TOOLTIP_COMMANDS_ONE = "Commands : [{0}]";
        public static readonly string TOOLTIP_MAKE_INTERN_LOOK = "Make interns look : [{0}]";
        public static readonly string TOOLTIP_TARGETING_ENEMY = "-> Attack !";
        public static readonly string TOOLTIP_TARGETING_UNKILLABLE_ENEMY = "Can't attack this enemy !";
        public static readonly string TOOLTIP_TARGETING_ITEM = "-> Go get this item";
        public static readonly string TOOLTIP_TARGETING_POSITION = "-> Go there";

        public static readonly string[] TOOLTIP_COMMAND_FEEDBACK = {
        "[...]",// None = 0,
        "[...]",// ExecutingCommand,
        "[Hands full]",// NoFreeSlotsAvailable,
        "[Cannot hold this item]",// CantHoldItem,
        "[Cannot take this item]",// TargetItemNotGrabbable,
        "[No more items to grab]",// NoItemsLeftToGrab,
        "[Thinking]",// Thinking,
        "[Not holding any weapon]",// NotHoldingWeapon,
        "[This monster is unkillable]",// CantKillEnemy,
        "[Fleeing !]",// Fleeing,
        "[Combat too long, aborting...]",// CombatTooLong,
        "[The monster is not here]",// EnemyNotHere,
        "[The monster is too far]",// EnemyTooFar,
        };

        public static readonly string[] TOOLTIP_EXECUTING_COMMAND = {
        "[...]",// None = 0,
        "[Following]",// FollowPlayer,
        "[Going to the cruiser]",// GoToVehicle,
        "[Going to the position]",// GoToPosition,
        "[Waiting for command]",// WaitForCommand,
        "[Scavenge -> Ship]",// ScavengingToShip,
        "[Scavenge -> Cruiser]",// ScavengingToCruiser,
        "[Scavenge -> Gathering point]",// ScavengingToGatheringPoint,
        "[Grabbing the item]",// GoFetchItem,
        "[Kill]",// Kill,
        "[Drop all items -> Ship]",// DropAllItemsToShip,
        "[Drop all items -> Gathering point]",// DropAllItemsOnGatheringPoint,
        "[Drop all items -> Cruiser]",// DropAllItemsInCruiser,
        "[Unloading cruiser]",// UnloadCruiser,
        "[Unloading gathering point]",// UnloadGatheringPoint,
        };

        public static string[] COMMANDS_BUTTON_STRING = {
            string.Empty,
            "Follow me",// FollowMe
            "Stay here",// StayHere
            "Point to action",// PointToAction
            "Flee when an enemy is near ",// SetToAutoFlee
            "Try to attack when an enemy is near",// SetToAutoDefense
            "Drop item",// DropItem
            "Drop all items",// DropAllItems
            "Go to the ship",// GoToShip
            "Set new gathering point",// SetGatheringPoint
            "Go to the gathering point",// GoToGatheringPoint
            "Delete the gathering point",// RemoveGatheringPoint
            "Go to the cruiser",// GoToVehicle
            "Scavenge and return to ship",// ScavengeToShip
            "Scavenge and return to the gathering point", // ScavengeToGatheringPoint
            "Scavenge and return to the cruiser", // ScavengeToCruiser
            "Previous suit", // PreviousSuit,
            "Next suit", // NextSuit,
            "Same suit as player", // SameSuit,
            "Random suit", // RandomSuit,
            "Choose suit", // SelectSuit,
            "Close", // Close,
            "See all interns", // ReturnToAll,
            "Previous intern", // PreviousIntern,
            "Next intern", // NextIntern,
            "Unload items in cruiser", // UnloadCruiser,
            "Unload items near the gathering point", // UnloadGatheringPoint,
            "Drop all items in the ship", // DropAllItemsInShip,
            "Drop all items on the gathering point", // DropAllItemsOnGatheringPoint,
            "Drop all items in the cruiser", // DropAllItemsInCruiser,
            "Swap weapon", // SwapWeapon,
            "Use", // ActivateItem,
        };

        public static string[] CATEGORIES_STRING = {
            "- None ({0})",// None
            "- In proximity ({0})",// InternClose
            "- Too far ({0})",// InternTooFar
            "- Not managed ({0})",// InternNotOwned
            "- Dead ({0})",// InternDead
            "- Weapon ({0})",// HeldWeapon
            "- {0} Items ${1}",// HeldItem
        };

        public static string TOOLTIP_AUTODEFENSE_BEHAVIOUR = "[Attack enemy if holding weapon]";
        public static string TOOLTIP_FLEE_BEHAVIOUR = "[Flee if enemy]";

        public static float DISTANCE_UI_PROXIMITY = 300f;

        // Outlines
        public static float OUTLINE_RIM_DEFAULT = 5f;
        public static float OUTLINE_RIM_SOLID = 0.1f;
        public static float DISTANCE_SOLID_OUTLINE = 15f;
        public static float OUTLINE_INTENSITY_DEFAULT = 0.7f;

        // TooltipBarUI
        public static string TOOLTIPBAR_ITEM = "{0} ${1}";
        public static string TOOLTIPBAR_NO_CRUISER = "No cruiser spawned !";
        public static string TOOLTIPBAR_NO_GATHERINGPOINT = "No gathering point set !";
        public static string TOOLTIPBAR_NO_INTERNS_TO_MANAGE = "Not managing any interns !";
        public static string TOOLTIPBAR_NOT_IN_SPACE = "Not available in space.";
    }
}
