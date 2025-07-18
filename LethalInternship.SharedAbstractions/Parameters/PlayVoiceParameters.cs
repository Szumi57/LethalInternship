using LethalInternship.SharedAbstractions.Enums;

namespace LethalInternship.SharedAbstractions.Parameters
{
    public struct PlayVoiceParameters
    {
        public bool CanTalkIfOtherInternTalk { get; set; }
        public bool WaitForCooldown { get; set; }
        public bool CutCurrentVoiceStateToTalk { get; set; }
        public bool CanRepeatVoiceState { get; set; }

        public EnumVoicesState VoiceState { get; set; }

        public bool ShouldSync { get; set; }
        public bool IsInternInside { get; set; }
        public bool AllowSwearing { get; set; }
    }
}
