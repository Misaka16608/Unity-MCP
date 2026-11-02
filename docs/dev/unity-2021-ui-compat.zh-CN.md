# Unity 2021 UI 兼容分支

分支：`codex/unity-2021-ui`，仓库：`Misaka16608/Unity-MCP`。

该分支从官方 `IvanMurzak/Unity-MCP` 的 `main` 建立，初始基线为
`475d025746755d130bfdb5274bc6e75ba9c83f24`（2026-11-02 获取）。后续继续合并官方更新，
仅维护 Unity 2021.3 所需的 UI 和 Editor API 兼容补丁。

## 依赖与安装

保留官方协议、NuGet 下载和服务端分发实现。初始基线的插件版本字段为 `1.6.0`，
McpPlugin 为 `8.16.0`，ReflectorNet 为 `5.5.1`，Rql 为 `0.3.0`，依赖门控为
`UNITY_MCP_DEPS_32`，服务端为官方 GameDev-MCP-Server `9.3.0`。
这是上游 main 加兼容补丁，不能与已发布的 OpenUPM `1.6.0` 内容视为相同。

本分支不包含旧 fork 的请求安全协议、自定义 NuGet、定制服务端及自定义工具。
仍使用旧 fork DLL 的项目，切换时也必须让依赖解析器更新成该分支固定的官方 DLL，
不能只替换插件源码。

通过 Unity Package Manager 的 Git URL 安装：

```text
https://github.com/Misaka16608/Unity-MCP.git?path=/Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp#codex/unity-2021-ui
```

项目还需保留官方安装要求的 OpenUPM registry 配置，让 `extensions.unity.playerprefsex`
依赖可解析。需要可重复安装时，将 URL 最后的分支名替换为本分支已验证的完整提交 SHA。
仅合并或推送该分支不会发布插件，也不会自动更新现有 Unity 项目。

## 兼容范围

| 项目 | Unity 2021.3 行为 | Unity 2022.3+ 行为 |
| --- | --- | --- |
| 主窗口超时输入 | 使用 `UnityEditor.UIElements.IntegerField` | 使用 `UnityEngine.UIElements.IntegerField` |
| UXML 数值控件 | 在公共占位节点处创建对应版本的原生数值控件，保留名称、样式和失焦校验 | 同左 |
| Scene View 工具栏 | 使用基础 Overlay 注册；不指定新版默认停靠位置或 collapsedIcon，可能需在 Overlays 菜单手动启用 AI | 保留上游停靠和图标配置 |
| 材质变体 | RQL 不注册引擎不支持的 `parent` / `isVariant`，相应测试按版本编译 | 保留上游支持 |
| Prefab 覆盖列表 | 不调用不可用的 `GetRemovedGameObjects`；仍列出其他支持的覆盖 | 保留上游移除对象列表 |
| 全局纹理 Mipmap 限制 | 相同 RQL 属性映射到 `QualitySettings.masterTextureLimit` | 映射到 `globalTextureMipmapLimit` |
| 已加载场景计数 | 遍历场景并检查 `isLoaded`，排除仅保留在层级中的未加载场景 | 保留上游 `SceneManager.loadedSceneCount` |
| 物理模拟模式 | 提供引擎支持的 `autoSimulation` 布尔设置，不暴露新版 `simulationMode` | 保留上游 `simulationMode` 枚举设置 |

降低 `package.json` 的最低 Unity 版本至 `2021.3`，让 UPM 可以安装此兼容分支。
这不表示上游或其全部扩展已承诺支持 Unity 2021；每次同步仍须重新检查 API 和依赖兼容性。

## 同步流程

先确认当前工作区没有未提交修改，再在本分支执行：

```powershell
git switch codex/unity-2021-ui
git fetch upstream main
git merge upstream/main
python .github/scripts/check_nuget_gate.py
git diff --check
```

解决冲突时优先保留上游行为，把 2021 差异限制在版本条件或等价的公开 API 中。
不要为兼容 UI 恢复旧 fork 的协议、DLL、工具或服务端地址。若上游新增只能在较新引擎使用的功能，
应明确其 2021 行为；不能为了编译成功悄悄返回伪造结果。

使用独立测试项目分别验证 Unity 2021.3 和 2022.3：先检查依赖解析及插件编译，再打开主窗口，
确认两个超时输入控件的数值、样式和失焦校验，检查 Scene View 工具栏注册。
涉及 RQL 改动时，还要执行对应材质、Prefab、质量设置和场景测试。
确认通过后再推送分支；业务项目升级另行安排。

相关 EditMode 回归可用下列筛选器运行（分别替换为两个版本的 Unity 可执行文件和隔离测试工程路径）：

```powershell
& $unity -batchmode -projectPath $testProject -runTests -testPlatform EditMode `
  -testFilter 'SceneViewToolbarOverlayTests;UnityEngine_Material_AdapterTests;RqlProjectSettingsTests;RqlEditorApi_ScenesTests;RqlPrefabInstanceTests' `
  -testResults $resultsXml -logFile $editorLog
```

测试工程的 `Packages/manifest.json` 需通过本地 `file:` 路径引用本分支包，设置
`"testables": ["com.ivanmurzak.unity.mcp"]`，并包含物理等标准内置模块。
使用 ASCII `productName`，以满足上游 `PagesAreReadable` 对原始文本和 JSON 转义文本直接比较的测试前提。
确保依赖解析完成后再运行；不要让测试与日常编辑共用一个工程。

## 本次验证

初始补丁已使用本机 Unity `2021.3.33f1` 和 `2022.3.62f3` 的程序集分别完成
DependencyResolver、Runtime、Editor 源码编译；官方 NuGet 门控检查通过。
两个独立 Unity 项目均完成实际导入、插件及内置 RQL 模块编译和窗口验证：
两个数值控件类型正确、显示配置值、保留样式，失焦将低于 1000 ms 的工具超时钳制并保存，
Scene View Overlay 的公开注册特性存在。验证没有连接或替换业务项目的 MCP 服务端。
上述五组 EditMode 回归结果：Unity 2021.3.33f1 共 52 项全部通过，Unity 2022.3.62f3
共 55 项全部通过；没有失败或跳过项。两版数量差异来自仅在 2022.3+ 编译的两个材质变体测试
和一个 Prefab 移除对象覆盖测试。

此处不宣称已完成全部扩展、PlayMode 或 Player 构建验证。
