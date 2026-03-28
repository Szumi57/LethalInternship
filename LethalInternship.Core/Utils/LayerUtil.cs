using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using UnityEngine;

namespace LethalInternship.Core.Utils
{
    public static class LayerUtil
    {
        public static void LogAllLayersName()
        {
            LogLayerName(StartOfRound.Instance.collidersRoomMaskDefaultAndPlayers, "collidersRoomMaskDefaultAndPlayers");
            //----------- collidersRoomMaskDefaultAndPlayers -----------
            // Layer 0: Default
            // Layer 3: Player
            // Layer 8: Room
            // Layer 11: Colliders
            // Layer 25: Terrain
            // Layer 30: Vehicle

            LogLayerName(64, "PlayerControllerB.grabbableObjectsMask 64");
            // ----------- PlayerControllerB.grabbableObjectsMask 64 -----------
            // Layer 6: Props

            LogLayerName(StartOfRound.Instance.walkableSurfacesMask, "walkableSurfacesMask");
            // ----------- walkableSurfacesMask -----------
            // Layer 0: Default
            // Layer 3: Player
            // Layer 8: Room
            // Layer 11: Colliders
            // Layer 25: Terrain
            // Layer 28: Railing
            // Layer 30: Vehicle

            LogLayerName(StartOfRound.Instance.collidersAndRoomMaskAndDefault, "collidersAndRoomMaskAndDefault");
            // ----------- collidersAndRoomMaskAndDefault -----------
            // Layer 0: Default
            // Layer 8: Room
            // Layer 11: Colliders
            // Layer 25: Terrain
            // Layer 30: Vehicle

            LogLayerName(StartOfRound.Instance.collidersAndRoomMask, "collidersAndRoomMask");
            // ----------- collidersAndRoomMask -----------
            // Layer 8: Room
            // Layer 11: Colliders
            // Layer 25: Terrain
            // Layer 30: Vehicle

            LogLayerName(StartOfRound.Instance.collidersAndRoomMaskAndPlayers, "collidersAndRoomMaskAndPlayers");
            // ----------- collidersAndRoomMaskAndPlayers -----------
            // Layer 3: Player
            // Layer 8: Room
            // Layer 11: Colliders
            // Layer 25: Terrain
            // Layer 30: Vehicle

            LogLayerName(StartOfRound.Instance.collidersRoomDefaultAndFoliage, "collidersRoomDefaultAndFoliage");
            // ----------- collidersRoomDefaultAndFoliage -----------
            // Layer 0: Default
            // Layer 8: Room
            // Layer 10: Foliage
            // Layer 11: Colliders
            // Layer 25: Terrain
            // Layer 30: Vehicle

            LogLayerName(StartOfRound.Instance.allPlayersCollideWithMask, "allPlayersCollideWithMask");
            // ----------- allPlayersCollideWithMask -----------
            // Layer 0: Default
            // Layer 1: TransparentFX
            // Layer 2: Ignore Raycast
            // Layer 4: Water
            // Layer 5: UI
            // Layer 7: HelmetVisor
            // Layer 8: Room
            // Layer 9: InteractableObject
            // Layer 11: Colliders
            // Layer 13: Triggers
            // Layer 14: MapRadar
            // Layer 16: MoldSpore
            // Layer 17: Anomaly
            // Layer 19: Enemies
            // Layer 20: PlayerRagdoll
            // Layer 21: MapHazards
            // Layer 23: EnemiesNotRendered
            // Layer 24: MiscLevelGeometry
            // Layer 26: PlaceableShipObjects
            // Layer 27: PlacementBlocker
            // Layer 28: Railing
            // Layer 29: DecalStickableSurface
            // Layer 31: 
        }

        private static void LogLayerName(int layer, string name)
        {
            PluginLoggerHook.LogDebug?.Invoke($"----------- {name} -----------");
            for (int i = 0; i < 32; i++)
            {
                if ((layer & (1 << i)) != 0)
                {
                    PluginLoggerHook.LogDebug?.Invoke($"Layer {i}: {LayerMask.LayerToName(i)}");
                }
            }
        }
    }
}
