# GitHub Release 统一托管定制依赖

本分支把定制 NuGet 包和匹配的服务端统一托管在公开仓库
[Misaka16608/Unity-MCP](https://github.com/Misaka16608/Unity-MCP/releases)。
业务 Unity 项目更新这个 fork 的 UPM 包后会自动下载依赖，不需要向每个项目复制服务端目录。
每个项目仍保留自己的进程、端口、认证和下载缓存；“统一托管”指发布来源统一。

## 当前版本与附件

| 组件 | 版本 |
| --- | --- |
| Unity UPM / CLI | `1.2.0-request-safety.4` |
| McpPlugin / McpPlugin.Common | `8.9.0-request-safety.4` |
| GameDev MCP Server | `9.2.12-request-safety.4` |
| 请求安全 API | `2.0.0-request-safety.3` |
| 统一依赖 Release 标签 | `v9.2.12-request-safety.4` |

Release 包含两个 `.nupkg`、7 个 `gamedev-mcp-server-<rid>.zip`、`sources.json`、`SHA256SUMS`。
RID 为 `win-x64`、`win-x86`、`win-arm64`、`linux-x64`、`linux-arm64`、`osx-x64`、`osx-arm64`。
服务端自带 .NET 运行时。NuGet 包名保持原名，仅由本 fork 的下载器获取，未向 NuGet.org 发布。
普通第三方 NuGet 依赖继续来自 NuGet.org。两个定制包和服务端都在安装/解压前核对 SHA256。
离线 NuGet 缓存需同时保存对应版本的 SHA256 清单。

这些附件是本 fork 的未签名构建，未包含上游 Windows 签名/macOS 公证。
此流程没有发布 Docker 镜像；现有 Docker 示例指向上游镜像，不适用于定制协议版本，使用下载的服务端程序。

## 合并上游与构建

1. 按[远端更新流程](update-from-remotes.zh-CN.md)先检查工作区、创建备份分支、fetch、合并、解决冲突。
2. 更新 `commands/request-safety/sources.json` 中的官方源码提交、定制版本及补丁前后哈希。
   同步更新补丁文件。不要直接复制旧 DLL 配合新版源码。
3. 同步 `NuGetConfig.cs`、EngineFree 的包版本、`HostedDependencyRelease` 的两个版本、
   `McpServerManager.ServerVersion` 和 CLI 的版本/仓库常量。
   NuGet pin 变化时按 `check_nuget_gate.py --help` 提升并重新确认 generation gate。
4. 用 `commands/bump-version.ps1` 同步 UPM、运行时代码、安装器和 CLI 版本；更新发布说明。
5. 提交代码，再构建附件，以便 `sources.json` 记录的 `unityCommit` 对应完整的可复现源码。

```powershell
python commands/request-safety/release_assets.py check-config
pwsh -NoProfile -File commands/request-safety/build.ps1 -ReleaseAssets -SkipPublish
cd cli
npm ci
npm test
cd ..
python commands/request-safety/release_assets.py validate
```

脚本从固定的官方源码提交应用补丁，构建两个包，运行请求安全、服务端回归、EngineFree 测试，
然后交叉编译 7 个自包含服务端。产物位于 `.local/request-safety/release-assets/v<serverVersion>/`。
已存在的附件目录不会覆盖；重新构建前把旧目录移到单独的备份目录。
跨平台产物通过打包完整性检查；实际启动验证需在对应 OS/架构执行。

## 发布

先推送已验证的源码提交到本 fork 的 `main`。推荐使用手动工作流：

```powershell
gh workflow run release-dependencies.yml -R Misaka16608/Unity-MCP --ref main -f dry_run=true
# 在 GitHub Actions 确认测试和附件后再正式发布。
gh workflow run release-dependencies.yml -R Misaka16608/Unity-MCP --ref main -f dry_run=false
```

默认只构建。正式运行先上传 draft，再公开完整 Release；不会调用 npm publish、NuGet.org、Docker 或 Discord。
同名 Release 已存在则失败；不要覆盖已发布附件，任何变更都递增定制版本后发布新标签。
如果上传中断，先检查并处理未公开 draft，避免发布不完整的附件。

也可以从本机构建结果发布，标签必须指向已经推送的源码提交：

```powershell
$tag = 'v9.2.12-request-safety.4'
$commit = git rev-parse HEAD
$assets = Get-ChildItem ".local/request-safety/release-assets/$tag" -File
gh release create $tag @($assets.FullName) -R Misaka16608/Unity-MCP --target $commit --draft --prerelease --title "Paired dependencies $tag" --notes-file docs/dev/dependency-release-notes.md
# 确认所有 11 个附件上传完整后公开。
gh release edit $tag -R Misaka16608/Unity-MCP --draft=false
python commands/request-safety/release_assets.py verify-remote
```

依赖 Release 必须先存在，再发布引用它的 Unity 插件。上游 `release.yml` 涉及官方 npm/签名等资源，
不要把它当成本 fork 的依赖发布入口；插件也可以直接通过本 fork 的 Git URL/固定提交安装。

## 已有 Unity 项目迁移

先让项目使用本 fork 的新版 UPM 源码，并等 NuGet 恢复到上表版本。对于旧流程设置了
`UNITY_MCP_SERVER_PATH` 的项目，下面命令先检查公开 Release 和项目依赖版本，再计划/备份移除该 override：

```powershell
python commands/request-safety/use-hosted-release.py --project F:/YourUnityProject
python commands/request-safety/use-hosted-release.py --project F:/YourUnityProject --apply
```

可以重复指定 `--project`。环境变量中另设的同名 override 也应移除。
关闭旧服务端并重启 Unity 后，由编辑器下载该平台的匹配服务端。项目端口和认证保持原有配置。
保留 `.env` 备份用于回退；旧 `Library/mcp-server-paired` 目录不会被删除。
新项目没有 override 时不需运行迁移脚本。

本地开发仍可用 `build.ps1 -SeedToolProject` 和原有 `configure-project-server.py`，但后者会重新启用本地路径。
它不再是常规分发步骤。干净机器上的 dotnet 测试可先运行
`release_assets.py fetch-packages`，再用 `-p:RestoreAdditionalProjectSources=<仓库>/.local/request-safety/feed` 恢复。
