# TeamStadiumOpponentListResponseAnalyzer

显示团队竞技场对手信息的 URA 插件。

## 行为

- `GameApi.TeamStadium.OpponentList`：在“对手列表”面板中逐行显示 `strength`、用户名和育成次数。
- `GameApi.TeamStadium.DecideFrameOrder` 与 `GameApi.TeamStadium.Start`：在“当前对手”面板中显示用户名，以及距离、场地和跑法适性等级的计数。
- 适性统计只包含 `trained_chara_id != 0` 且能在 `trained_chara_array` 中找到对应记录的队伍成员。

插件没有配置文件，也不持久化显示内容。未知的 `distance_type`、`running_style` 或适性值会明确失败。

## 构建

仓库通过 `NuGet.Config` 恢复 `UmamusumeResponseAnalyzer` 编译期包。在仓库根执行：

```powershell
dotnet build .\TeamStadiumOpponentListResponseAnalyzer.csproj -c Release -m:1 -p:RuntimeIdentifier=win-x64 -p:SelfContained=false -p:PlatformTarget=AnyCPU -p:DeployUraPluginToLocalAppDataOnBuild=false
```
