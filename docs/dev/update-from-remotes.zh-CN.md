# 拉取远端、合并并更新本地 Unity 项目

适用于此仓库保留 request-safety 定制的本地开发环境。流程是：获取远端引用 → 隔离合并与验证 → 更新主工作区 → 同步各 Unity 项目的 DLL 和服务端 → 逐项目验收。

本文只描述操作，不代表执行了更新。当前实现核对日期：2026-10-19。版本号从仓库配置读取，后续升级时不要直接套用历史版本号或历史 PID。

## 1. 准备与保存现场

使用 PowerShell 7.4+，准备 Git、支持 `fetch` 的 Node.js、Python、.NET 8/9 SDK，以及目标项目对应的 Unity Editor。当前配套服务端还需要 .NET 9 Runtime。

以下代码块按顺序在同一个 PowerShell 会话中执行。任一步报错即停止；解决原因后再继续。

```powershell
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true
$repoRoot = 'F:\_Code\Unity-MCP'
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$backupBranch = "codex/before-update-$stamp"
$mergeBranch = "codex/update-remotes-$stamp"
$buildRoot = Join-Path $repoRoot '.local/request-safety'
$recordRoot = Join-Path $buildRoot "upgrades/$stamp"
$mergePath = Join-Path $buildRoot "worktrees/update-$stamp"
Set-Location -LiteralPath $repoRoot

git branch --show-current
git status --short
git remote -v
```

先人工确认当前分支为待更新的 `main`。工作区有修改时，先审阅并提交需要保留的内容，或自行保存到 stash；不要自动丢弃改动。本文后续以干净的 `main` 为前提。

```powershell
if ((git branch --show-current) -ne 'main') { throw '请先切换到待更新的 main。' }
if (git status --porcelain) { throw '请先保存工作区修改。' }
git branch $backupBranch HEAD
New-Item -ItemType Directory -Force -Path $recordRoot | Out-Null
git rev-parse HEAD | Set-Content -LiteralPath "$recordRoot/before-head.txt"
git remote -v | Set-Content -LiteralPath "$recordRoot/remotes.txt"
Copy-Item -LiteralPath 'commands/request-safety/sources.json' -Destination $recordRoot
```

更新前记录每个目标 Unity 项目的包引用方式、Editor 版本、服务端路径、端口、传输方式及认证配置。项目中有未决的长任务时，先查询结果并保留 `Library/mcp-operations`。这些本地记录可能含认证信息，不要提交到 Git。

## 2. 获取远端，并比较工作区实际差异

此仓库的两个远端用途如下；执行前以 `git remote -v` 的实际结果为准。

| 远端 | 用途 | 当前地址 |
| --- | --- | --- |
| `origin` | 自己的 fork，检查其他设备或协作者的更新 | `https://github.com/Misaka16608/Unity-MCP.git` |
| `upstream` | 官方仓库，获取上游更新 | `https://github.com/IvanMurzak/Unity-MCP.git` |

```powershell
git fetch origin --prune --tags
git fetch upstream --prune --tags
$originCommit = git rev-parse origin/main
$upstreamCommit = git rev-parse upstream/main
@("origin/main=$originCommit", "upstream/main=$upstreamCommit") |
    Set-Content -LiteralPath "$recordRoot/remote-heads.txt"

# 左侧为本地独有提交数，右侧为远端独有提交数。
git rev-list --left-right --count HEAD...origin/main
git log --oneline --left-right HEAD...origin/main

# 直接比较当前工作区与远端快照；前一步已要求工作区干净。
git diff --stat origin/main
git diff origin/main -- commands/request-safety Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp
git log --oneline HEAD..upstream/main
git diff --stat HEAD upstream/main
```

不要仅依据跟踪分支的 ahead/behind 判断内容，也不要用 `git pull` 一步完成获取和合并。网络失败时停止，恢复正常代理或网络后重试；不要关闭 TLS 证书验证，也不要把旧引用当作最新远端。

## 3. 在隔离工作树中合并

隔离工作树不会复制主工作区的 `.local` 构建产物，也不会改动业务 Unity 项目。这里将它放在已忽略的 request-safety 目录下。

```powershell
New-Item -ItemType Directory -Force -Path (Split-Path $mergePath -Parent) | Out-Null
git worktree add -b $mergeBranch $mergePath HEAD
Set-Location -LiteralPath $mergePath
```

先检查 `origin/main` 是否包含本地尚未接收的提交：

```powershell
git rev-list --count "HEAD..$originCommit"
```

结果大于 0 时，先执行以下合并；结果为 0 则跳过。使用前面记录的 SHA，保证本次审阅和合并针对同一个远端快照。

```powershell
git merge --no-ff --no-commit $originCommit
git status --short
# 审阅并解决冲突后，逐个 git add 已确认的文件。
git diff --cached --stat
git commit -m 'Merge origin/main before upstream update'
```

再检查并合并官方更新。结果为 0 时，同样跳过本次 merge 和 commit。

```powershell
git rev-list --count "HEAD..$upstreamCommit"
```

仅在上述结果大于 0 时执行：

```powershell
git merge --no-ff --no-commit $upstreamCommit
git status --short
# 有冲突时先处理，不能直接继续提交。
git diff --name-only --diff-filter=U
git diff --cached --stat
git commit -m 'Merge upstream/main and preserve local request safety'
```

发生冲突时，PowerShell 会在 merge 报错处停止。解决后用 `git add <文件>` 标记，确认无未解决冲突再提交。放弃尚未提交的这次合并可执行 `git merge --abort`；先前已完成的合并仍保留。

重点按以下原则处理：

| 冲突位置 | 处理原则 |
| --- | --- |
| `MainThread.Editor.cs` / `MainThread.Player.cs` | 保留上游调度修复，接入本地 `QueuedExecution`、取消和截止时间检查；避免恢复旧的后台线程修改 Editor update 委托方式 |
| NuGet 配置、DLL、依赖门禁和 asmdef | 按一套版本统一更新，不要对全部二进制文件直接选 ours/theirs |
| `commands/request-safety/` | 核对共享框架与服务端的源码基线，迁移补丁后再构建 |
| RQL 与预制体读取 | 保留上游接口；若继续保留预制体工具，要验证嵌套子对象与字段序列化 |
| Unity 2021 兼容 | 对仍在使用的旧项目保留版本条件分支，并实际验证 |
| `.meta`、技能、chain fixtures | `.meta` 由 Unity 生成；技能按生成流程更新；chain fixtures 不手工改写 |

不要因为某一功能已被上游部分修复，就整块删除本地安全链路；功能精简应另行评估。

## 4. 对齐配套依赖并验证隔离结果

先核对这些配置，而不是只看插件的 `package.json`：

| 文件 | 核对内容 |
| --- | --- |
| `commands/request-safety/sources.json` | Common/plugin 包版本、API 版本、配套服务端版本、两个依赖仓库的源码 SHA 和文件校验值 |
| `commands/request-safety/patches/*.patch` | 本地安全补丁是否适用于新的官方源码 |
| `Editor/DependencyResolver/NuGetConfig.cs` | 包版本、依赖门禁 generation |
| `.github/nuget-gate.lock`、受门禁控制的 asmdef | 与实际依赖及 generation 一致 |
| `Editor/Scripts/McpServerManager.cs` | 官方服务端下载 pin；本地配套版本通过路径覆盖安装 |

上述 `Editor/` 路径均位于 `Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/`。

若上游升级了共享框架或相关接口，需要先把 request-safety 补丁迁移到新的固定源码提交：审阅代码差异，更新补丁与清单中的 base/patched SHA256，再通过 `prepare.ps1` 校验。不能只改版本字符串，不能跳过补丁或校验来获得构建成功。若重用构建缓存，源码基线变化时还需保留并移走旧 `deps/` 和两个源码 ZIP；下载脚本会复用已存在的 ZIP。

本地定制逻辑改变时，为包和服务端使用新的定制版本；协议改变时同步 API 版本。已安装的同版本服务端内容不同会被安装脚本拒绝覆盖。NuGet pins 改变时，更新 generation 和对应 asmdef，再生成门禁锁：

```powershell
# 仅在有意完成 pins/generation 配套调整后执行 --write。
python .github/scripts/check_nuget_gate.py --write
python .github/scripts/check_nuget_gate.py
```

构建配套依赖、运行测试、生成本地 Windows 服务端，并同步工具项目的两个定制 DLL：

```powershell
pwsh -NoProfile -File commands/request-safety/build.ps1 -SeedToolProject
git diff --check
git status --short
```

`build.ps1` 会校验补丁、打包、运行 RequestExecution、选定服务端回归和 EngineFree 测试、检查依赖门禁，再执行本地 `dotnet publish`。这里的 publish 不会上传或发布版本。本次需要更新服务端，不使用 `-SkipPublish`。

还需完成以下验证，才能进入主工作区更新：

- 用目标 Unity 版本编译工具项目；官方 pins 改变时，先由隔离工具项目的依赖解析器恢复其他官方依赖，确认 DLL 与 `.nuget-installed.json` 一致。
- 检查 `Unity-Tests/*/Assets/Plugins/NuGet` 中已跟踪的 DLL/清单是否需要同步。现有 repair 脚本可对已有 NuGet 安装清单的测试项目逐个计划和应用；`-SeedToolProject` 本身仅更新工具项目。
- 按改动范围运行 Unity EditMode/PlayMode 测试。`compile-unity-sources.ps1` 依赖实际生成的 Editor.Tests 工程，只检查选定源文件，不代表完整工程或 Player 构建通过。
- 调度或传输改动应验证并发回调不丢失、不重复，排队取消或过期后不进入写操作；保留预制体工具时，用真实资产验证只读结果。
- 需要隔离运行主线程 EditMode 测试时，可使用 `unity-tests.ps1 -UnityExe '<实际 Unity.exe 路径>'`。缺失测试结果、许可证失败和启动保护拦截都不算通过。

记录本次测试数量、结果、Unity 版本及未覆盖范围，不套用历史的 196 项测试或 16,000 回调结果。构建与测试记录位于该隔离工作树的 `.local/request-safety/results/`。

审阅后逐项暂存实际需要的源码、补丁、DLL、清单和配置更新，提交为集成分支的后续修复提交。要求 `git status --porcelain` 为空；不要提交 `.local`、业务项目文件或认证配置。

## 5. 更新主工作区并转入已验证产物

先保存场景和资源，关闭所有直接通过 `file:` 引用主工作区包源码的 Unity Editor，以及需要同步 DLL 的目标 Editor，避免切换期间导入一半更新。

```powershell
Set-Location -LiteralPath $repoRoot
if ((git branch --show-current) -ne 'main') { throw '当前不是 main。' }
if (git status --porcelain) { throw '主工作区出现新修改，请先处理。' }
if (git -C $mergePath status --porcelain) { throw '集成工作树尚未提交完成。' }
git merge --ff-only $mergeBranch
python .github/scripts/check_nuget_gate.py
```

`--ff-only` 失败说明验证期间主分支又有新提交。把这些提交纳入集成分支并验证后再试，不要强制重置主分支。

将隔离工作树验证过的 `feed`、`publish` 和测试结果复制到主工作区的构建目录；旧内容先备份。这样同步脚本使用的产物与刚验证的代码一致。

```powershell
$integrationBuild = Join-Path $mergePath '.local/request-safety'
foreach ($name in @('feed', 'publish', 'results')) {
    $sourcePath = Join-Path $integrationBuild $name
    if (!(Test-Path -LiteralPath $sourcePath -PathType Container)) {
        throw "缺少已验证产物：$sourcePath"
    }
}
foreach ($name in @('feed', 'publish', 'results')) {
    $targetPath = Join-Path $buildRoot $name
    if (Test-Path -LiteralPath $targetPath) {
        Move-Item -LiteralPath $targetPath -Destination (Join-Path $recordRoot "previous-$name")
    }
    Copy-Item -LiteralPath (Join-Path $integrationBuild $name) -Destination $targetPath -Recurse
}
git rev-parse HEAD | Set-Content -LiteralPath "$recordRoot/after-head.txt"
```

此代码块每次更新只执行一次；中途失败时先检查已复制内容与备份，再继续，避免重复移动备份。旧 `deps/` 和源码 ZIP 未被此步骤更新；以后在主工作区重新构建前，若 `sources.json` 已变化，按第 4 节处理旧源码缓存。

## 6. 同步每个 Unity 项目的 DLL 和服务端

**更新 Git 仓库不会自动替换每个 Unity 项目的 `Assets/Plugins/NuGet`，也不会替换已运行的服务端。**

先核对各项目的 `Packages/manifest.json`：`file:` 引用主工作区的项目随主工作区获取源码；Git URL、Registry 或内嵌副本需要另行更新到本次已验证的包来源。现有两个修复脚本不修改包引用，也不负责全新安装；项目缺失 NuGet 安装清单时，应先通过正常依赖安装流程初始化。

应用前，为每个项目的完整 `Assets/Plugins/NuGet`（含现有 `.meta`）、包引用及相关 ProjectSettings 保存 Git 快照或文件备份，另行保留 `.env` 和现有服务端启动配置。两个脚本的自动备份只覆盖它们实际修改的文件，后续 Unity 依赖解析器升级的其他依赖需要靠这份项目快照恢复。

以下是本机已知项目，路径和端口仅供核对，执行前删去不再使用的项目或补上新项目：

| 项目路径 | 历史端口 |
| --- | --- |
| `F:/JSLM005` | 21000 |
| `D:/YYBS_YB/yybs_yuanban_0.1zhe` | 21500 |
| `F:/YYBS/Android/yybs_lz/int_UseMCP` | 25631 |
| `F:/YYBS/Android/yybs_lz/patch_UseMCP` | 25630 |

先计划 DLL 更新，再应用；DLL 应用完成后才能计划配套服务端安装，因为后者会检查项目内 DLL 版本。

```powershell
Set-Location -LiteralPath $repoRoot
$projectOptions = @(
    '--project', 'F:/JSLM005',
    '--project', 'D:/YYBS_YB/yybs_yuanban_0.1zhe',
    '--project', 'F:/YYBS/Android/yybs_lz/int_UseMCP',
    '--project', 'F:/YYBS/Android/yybs_lz/patch_UseMCP'
)
python -X utf8 commands/request-safety/repair-project-dependencies.py @projectOptions
# 审阅输出后应用；保留输出的 Backup 路径。
python -X utf8 commands/request-safety/repair-project-dependencies.py @projectOptions --apply

python -X utf8 commands/request-safety/configure-project-server.py @projectOptions
# 审阅输出后应用；保留输出的 Backup 路径。
python -X utf8 commands/request-safety/configure-project-server.py @projectOptions --apply
```

- DLL 脚本同步 Common、McpPlugin、ReflectorNet、RQL 及安装清单，保留已有 `.meta` 导入设置和无关依赖；其他新增或升级的官方依赖仍由依赖解析器处理。
- 服务端脚本安装 `sources.json` 指定的配套版本到项目 `Library/mcp-server-paired/`，缓存两个本地 NuGet 包，并设置项目根目录 `.env` 的 `UNITY_MCP_SERVER_PATH`。
- 两个脚本都不停止或启动进程。已有服务端安装目录内容与同版本构建不同时会拒绝覆盖；先保留旧安装，再使用新的定制版本，不要删除校验。

重新打开 Unity，等待资源导入、依赖解析和脚本编译。通过插件窗口停止旧服务端、启动配套服务端并重新连接。若旧进程仍占用端口，先核对其 PID、可执行路径及项目参数，只处理已确认属于该项目的服务端；不要按进程名批量终止 Unity 或所有 MCP 服务端。

沿用原有端口、传输及认证配置。曾发生 API 握手失败的项目应显式断开再连接，因为失败后可能已关闭自动重连。

## 7. 逐项目验收并记录

每个目标项目都要独立验收，端口监听或服务端进程存在不能替代 Editor 连通验证。对已确认仅连接该项目的本地 HTTP 服务，可执行：

```powershell
$baseUrl = 'http://127.0.0.1:21000' # 改成当前项目实际地址。
$headers = @{} # 有认证时填入现有授权头，不要关闭认证来通过检查。
$tools = Invoke-RestMethod -Uri "$baseUrl/api/tools" -Headers $headers
$ping = Invoke-RestMethod -Method Post -Uri "$baseUrl/api/system-tools/ping" `
    -Headers $headers -ContentType 'application/json' -Body '{}'
$queryBody = @{ query = 'scenes | first 1 | select name path isDirty' } | ConvertTo-Json
$query = Invoke-RestMethod -Method Post -Uri "$baseUrl/api/tools/rql-query" `
    -Headers $headers -ContentType 'application/json' -Body $queryBody
$ping | ConvertTo-Json -Depth 12
$query | ConvertTo-Json -Depth 12
```

检查 ping 的工具结果为 `pong`，RQL 结果成功且属于目标项目；空场景列表可以是正常结果。使用共享多实例服务时，改用该项目的 `/p/<项目 pin>/api/...` 路由；stdio 环境通过实际 MCP 客户端执行相同探测。

验收完成条件：

- Unity Console 无本次引入的编译/程序集加载错误，插件与服务端握手成功。
- 工具列表符合本次代码和启用配置，ping、只读 RQL 查询成功；不要硬编码历史工具数量。
- 调度、安全链路或预制体工具有变更时，对应检查也通过。
- 旧 Unity 项目的第三方依赖仍可用；JSLM005 保留已有 Editor 专用 Protobuf/Unsafe 兼容配置，受影响时做实际序列化往返验证。
- 对所有项目重新运行两个修复脚本的只读计划，确认 DLL 无待更新项、服务端无需安装且 `.env` 无需更新。

在更新记录中保存合并前后 SHA、远端 SHA、依赖/API/服务端版本、测试结果、项目路径与实际端口，以及两个脚本给出的备份目录。记录未验证的 Player 构建或业务玩法范围。

## 8. 失败处理与回滚

| 现象 | 优先检查 |
| --- | --- |
| `Common.Execution` / `ToolExecutionContext` 缺失 | Unity 项目仍在使用旧 Common/plugin DLL；检查实际安装与导入设置，再做 DLL 同步 |
| `API VERSION MISMATCH` | 服务端路径覆盖、实际运行的旧进程，以及配套版本是否一致 |
| 补丁或源码 SHA256 校验失败 | 固定源码、ZIP 缓存和补丁基线；保留现场后迁移，不跳过校验 |
| 依赖门禁失败 | NuGet pins、generation、asmdef 和 lock 是否整体一致 |
| Unsafe/Protobuf 加载错误 | 项目第三方程序集版本和平台导入设置，不通过关闭全局版本校验掩盖问题 |

主工作区尚未更新时，业务项目不受隔离合并影响；修复集成分支即可。未完成的 Git 合并可 `git merge --abort`。

主工作区和项目已更新后，需要把源码、DLL、服务端及 `.env` 一并恢复。先保存并关闭目标 Editor，停止对应服务端。若更新后没有产生需要保留的新修改，可在干净工作区创建回滚分支，用更新前快照生成一个新的恢复提交：

```powershell
Set-Location -LiteralPath $repoRoot
if (git status --porcelain) { throw '先保存更新后产生的修改。' }
$rollbackBranch = "codex/rollback-update-$stamp"
git switch -c $rollbackBranch
git restore --source=$backupBranch --staged --worktree -- .
git diff --cached --stat
# 审阅恢复范围后提交；此操作保留已有 Git 历史。
git commit -m 'Restore pre-update repository snapshot'
git switch main
git merge --ff-only $rollbackBranch
```

再依据 `project-repair/<批次>/changes.json` 恢复原 DLL/安装清单，依据 `server-repair/<批次>/changes.json` 恢复原 `.env`，并使用第 5 节保留的旧构建产物或原版本安装目录。依赖解析器额外变更的 DLL、包引用和设置，从第 6 节的项目快照恢复。备份脚本没有提供一键回滚：覆盖前核对文件当前哈希，避免覆盖之后的独立修改；新增且原本不存在的文件要按记录单独处理。

重新启动旧配套服务端、打开 Editor 并重复第 7 节验收。不要删除业务项目的整个 `Library` 或未决任务账本。更新后已有其他开发提交时，审阅并选择性回退本次变更，不直接恢复整树快照。

## 9. 完成与清理

确认所有目标项目验收完成后，保留备份与测试记录至稳定运行。需要同步 fork 时，审阅后再执行 `git push origin main`；本流程不自动推送，不向 `upstream` 推送，也不触发发布。定制 NuGet 包和服务端尚未成为正式配套发布时，不直接执行 release 工作流。

清理隔离工作树前，确认代码已提交，所需构建/测试证据已转存，并且项目未引用隔离路径。检查 `git worktree list` 后，用 `git worktree remove <本次隔离路径>` 清理；不要直接递归删除正在使用的工作树或备份。

实现细节与历史验证见：[request-safety 说明](../../commands/request-safety/README.md)、[验证记录](../../commands/request-safety/VALIDATION.md)、[依赖清单](../../commands/request-safety/sources.json)。
