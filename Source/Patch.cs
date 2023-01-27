using HarmonyLib;
using Verse;
using RimWorld;
using System.Collections.Generic;
using Settings = VariableRoomSizes.ModSettings_VariableRoomSizes;

namespace VariableRoomSizes
{
    [StaticConstructorOnStartup]
    public static class Setup
    {
        static Setup()
        {
            new Harmony("Owlchemist.VariableRoomSizes").PatchAll();
            Reset();
        }

        public static void Reset()
        {
            //Setup data for new mod users
            if (Settings.customMultipliers == null) Settings.customMultipliers = new Dictionary<string, float>();
            Patch_RoomStatWorker_Space.multiplierCache = new Dictionary<RoomRoleDef, float>();
            
            var list = DefDatabase<RoomRoleDef>.AllDefsListForReading;
            var length = list.Count;
            Settings.customMultipliersCache = new float[length]; //Used for the mod options ref
            try
            {
                for (int i = 0; i < length; i++)
                {
                    var roomRoleDef = list[i];
                    
                    if (!Settings.customMultipliers.TryGetValue(roomRoleDef.defName, out float customValue))
                    {
                        var defX = roomRoleDef.GetModExtension<Size>();
                        if (defX != null) customValue = defX.multiplier;
                        else customValue = 1f;
                    }
                    Patch_RoomStatWorker_Space.multiplierCache.Add(roomRoleDef, customValue);
                    Settings.customMultipliersCache[i] = customValue;
                }
            }
            catch (System.Exception ex)
            {
                Log.Error("[Variable Room Sizes] Error setting up cache:\n" + ex);
            }
        }
    }

    [HarmonyPatch(typeof(RoomStatWorker_Space), nameof(RoomStatWorker_Space.GetScore))]
	public class Patch_RoomStatWorker_Space
    {
        public static Dictionary<RoomRoleDef, float> multiplierCache;
        static float Postfix(float __result, Room room)
        {
            if (room.role != null && multiplierCache.TryGetValue(room.role, out float multiplier))
            {
                return __result * multiplier;
            }        
            return __result;
        }
    }

    //When the room is first calculated, a second pass is needed, but only for the space stat. This is because it doesn't know what the room type is yet until the first pass finishes.
    [HarmonyPatch(typeof(Room), nameof(Room.UpdateRoomStatsAndRole))]
	public class Patch_UpdateRoomStatsAndRole
    {
        static void Postfix(Room __instance)
        {
            if (__instance.stats == null) return;
            __instance.stats[RoomStatDefOf.Space] = RoomStatDefOf.Space.Worker.GetScore(__instance);
        }
    }
}