using System;

namespace LethalInternship.Managers.SaveInfos
{
    /// <summary>
    /// Represents the date serializable, to be saved on disk, necessay for LethalInternship (not much obviously)
    /// </summary>
    [Serializable]
    internal class SaveFile
    {
        public int NbInternOwned;
        public bool LandingStatusAborted;
    }
}
