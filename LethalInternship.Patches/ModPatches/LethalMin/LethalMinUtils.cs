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
            List<PikminItem> listPickMinItems = PikminManager.GetPikminItemsInMap();
            if (listPickMinItems == null
                || listPickMinItems.Count == 0)
            {
                return false;
            }

            foreach (var item in listPickMinItems)
            {
                if (item != null
                    && item.Root == grabbableObject
                    && item.PikminOnItem > 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
