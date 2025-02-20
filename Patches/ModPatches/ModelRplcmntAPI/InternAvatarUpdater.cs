using ModelReplacement.AvatarBodyUpdater;
using System.Collections.Generic;
using UnityEngine;

namespace LethalInternship.Patches.ModPatches.ModelRplcmntAPI
{
    public class InternAvatarUpdater
    {
        public GameObject ReplacementModelRef;

        public Transform AvatarTransformFromBoneName;
        public Transform PlayerTransformFromBoneName;

        public List<InternAvatarUpdaterBones> InternAvatarUpdaterBones;

        public InternAvatarUpdater(AvatarUpdater avatarUpdater, 
                                   SkinnedMeshRenderer playerModelRenderer,
                                   GameObject replacementModelRef)
        {
            ReplacementModelRef = replacementModelRef;
            AvatarTransformFromBoneName = avatarUpdater.GetAvatarTransformFromBoneName("spine");
            PlayerTransformFromBoneName = avatarUpdater.GetPlayerTransformFromBoneName("spine");

            InternAvatarUpdaterBones = new List<InternAvatarUpdaterBones>();
            foreach(Transform bone in playerModelRenderer.bones)
            {
                InternAvatarUpdaterBones.Add(new InternAvatarUpdaterBones(avatarUpdater, bone));
            }
        }
    }

    public class InternAvatarUpdaterBones
    {
        public Transform Bone;
        public Transform? AvatarTransformFromBoneName2;
        public RotationOffset? RotationOffsetComponent;

        public InternAvatarUpdaterBones(AvatarUpdater avatarUpdater, Transform bone)
        {
            Bone = bone;
            AvatarTransformFromBoneName2 = avatarUpdater.GetAvatarTransformFromBoneName(bone.name);
            if (AvatarTransformFromBoneName2 != null)
            {
                RotationOffsetComponent = AvatarTransformFromBoneName2.GetComponent<RotationOffset>();
            }
        }
    }
}
