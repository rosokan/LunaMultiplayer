﻿using Harmony;
using LunaClient.Systems.ShareStrategy;
using LunaCommon.Enums;
using Strategies;
// ReSharper disable All

namespace LunaClient.Harmony
{
    /// <summary>
    /// This harmony patch is intended to disable the check for the strategy deactivation,
    /// when the ShareStrategySystem is processing incoming messages.
    /// So the strategy can be deactivated without this check.
    /// </summary>
    [HarmonyPatch(typeof(Strategy))]
    [HarmonyPatch("CanBeDeactivated")]
    class Strategy_CanBeDeactivated
    {
        [HarmonyPrefix]
        private static bool PrefixCanBeDeactivated(ref bool __result)
        {
            if (MainSystem.NetworkState < ClientState.Connected || !ShareStrategySystem.Singleton.Enabled) return true;

            if (ShareStrategySystem.Singleton.IgnoreEvents)
            {
                __result = true;
                return false;
            }

            return true;
        }
    }
}
