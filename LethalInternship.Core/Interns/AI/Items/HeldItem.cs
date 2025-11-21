using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using System;
using System.Collections.Generic;
using System.Text;

namespace LethalInternship.Core.Interns.AI.Items
{
    public class HeldItem
    {
        public GrabbableObject? GrabbableObject { get; set; }

        public EnumItemTypes EnumItemType { get; set; }
        public bool IsTwoHanded => GrabbableObject != null && GrabbableObject.itemProperties.twoHanded;

        public HeldItem(GrabbableObject? grabbableObject)
        {
            GrabbableObject = grabbableObject;

            if (grabbableObject != null)
            {
                if (grabbableObject.name.Contains("ShovelItem")
                    || grabbableObject.name.Contains("StopSign")
                    || grabbableObject.name.Contains("YieldSign")
                    || grabbableObject.name.Contains("KnifeItem"))
                {
                    EnumItemType = EnumItemTypes.WeaponMelee;
                }
                else if (grabbableObject.name.Contains("ShotgunItem")
                    || grabbableObject.name.Contains("PatcherGunItem"))
                {
                    EnumItemType = EnumItemTypes.WeaponRanged;
                }
                else
                {
                    EnumItemType = EnumItemTypes.Default;
                }
            }
        }

        public override string ToString()
        {
            return $"HeldItem {GrabbableObject}, type {EnumItemType}, IsTwoHanded {IsTwoHanded}";
        }
    }
}
