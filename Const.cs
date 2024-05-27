namespace LethalInternship
{
    internal class Const
    {
        public static readonly int INTERN_AVAILABLE = 16;
        public static readonly float EPSILON = 0.01f;

        public static readonly float INTERN_FOV = 90f;
        public static readonly float INTERN_OBJECT_AWARNESS = 10f;
        public static readonly int INTERN_OBJECT_RANGE = 20;


        public static readonly float BASE_MAX_SPEED = 1f;
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
        public static readonly float DISTANCE_CLOSE_ENOUGH_LAST_KNOWN_POSITION = 0.5f;

        // ChillWithPlayerState

        // StuckState
        public static readonly float TIMER_STUCK = 0.3f;
        public static readonly float TIMER_STUCK_AFTER_TRIED_JUMP = 1.5f;
        public static readonly float TIMER_STUCK_TOO_MUCH = 5f;


        public static readonly float DISTANCE_AI_FROM_LADDER = 5f;
        public static readonly float DISTANCE_NPCBODY_FROM_LADDER = 2f;
        public static readonly float DISTANCE_NPCBODY_FROM_DOOR = 2.5f;
        public static readonly float TIMER_CHECK_DOOR = 0.9f;
        public static readonly float TIMER_CHECK_LADDERS = 1.2f;
    }
}
