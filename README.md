# Worst Class Ever — 玩家人数 / 老师人数上限扩展

适用于 **Worst Class Ever Demo** 的 BepInEx 5 插件。当前版本 **1.1.0**。
运行时使用 Harmony 修改房间容量与开课身份分配，不替换游戏本体文件。

| 设置 | 范围 | 默认值 |
| --- | --- | --- |
| 房间人数上限（含主机） | 2–16 | 10 |
| 老师人数上限 | 1–8 | 2 |

**本次为实验性版本**：
**安装方法**
***将本压缩包直接解压到游戏根目录（即BepInEx和Worst Class Ever.exe在同一级）***

## 官方下载与文件校验

唯一官方发布地址：[Tomo4860/BigClassMod Releases](https://github.com/Tomo4860/BigClassMod/releases)。
下载 `BigClass-1.1.0.zip` 和同一版本的 `SHA256SUMS.txt`，安装前核对 SHA-256。

在下载目录打开 PowerShell：

```powershell
Get-FileHash -Algorithm SHA256 -LiteralPath '.\BigClass-1.1.0.zip'
```

结果应与官方 Release 页面和 `SHA256SUMS.txt` 中对应文件的 64 位值完全一致（忽略大小写）。
解压后也可校验 DLL：

```powershell
Get-FileHash -Algorithm SHA256 -LiteralPath '.\BepInEx\plugins\BigClassMod\BigClassMod.dll'
```

DLL 的预期值见 `SHA256SUMS.txt` 的对应条目。如果不一致，请勿安装或运行。
应从上述官方页面获取预期哈希；第三方同时篡改文件和校验文本时，仅比较两者没有意义。
哈希校验只确认文件一致性，不等同于数字签名、安全审计或无恶意代码保证。

## 安装

1. 关闭游戏。适配目标为 Windows x64、Unity Mono 版 Worst Class Ever Demo。
2. 从 [BepInEx 官方发布页](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.3)
   安装 **BepInEx 5.4.23.3 x64（Mono）** 到游戏目录，启动一次后退出。
3. 校验并解压 `BigClass-1.1.0.zip`，将其中的 `BepInEx` 文件夹合并到游戏根目录。
4. 确认文件位于 `BepInEx\plugins\BigClassMod\BigClassMod.dll`。
5. 启动游戏，进入 **设置 → 通用**，在底部调整“房间人数上限”“老师人数上限”。

发布包只包含本插件、文档和许可；不包含游戏文件或 BepInEx 本体。

## 配置

首次加载后生成 `BepInEx\config\rm.worstclassever.bigclass.cfg`：

```ini
[Room]
MaxPlayers = 10

[Class]
MaxTeachers = 2
```

房间人数在下一次创建房间时生效；老师人数在下一次按铃开课时生效。
开课时最多保留配置数量的已选老师；没有人选老师时，仍随机指定一名。
超过老师上限的玩家会转为学生，不是解锁任意角色或无限人数。

## 兼容性与验证范围

- 原项目记录已验证设置项插入、输入编辑、六个补丁挂载和游戏启动；本次发布整理未重新进行游戏内实测。
- 多账号超限进房、多老师开课和未安装插件客户端的兼容性仍待验证。
- UOS 中继服务是否接受超过原版上限的房间，需要实测，不能由本地配置保证。
- 超过 8 人时出生座位向后延伸，可能出现重叠或场景布局问题。
- 多老师会改变游戏平衡，胜利分数仍沿用游戏原有配置。
- 游戏更新可能改变类或方法，导致补丁失效；不保证正式版或其他版本兼容。

排查时查看 `BepInEx\LogOutput.log` 中的 `BigClass`、`自检通过`、`自检失败`。
正式多人使用前，建议先验证第 7 名玩家加入及两名老师同时开课。

## 卸载

关闭游戏后，仅移除 `BepInEx\plugins\BigClassMod` 文件夹。
如需清除配置，再移除 `BepInEx\config\rm.worstclassever.bigclass.cfg`。
保留其他插件共用的 BepInEx 和加载器文件。

## 从源码构建

安装 .NET SDK（本次使用 8.0.419），并准备本机游戏与 BepInEx 5。
在源码根目录执行，将路径换成自己的游戏目录：

```powershell
dotnet build -c Release -p:GameDir="D:\Games\Worst Class Ever Demo"
```

也可设置环境变量 `WCE_GAME_DIR` 后执行 `dotnet build -c Release`。
项目从本机游戏的 `Worst Class Ever_Data\Managed` 和 `BepInEx\core` 引用依赖。
构建产物为 `bin\Release\BigClassMod.dll`。仓库不提供游戏 DLL 或反编译的游戏源码。

## 许可

Copyright (c) 2026 Tomo4860. **保留所有权利，禁止未经许可再分发。**
允许个人使用、源码研究、本地编译和本地修改；禁止转载、镜像、重新打包、
发布修改版或第三方安装包。可以分享官方发布链接。完整条款见 [LICENSE](LICENSE)。
GitHub 服务条款允许的站内查看与 Fork 不受本许可限制。
本项目是源码可见项目，不采用 MIT 等允许自由再分发的开源许可。

## 版本记录

- **1.1.0**：设置界面增加房间人数和老师人数输入项；本次发布统一程序集版本并使构建路径可配置。
- **1.0.0**：通过配置文件调整房间人数与老师人数上限。
