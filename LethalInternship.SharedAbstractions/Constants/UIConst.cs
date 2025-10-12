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
    }
}
