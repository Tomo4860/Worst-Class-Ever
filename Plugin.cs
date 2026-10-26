using System;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BigClassMod
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "rm.worstclassever.bigclass";
        public const string PluginName = "BigClass";
        public const string PluginVersion = "1.1.0";

        internal static ManualLogSource Log;
        internal static ConfigFile Cfg;

        internal static ConfigEntry<int> MaxPlayers;
        internal static ConfigEntry<int> MaxTeachers;

        private void Awake()
        {
            Log = Logger;
            Cfg = Config;

            MaxPlayers = Config.Bind(
                "Room",
                "MaxPlayers",
                10,
                new ConfigDescription(
                    "房间人数上限（含主机）。原版为 6。有效范围 2-16。\n只改这个值不会改变教室座位布局，超过 8 人时座位会向后延伸。",
                    new AcceptableValueRange<int>(2, 16)));

            MaxTeachers = Config.Bind(
                "Class",
                "MaxTeachers",
                2,
                new ConfigDescription(
                    "上课时可同时存在的“老师”人数上限。原版为 1（开课时只保留一名老师，其余强制转学生）。\n有效范围 1-8。",
                    new AcceptableValueRange<int>(1, 8)));

            Log.LogInfo($"{PluginName} {PluginVersion} loaded. MaxPlayers={MaxPlayers.Value}, MaxTeachers={MaxTeachers.Value}");

            try
            {
                var harmony = new Harmony(PluginGuid);
                harmony.PatchAll(typeof(Plugin).Assembly);
                Log.LogInfo("[BigClass] Harmony PatchAll 完成");
                SelfCheck();
            }
            catch (Exception ex)
            {
                Log.LogError($"[BigClass] Harmony 补丁失败：{ex}");
            }
        }

        private static void SelfCheck()
        {
            Check(typeof(RoomManager), "InitServerRoom", Type.EmptyTypes);
            Check(typeof(RoomManager), "GetPlayerRoomInitPosition", new[] { typeof(int), typeof(int) });
            Check(typeof(ClassroomManager), "AssignClassTeacher", new[] { typeof(System.Collections.Generic.List<Unit>), typeof(Vector3) });
            Check(typeof(UOSRoomUtility), "GetPlayerLimit", new[] { typeof(System.Collections.Generic.Dictionary<string, string>), typeof(uint) });
            Check(typeof(LobbyServiceHelper), "CreateRoomAsync", new[] { typeof(Unity.Sync.Relay.Lobby.CreateRoomRequest), typeof(System.Threading.CancellationToken) });
            Check(typeof(SteamLobbyManager), "OnLobbyCreated", null);
        }

        private static void Check(Type type, string methodName, Type[] args)
        {
            MethodInfo method = args == null
                ? AccessTools.Method(type, methodName)
                : AccessTools.Method(type, methodName, args);

            if (method == null)
            {
                Log.LogError($"[BigClass] 自检失败：找不到 {type.Name}.{methodName}");
                return;
            }

            HarmonyLib.Patches info = Harmony.GetPatchInfo(method);
            bool ours = info != null
                && (info.Prefixes.Any(p => p.owner == PluginGuid)
                    || info.Postfixes.Any(p => p.owner == PluginGuid)
                    || info.Transpilers.Any(p => p.owner == PluginGuid));

            if (ours)
            {
                Log.LogInfo($"[BigClass] 自检通过：{type.Name}.{methodName}");
            }
            else
            {
                Log.LogError($"[BigClass] 自检失败：{type.Name}.{methodName} 没有本插件的补丁");
            }
        }
    }
}

