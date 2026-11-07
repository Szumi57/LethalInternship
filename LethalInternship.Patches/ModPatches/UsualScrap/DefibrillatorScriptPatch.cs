namespace LethalInternship.Patches.ModPatches.UsualScrap
{
    public class DefibrillatorScriptPatch
    {
        //public static readonly FieldInfo FieldUsesLimited = AccessTools.Field(AccessTools.TypeByName("UsualScrap.Behaviors.DefibrillatorScript"), "UsesLimited");
        //public static readonly FieldInfo FieldUseLimit = AccessTools.Field(AccessTools.TypeByName("UsualScrap.Behaviors.DefibrillatorScript"), "UseLimit");
        //public static readonly FieldInfo FieldDisplayRenderers = AccessTools.Field(AccessTools.TypeByName("UsualScrap.Behaviors.DefibrillatorScript"), "displayRenderers");

        public static void Findbody()
        {
            // ragdoll.ragdoll is null, line 306 on github
            // error but not critical
        }
    }
}
