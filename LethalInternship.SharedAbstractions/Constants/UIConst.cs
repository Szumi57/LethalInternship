using UnityEngine;

namespace LethalInternship.SharedAbstractions.Constants
{
    public class UIConst
    {
        public static string UI_CHOOSE_LOCATION = "Choose this location";

        public static string UI_TITLE_LIST_INTERNS = "Interns close :";
        public static string UI_TITLE_LIST_SINGLE_INTERN = "Managing intern :";


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
        public static float DISTANCE_SOLID_OUTLINE = 10f;
        public static float OUTLINE_INTENSITY_DEFAULT = 0.7f;
    }
}
