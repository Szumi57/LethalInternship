using GameNetcodeStuff;

namespace LethalInternship.SharedAbstractions.Interns
{
    /// <summary>
    /// Pilot class of the body <c>PlayerControllerB</c> of the intern.
    /// </summary>
    public interface IRagdollInternBody
    {
        public void SetGrabbedBy(PlayerControllerB playerGrabberController,
                                 DeadBodyInfo deadBodyInfo,
                                 int idPlayerHolder);
        public void Hide();
        public void SetFreeRagdoll(DeadBodyInfo deadBodyInfo);
        public float GetWeight();
        public DeadBodyInfo? GetDeadBodyInfo();
        public bool IsRagdollBodyHeld();
        public bool IsRagdollBodyHeldByPlayer(int idPlayer);
        public PlayerControllerB GetPlayerHolder();
        public bool IsRagdollEnabled();
    }
}
