using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Steamworks;
using Unity.Sync.Relay.Lobby;
using UnityEngine;

namespace BigClassMod.Patches
{
    /// <summary>
    /// 服务端房间容量。原版 RoomManager.InitServerRoom() 里写死 new Room(10001, 6)。
    /// 这里整体接管该方法，按配置值重建房间并保留原有的消息处理器注册与广播顺序。
    /// </summary>
    [HarmonyPatch(typeof(RoomManager), "InitServerRoom")]
    internal static class Patch_InitServerRoom
    {
        private static readonly FieldInfo RoomField = AccessTools.Field(typeof(RoomManager), "mCurrentRoom");
        private static readonly MethodInfo RegisterHandlers = AccessTools.Method(typeof(RoomManager), "RegisterServerMessageHandlers");

        [HarmonyPrefix]
        private static bool Prefix(RoomManager __instance)
        {
            RegisterHandlers.Invoke(__instance, null);
            RoomField.SetValue(__instance, new Room(10001, Plugin.MaxPlayers.Value));
            MsgCenter.Broadcast(MsgID.OnLoadGameWorldData);
            Plugin.Log.LogInfo($"[BigClass] 服务端房间容量 = {Plugin.MaxPlayers.Value}");
            return false;
        }
    }

    /// <summary>
    /// UOS 中继房间创建时上报的 MaxPlayers（原版写死 6）。中继服务按这个值分配房间。
    /// </summary>
    [HarmonyPatch(typeof(LobbyServiceHelper), nameof(LobbyServiceHelper.CreateRoomAsync))]
    internal static class Patch_CreateRoomAsync
    {
        [HarmonyPrefix]
        private static void Prefix(CreateRoomRequest request)
        {
            if (request != null)
            {
                request.MaxPlayers = Plugin.MaxPlayers.Value;
            }
        }
    }

    /// <summary>
    /// Steam 大厅：创建完成后改写展示/校验用的 roomMaxPlayers，并把 Steam 大厅成员上限抬到同一数值。
    /// 原版大厅创建时用的固定上限是 16。
    /// </summary>
    [HarmonyPatch(typeof(SteamLobbyManager), "OnLobbyCreated")]
    internal static class Patch_OnLobbyCreated
    {
        [HarmonyPostfix]
        private static void Postfix(SteamLobbyManager __instance)
        {
            CSteamID lobbyId = __instance.LobbyID;
            if (!lobbyId.IsValid())
            {
                return;
            }

            int max = Plugin.MaxPlayers.Value;
            SteamMatchmaking.SetLobbyData(lobbyId, "roomMaxPlayers", max.ToString());
            bool memberLimit = SteamMatchmaking.SetLobbyMemberLimit(lobbyId, max);
            Plugin.Log.LogInfo($"[BigClass] Steam 大厅人数上限设为 {max}（SetLobbyMemberLimit={memberLimit}）");
        }
    }

    /// <summary>
    /// 房间列表与“房间是否满”的判定。原版对 playerLimit 属性做了 result &lt;= 6 的硬校验，
    /// 超过 6 会被丢弃并回落到 fallback。这里去掉上限，只拒绝非法值。
    /// </summary>
    [HarmonyPatch(typeof(UOSRoomUtility), nameof(UOSRoomUtility.GetPlayerLimit), new[] { typeof(Dictionary<string, string>), typeof(uint) })]
    internal static class Patch_GetPlayerLimit
    {
        [HarmonyPrefix]
        private static bool Prefix(Dictionary<string, string> properties, uint fallback, ref uint __result)
        {
            if (properties != null
                && properties.TryGetValue(UOSRoomUtility.PlayerLimitProperty, out string raw)
                && uint.TryParse(raw, out uint parsed)
                && parsed != 0)
            {
                __result = parsed;
            }
            else
            {
                __result = fallback;
            }

            return false;
        }
    }

    /// <summary>
    /// 教室入座点。原版 GetPlayerRoomInitPosition 把座位索引夹在 0-7，8 人以上会全部叠到 0 号位。
    /// 这里把夹取上限放宽到 63，座位按原来的“每排两人”规则继续向后排。
    /// </summary>
    [HarmonyPatch(typeof(RoomManager), "GetPlayerRoomInitPosition")]
    internal static class Patch_SeatIndexClamp
    {
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == System.Reflection.Emit.OpCodes.Ldc_I4_7)
                {
                    instruction.opcode = System.Reflection.Emit.OpCodes.Ldc_I4_S;
                    instruction.operand = (sbyte)63;
                }

                yield return instruction;
            }
        }
    }
}
