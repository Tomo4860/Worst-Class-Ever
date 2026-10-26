using System;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace BigClassMod.Patches
{
    /// <summary>
    /// 在设置窗口的「通用」页插入两个数值输入项：房间人数上限、老师人数上限。
    ///
    /// 结构来自实际运行时抓取：
    ///   UI_Setting_p(Clone)
    ///     Container_Common (UI_CommonSettingContainer)
    ///       Scroll View / Viewport / SettingContainer  ← VerticalLayoutGroup，行高 80
    ///         SettingSlider (UI_SettingSlider)  ← 行模板：Title(TextEx) + SliderEx + InputFieldEx
    ///
    /// 做法是克隆一行现成的模板，保留它的标题排版、背景、输入框样式，去掉 UI_SettingSlider 组件
    /// （否则它会按 SettingManager 的设置类型回写数据），隐藏滑块，输入框直接写本模组的配置。
    /// </summary>
    [HarmonyPatch(typeof(UISettingView), "OnActive")]
    internal static class Patch_SettingsUI
    {
        private const string PlayerRowName = "BigClass_PlayerLimitRow";
        private const string TeacherRowName = "BigClass_TeacherLimitRow";

        [HarmonyPostfix]
        private static void Postfix(UISettingView __instance)
        {
            try
            {
                Inject(__instance);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError("[BigClass] 设置项注入失败：" + ex);
            }
        }

        private static void Inject(UISettingView view)
        {
            if (view.panelList == null || view.panelList.Count == 0 || view.panelList[0] == null)
            {
                return;
            }

            UI_SettingPanel common = view.panelList[0];
            UI_SettingSlider template = common.GetComponentInChildren<UI_SettingSlider>(true);
            if (template == null)
            {
                Plugin.Log.LogWarning("[BigClass] 找不到设置行模板 UI_SettingSlider");
                return;
            }

            Transform container = template.transform.parent;
            if (container == null)
            {
                return;
            }

            EnsureRow(container, template, PlayerRowName, "房间人数上限", Plugin.MaxPlayers, 2, 16);
            EnsureRow(container, template, TeacherRowName, "老师人数上限", Plugin.MaxTeachers, 1, 8);
        }

        private static void EnsureRow(Transform container, UI_SettingSlider template, string rowName,
            string label, ConfigEntry<int> entry, int min, int max)
        {
            Transform existing = container.Find(rowName);
            if (existing != null)
            {
                InputField existingInput = existing.GetComponentInChildren<InputField>(true);
                if (existingInput != null)
                {
                    existingInput.text = entry.Value.ToString();
                }

                Text existingTitle = existing.GetComponentInChildren<Text>(true);
                SetLabel(existingTitle, label);
                return;
            }

            GameObject row = UnityEngine.Object.Instantiate(template.gameObject, container);
            row.name = rowName;
            row.SetActive(true);

            UI_SettingSlider rowLogic = row.GetComponent<UI_SettingSlider>();
            Text title = rowLogic.title;
            InputField input = rowLogic.inputField;
            Slider slider = rowLogic.slider;

            // 去掉原组件的回写逻辑（它会按 SettingManager 的设置类型写脏数据），并隐藏滑块。
            UnityEngine.Object.Destroy(rowLogic);
            if (slider != null)
            {
                slider.gameObject.SetActive(false);
            }

            // Awake 里挂上的运行时监听一并清掉，换成写配置的监听。
            input.onEndEdit.RemoveAllListeners();
            input.contentType = InputField.ContentType.IntegerNumber;
            input.characterLimit = 2;
            input.text = entry.Value.ToString();

            if (title != null)
            {
                SetLabel(title, label);
            }

            input.onEndEdit.AddListener(raw => Commit(raw, entry, input, min, max));

            Plugin.Log.LogInfo($"[BigClass] 设置项已插入：{label}（当前 {entry.Value}，范围 {min}-{max}）");
        }

        private static void SetLabel(Text title, string label)
        {
            if (title == null)
            {
                return;
            }

            // 标题是 TextEx（带本地化 Key），不清掉 Key 的话 OnEnable 会把文本刷回模板行的原文。
            TextEx textEx = title as TextEx;
            if (textEx != null && !string.IsNullOrEmpty(textEx.Key))
            {
                textEx.SetKey(null);
            }

            title.text = label;
        }

        private static void Commit(string raw, ConfigEntry<int> entry, InputField field, int min, int max)
        {
            if (!int.TryParse(raw, out int value))
            {
                field.text = entry.Value.ToString();
                return;
            }

            value = Mathf.Clamp(value, min, max);
            if (entry.Value != value)
            {
                entry.Value = value;
                Plugin.Cfg.Save();
                Plugin.Log.LogInfo($"[BigClass] 设置更新：{entry.Definition.Key} = {value}，下次创建房间/开课时生效");
            }

            field.text = value.ToString();
        }
    }
}
