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

            PluginLoggerHook.LogDebug?.Invoke($"new HeldItem {grabbableObject} {grabbableObject?.name}");
            EnumItemType = EnumItemTypes.Default;
        }
    }
}
