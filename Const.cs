namespace LethalInternship
{
    internal class Const
    {
        public static readonly int INTERN_AVAILABLE = 16;
        public static readonly float EPSILON = 0.01f;

        public static readonly float INTERN_FOV = 90f;

        public static readonly float INTERN_OBJECT_AWARNESS = 3f;
        public static readonly float INTERN_OBJECT_RANGE = 20f;
        public static readonly float WAIT_TIME_FOR_GRAB_DROPPED_OBJECTS = 10f;

        public static readonly float BASE_MAX_SPEED = 0.9f;
        public static readonly float BASE_MIN_SPEED = 0.04f;
        public static readonly float BODY_TURNSPEED = 6f;
        public static readonly float CAMERA_TURNSPEED = 8f;

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
        public static readonly float DISTANCE_CLOSE_ENOUGH_LAST_KNOWN_POSITION = 1f;
        public static readonly float WAIT_TIME_TO_TELEPORT = 0.7f;
        public static readonly float DISTANCE_TO_ENTRANCE = 4f;

        // ChillWithPlayerState

        // StuckState
        public static readonly float TIMER_STUCK_TOO_MUCH = 2f;
        public static readonly float TIMER_STUCK_WAY_TOO_MUCH = 5f;

        // Player in ShipState
        public static readonly float DISTANCE_TO_SHIP_BOUND_CLOSEST_POINT = 0.5f;
        public static readonly float DISTANCE_OF_DROPPED_OBJECT_SHIP_BOUND_CLOSEST_POINT = 2f;


        public static readonly float DISTANCE_AI_FROM_LADDER = 5f;
        public static readonly float DISTANCE_NPCBODY_FROM_LADDER = 2f;
        public static readonly float DISTANCE_NPCBODY_FROM_DOOR = 2.5f;
        public static readonly float TIMER_CHECK_DOOR = 0.9f;
        public static readonly float TIMER_CHECK_LADDERS = 1.2f;
    }
}
