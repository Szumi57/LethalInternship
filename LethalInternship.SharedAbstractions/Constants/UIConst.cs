using UnityEngine;

namespace LethalInternship.SharedAbstractions.Constants
{
    public class UIConst
    {
        public static string UI_CHOOSE_LOCATION = "Choose this location";

        public static string UI_TITLE_LIST_INTERNS = "Interns close :";
        public static string UI_TITLE_LIST_SINGLE_INTERN = "Managing intern :";


        // Cursor tooltips
        public static readonly string TOOLTIP_DROP_ITEM = "Drop your item : [{0}]";
        public static readonly string TOOLTIP_TAKE_ITEM = "Take my item : [{0}]";
        public static readonly string TOOLTIP_FOLLOW_ME = "Follow me: [{0}]";
        public static readonly string TOOLTIP_GRAB_INTERNS = "Grab intern: [{0}]";
        public static readonly string TOOLTIP_RELEASE_INTERNS = "Release grabbed interns : [{0}]";
        public static readonly string TOOLTIP_CHANGE_SUIT_INTERNS = "Change suit : [{0}]";
        public static readonly string TOOLTIP_COMMANDS = "Intern commands : [{0}]";
        public static readonly string TOOLTIP_MAKE_INTERN_LOOK = "Make interns look : [{0}]";

        public static string[] COMMANDS_BUTTON_STRING = {
            string.Empty, // 0 EnumInputAction
            "Choose a position", // 1 GoToPosition
            "Follow me", // 2 FollowMe
            "Go to the cruiser", // 3 GoToShip
            "Go to the vehicle", // 4 GoToVehicle
            "",
            "",
            "",
            "Go scavenging" // 8 Scavenging
        };

        // Outlines
        public static Color OUTLINE_COLOR_DEFAULT = new Color(255 / 255f, 111 / 255f, 1 / 255f); // 255 111 1
        public static float OUTLINE_RIM_DEFAULT = 5f;
        public static float OUTLINE_RIM_SOLID = 0.1f;
        public static float DISTANCE_SOLID_OUTLINE = 15f;
        public static float OUTLINE_INTENSITY_DEFAULT = 0.7f;
    }
}
