using Gallop;
using Gallop.Endpoints;
using System.Text.Json;
using UmamusumeResponseAnalyzer.LiveDisplay;
using UmamusumeResponseAnalyzer.Plugin;

namespace TeamStadiumOpponentListResponseAnalyzer
{
    public class TeamStadiumOpponentListResponseAnalyzer : IPlugin
    {
        public string Name => "TeamStadiumOpponentListResponseAnalyzer";
        ILiveDisplayOutput? liveDisplay;
        LiveDisplayWorkspace? workspace;

        public void Initialize(IPluginContext context)
        {
            liveDisplay = context.LiveDisplay;
        }

        public void Dispose()
        {
            var output = liveDisplay;
            var target = workspace;
            liveDisplay = null;
            workspace = null;
            if (output is not null && target is not null)
                output.RemoveWorkspace(target);
        }

        [ResponseAnalyzer<GameApi.TeamStadium.OpponentList>]
        public ValueTask AnalyzeOpponentList(TeamStadiumOpponentListResponse response)
        {
            var rows = response.data.opponent_info_array.Select(opponent =>
            {
                var user = opponent.user_info;
                return $"#{opponent.strength}: {user.name} 育成数 {user.single_mode_play_count}";
            });
            LiveDisplay.SetPanel(
                Workspace,
                "opponents",
                "对手列表",
                LiveDisplayContent.Text(string.Join(Environment.NewLine, rows)));
            return ValueTask.CompletedTask;
        }

        [ResponseAnalyzer<GameApi.TeamStadium.DecideFrameOrder>]
        public ValueTask AnalyzeDecideFrameOrder(TeamStadiumDecideFrameOrderResponse response)
        {
            LiveDisplay.SetPanel(Workspace, "current-opponent", "当前对手", AnalyzeOpponent(response.data.opponent_info_copy));
            return ValueTask.CompletedTask;
        }

        [ResponseAnalyzer<GameApi.TeamStadium.Start>]
        public ValueTask AnalyzeStart(TeamStadiumStartResponse response)
        {
            LiveDisplay.SetPanel(Workspace, "current-opponent", "当前对手", AnalyzeOpponent(response.data.opponent_info_copy));
            return ValueTask.CompletedTask;
        }

        static LiveDisplayContent AnalyzeOpponent(TeamStadiumOpponent opponent)
        {
            var team = opponent.team_data_array;
            var trained = opponent.trained_chara_array;
            var name = opponent.user_info.name;
            var distStats = new Dictionary<string, int>();
            var groundStats = new Dictionary<string, int>();
            var styleStats = new Dictionary<string, int>();

            var teamData = team.Where(x => x.trained_chara_id != 0).GroupBy(x => x.distance_type).ToDictionary(x => x.Key, x => x.ToList());
            foreach (var group in teamData)
            {
                foreach (var teamEntry in group.Value)
                {
                    var trainedChara = trained.FirstOrDefault(x => x.trained_chara_id == teamEntry.trained_chara_id);
                    if (trainedChara == null) continue;

                    var groundProper = teamEntry.distance_type switch
                    {
                        5 => GetProper(trainedChara.proper_ground_dirt),
                        _ => GetProper(trainedChara.proper_ground_turf)
                    };
                    var distProper = teamEntry.distance_type switch
                    {
                        1 => GetProper(trainedChara.proper_distance_short),
                        2 => GetProper(trainedChara.proper_distance_mile),
                        3 => GetProper(trainedChara.proper_distance_middle),
                        4 => GetProper(trainedChara.proper_distance_long),
                        5 => GetProper(trainedChara.proper_distance_mile),
                        _ => throw new NotImplementedException($"未知 distance_type: {teamEntry.distance_type}")
                    };
                    var styleProper = teamEntry.running_style switch
                    {
                        1 => GetProper(trainedChara.proper_running_style_nige),
                        2 => GetProper(trainedChara.proper_running_style_senko),
                        3 => GetProper(trainedChara.proper_running_style_sashi),
                        4 => GetProper(trainedChara.proper_running_style_oikomi),
                        _ => throw new NotImplementedException($"未知 running_style: {teamEntry.running_style}")
                    };

                    Increment(distStats, distProper);
                    Increment(groundStats, groundProper);
                    Increment(styleStats, styleProper);
                }
            }

            return LiveDisplayContent.Text(string.Join(Environment.NewLine,
            [
                $"当前对手: {name}",
                $"距离适性: {JsonSerializer.Serialize(distStats)}",
                $"场地适性: {JsonSerializer.Serialize(groundStats)}",
                $"跑法适性: {JsonSerializer.Serialize(styleStats)}"
            ]));
        }

        static void Increment(Dictionary<string, int> stats, string key)
        {
            stats[key] = stats.GetValueOrDefault(key) + 1;
        }

        ILiveDisplayOutput LiveDisplay => liveDisplay
            ?? throw new InvalidOperationException("TeamStadiumOpponentListResponseAnalyzer 尚未初始化 LiveDisplay。");

        LiveDisplayWorkspace Workspace => workspace
            ??= LiveDisplay.CreateWorkspace(Name);

        static string GetProper(int proper) => proper switch
        {
            1 => "G",
            2 => "F",
            3 => "E",
            4 => "D",
            5 => "C",
            6 => "B",
            7 => "A",
            8 => "S",
            _ => throw new NotImplementedException($"未知 proper: {proper}")
        };
    }
}
