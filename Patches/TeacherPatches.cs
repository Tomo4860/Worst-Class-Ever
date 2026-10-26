using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace BigClassMod.Patches
{
    /// <summary>
    /// 开课身份分配。原版 AssignClassTeacher 只保留一名老师：
    /// 随机挑一个，其余全部强制转学生，最后用“老师数 == 1”作为开课条件。
    /// 这里改为：保留已选择老师身份的玩家，最多 MaxTeachers 名，其余转学生；
    /// 一个老师都没选时仍按原版只随机指定一名。
    /// </summary>
    [HarmonyPatch(typeof(ClassroomManager), "AssignClassTeacher")]
    internal static class Patch_AssignClassTeacher
    {
        private static readonly MethodInfo TeleportMethod =
            AccessTools.Method(typeof(ClassroomManager), "TeleportClassPlayer", new[] { typeof(Unit), typeof(Vector3), typeof(Vector3), typeof(int) });

        private const float TeacherSpacing = 0.9f;

        [HarmonyPrefix]
        private static bool Prefix(ClassroomManager __instance, List<Unit> players, Vector3 teacherPosition, ref bool __result)
        {
            int maxTeachers = Mathf.Max(1, Plugin.MaxTeachers.Value);

            List<Unit> alreadyTeachers = new List<Unit>();
            foreach (Unit player in players)
            {
                if (player != null && player.IsTeacher)
                {
                    alreadyTeachers.Add(player);
                }
            }

            List<Unit> chosen = new List<Unit>();
            if (alreadyTeachers.Count > 0)
            {
                List<Unit> shuffled = new List<Unit>(alreadyTeachers);
                for (int i = shuffled.Count - 1; i > 0; i--)
                {
                    int j = UnityEngine.Random.Range(0, i + 1);
                    (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
                }

                // 原版优先保留 1002 号老师，这里沿用同样的偏好。
                foreach (Unit player in shuffled)
                {
                    if (chosen.Count >= maxTeachers)
                    {
                        break;
                    }

                    if (player.ID == 1002)
                    {
                        chosen.Add(player);
                    }
                }

                foreach (Unit player in shuffled)
                {
                    if (chosen.Count >= maxTeachers)
                    {
                        break;
                    }

                    if (!chosen.Contains(player))
                    {
                        chosen.Add(player);
                    }
                }
            }
            else if (players.Count > 0)
            {
                chosen.Add(players[UnityEngine.Random.Range(0, players.Count)]);
            }

            RoomManager roomManager = MonoSingletonT<RoomManager>.Instance;
            ClassroomMessageHandler handler = roomManager.GetClassroomMessageHandler();
            uint hostUid = MonoSingletonT<UnitManager>.Instance.localPlayerUID;

            foreach (Unit player in players)
            {
                if (player == null)
                {
                    continue;
                }

                bool shouldBeTeacher = chosen.Contains(player);
                if (player.IsTeacher != shouldBeTeacher)
                {
                    ForceChangeIndentifyMessageRequest_C2S request = new ForceChangeIndentifyMessageRequest_C2S
                    {
                        unitUID = player.UID,
                        isTeacher = shouldBeTeacher,
                        position = shouldBeTeacher ? GetTeacherPosition(teacherPosition, chosen.IndexOf(player), chosen.Count) : Vector3.zero
                    };
                    handler.OnForceChangeIndentifyMessageRequest(roomManager.GetConnectByPlayerUID(hostUid), request);
                }
                else if (shouldBeTeacher && TeleportMethod != null)
                {
                    TeleportMethod.Invoke(__instance, new object[]
                    {
                        player,
                        GetTeacherPosition(teacherPosition, chosen.IndexOf(player), chosen.Count),
                        Vector3.zero,
                        0
                    });
                }
            }

            int teacherCount = 0;
            Room room = roomManager.CurrentRoom;
            if (room != null)
            {
                foreach (Unit unit in room.GetPlayers().Values)
                {
                    if (unit != null && unit.isPlayer && unit.isActived && !unit.IsDead && unit.IsTeacher)
                    {
                        teacherCount++;
                    }
                }
            }

            __result = teacherCount >= 1 && teacherCount <= maxTeachers;
            Plugin.Log.LogInfo($"[BigClass] 开课身份分配：老师 {teacherCount} 名（上限 {maxTeachers}），开课={__result}");
            return false;
        }

        private static Vector3 GetTeacherPosition(Vector3 teacherPosition, int index, int count)
        {
            if (count <= 1)
            {
                return teacherPosition;
            }

            float offset = (index - (count - 1) * 0.5f) * TeacherSpacing;
            return teacherPosition + new Vector3(offset, 0f, 0f);
        }
    }
}
