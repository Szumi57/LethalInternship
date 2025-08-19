namespace LethalInternship.SharedAbstractions.Constants
{
    public class UIConst
    {
        public static string UI_CHOOSE_LOCATION = "Choose this location";

        public static string UI_TITLE_LIST_INTERNS = "Interns close :";
        public static string UI_TITLE_LIST_SINGLE_INTERN = "Managing intern :";


        public static string[] COMMANDS_BUTTON_STRING = {
            string.Empty, // 0 EnumInputAction
            "Choose a position", // GoToPosition
            "Follow me", // FollowMe
            "Go to the cruiser", // GoToShip
            "Go to the vehicle", // GoToVehicle
        };
    }
}
