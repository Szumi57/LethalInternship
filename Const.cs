﻿namespace LethalInternship
{
    internal class Const
    {
        public const string MORECOMPANY_GUID = "me.swipez.melonloader.morecompany";
        public static readonly float EPSILON = 0.01f;
        public static readonly bool DISABLE_ORIGINAL_GAME_DEBUG_LOGS = true;

        public static readonly int INTERN_AVAILABLE_MAX = 16;
        public static readonly int INTERN_MAX_HEALTH = 50;
        public static readonly float SIZE_SCALE_INTERN = 0.85f;

        public static readonly float INTERN_FOV = 90f;
        public static readonly int INTERN_ENTITIES_RANGE = 40;

        public static readonly float INTERN_OBJECT_AWARNESS = 3f;
        public static readonly float INTERN_OBJECT_RANGE = 15f;
        public static readonly float WAIT_TIME_FOR_GRAB_DROPPED_OBJECTS = 10f;

        public static readonly float BASE_MAX_SPEED = 0.9f;
        public static readonly float BASE_MIN_SPEED = 0.01f;
        public static readonly float BODY_TURNSPEED = 6f;
        public static readonly float CAMERA_TURNSPEED = 8f;

        public static readonly float DISTANCE_CLOSE_ENOUGH_TO_DESTINATION = 1f;

        // GetCloseToPlayerState
        public static readonly float DISTANCE_START_RUNNING = 10f;
        public static readonly float DISTANCE_STOP_RUNNING = 8f;
        public static readonly float DISTANCE_CLOSE_ENOUGH_HOR = 6f;
        public static readonly float DISTANCE_CLOSE_ENOUGH_VER = 2f;
        public static readonly float DISTANCE_AWARENESS_HOR = 15f;
        public static readonly float DISTANCE_AWARENESS_VER = 50f;

        // JustLostPlayerState
        public static readonly float TIMER_LOOKING_AROUND = 4f;
        public static readonly float DISTANCE_STOP_SPRINT_LAST_KNOWN_POSITION = 2f;
        public static readonly float WAIT_TIME_TO_TELEPORT = 0.7f;
        public static readonly float DISTANCE_TO_ENTRANCE = 4f;

        // ChillWithPlayerState

        // StuckState
        public static readonly float TIMER_STUCK_TOO_MUCH = 2f;
        public static readonly float TIMER_STUCK_WAY_TOO_MUCH = 5f;

        // Player in ShipState
        public static readonly float DISTANCE_TO_SHIP_BOUND_CLOSEST_POINT = 0.5f;
        public static readonly float DISTANCE_OF_DROPPED_OBJECT_SHIP_BOUND_CLOSEST_POINT = 2f;

        // PanikState
        public static readonly float DISTANCE_FLEEING = 20f;
        public static readonly float DISTANCE_FLEEING_NO_LOS = 5f;

        public static readonly float DISTANCE_AI_FROM_LADDER = 5f;
        public static readonly float DISTANCE_NPCBODY_FROM_LADDER = 2f;
        public static readonly float DISTANCE_NPCBODY_FROM_DOOR = 2.5f;
        public static readonly float TIMER_CHECK_DOOR = 0.9f;
        public static readonly float TIMER_CHECK_LADDERS = 1.2f;

        // Terminal
        public static readonly int PRICE_INTERN = 19;
        public static readonly int INDEX_HELP_TERMINALNODE = 13;
        public static readonly string STRING_OTHER_HELP = ">OTHER";

        public static readonly string STRING_INTERNSHIP_PROGRAM_COMMAND = "internship program";
        public static readonly string STRING_BUY_COMMAND = "buy";
        public static readonly string STRING_CONFIRM_COMMAND = "confirm";
        public static readonly string STRING_CANCEL_COMMAND = "cancel";
        public static readonly string STRING_BACK_COMMAND = "back";

        public static readonly string STRING_INTERNSHIP_PROGRAM_HELP = $">{STRING_INTERNSHIP_PROGRAM_COMMAND.ToUpper()}\nNeed some help ? Try our new workforce, ready to assist you and gain experience\n\n";


        public static readonly string TEXT_INFO_PAGE_IN_SPACE = @"Numbers of interns purchasable : {0}
                                                                  
                                                                  
                                                                  
                                                                  --> Interns scheduled on next moon : {1}
                                                                  
                                                                  Type 'buy' to order new intern, 'buy 2' to order 2 interns, etc";
        public static readonly string TEXT_INFO_PAGE_INTERN_TO_DROPSHIP = "Interns waiting to land : {0}";
        public static readonly string TEXT_INFO_PAGE_ON_MOON = @"Numbers of interns purchasable : {0}
                                                                 
                                                                 
                                                                 {1}
                                                                 --> Interns on this moon : {2}
                                                                 
                                                                 Type 'buy' to order new intern, 'buy 2' to order 2 interns, etc";


        public static readonly string TEXT_CONFIRM_CANCEL_PURCHASE_MAXIMUM = " a maximum of";
        public static readonly string TEXT_CONFIRM_CANCEL_PURCHASE = @"You ordered{0} {1} more interns.
                                                                       
                                                                       Do you confirm you purchase ?
                                                                       (enter confirm or cancel)";

        public static readonly string TEXT_ERROR_DEFAULT = @"An error occured in the internship program";
        public static readonly string TEXT_ERROR_NOT_ENOUGH_CREDITS = @"You do not have enough credits to order an intern";
        public static readonly string TEXT_ERROR_SHIP_LEAVING = @"You can not buy when the ship is leaving the moon";
    }
}
