﻿using LethalInternship.SharedAbstractions.Enums;
using System;

namespace LethalInternship.Core.SaveAdapter
{
    /// <summary>
    /// Represents the date serializable, to be saved on disk, necessay for LethalInternship (not much obviously)
    /// </summary>
    [Serializable]
    internal class SaveFile
    {
        public bool LandingStatusAborted;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public IdentitySaveFile[] IdentitiesSaveFiles;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    }

    [Serializable]
    internal class IdentitySaveFile
    {
        public int IdIdentity;
        public int SuitID;
        public int Hp;
        public int Status;

        public override string ToString()
        {
            return $"IdIdentity: {IdIdentity}, suitID {SuitID}, Hp {Hp}, Status {Status} {(EnumStatusIdentity)Status}";
        }
    }
}
