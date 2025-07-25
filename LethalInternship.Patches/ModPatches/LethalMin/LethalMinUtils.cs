using LethalInternship.SharedAbstractions.Hooks.LethalMinHooks;
using LethalMin;
using System.Collections.Generic;

namespace LethalInternship.Patches.ModPatches.LethalMin
{
    public class LethalMinUtils
    {
        public static void Init()
        {
            LethalMinHook.IsGrabbableObjectHeldByPikminMod = IsGrabbableObjectHeldByPikminMod;
        }

        public static bool IsGrabbableObjectHeldByPikminMod(GrabbableObject grabbableObject)
        {
            HashSet<PikminItem> listPickMinItems = PikminManager.instance.PikminItems;
            if (listPickMinItems == null
                || listPickMinItems.Count == 0)
            {
                return false;
            }

            foreach (var item in listPickMinItems)
            {
                if (item != null
                    && item.ItemScript == grabbableObject
                    && item.PikminOnItem.Count > 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
