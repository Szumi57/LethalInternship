using LethalInternship.SharedAbstractions.Enums;
using UnityEngine;

namespace LethalInternship.SharedAbstractions.Interns
{
    public interface IInternCullingBodyInfo
    {
        Component InternBody { get; set; }
        EnumBodyTypeCulling EnumBodyTypeCulling { get; set; }

        bool BodyInFOV { get; set; }
        bool HasModelReplacement { get; set; }
        int? RankDistanceAnyModel { get; set; }
        int? RankDistanceNoModelReplacement { get; set; }
        int? RankDistanceWithModelReplacement { get; set; }
        int? RankDistanceAnyModelInFOV { get; set; }
        int? RankDistanceNoModelReplacementInFOV { get; set; }
        int? RankDistanceWithModelReplacementInFOV { get; set; }
        float TimerRagdollUpdateModelReplacement { get; set; }

        void Init(bool hasModelReplacement);

        void ResetBodyInfos();
        bool IsRankDistanceAnyModelInFOVValid(int rankDistanceMax);
        float GetSqrDistanceWithLocalPlayer();
        bool CheckIsInFOV();
    }
}
