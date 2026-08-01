using Gallop;
using Gallop.Endpoints;
using System.Text.Json;
using UmamusumeResponseAnalyzer.TerminalGui;
using UmamusumeResponseAnalyzer.Plugin;

namespace TeamStadiumOpponentListResponseAnalyzer
{
    public class TeamStadiumOpponentListResponseAnalyzer : IPlugin
    {
        const string OpponentsPanelKey = "opponents";
        const string CurrentOpponentPanelKey = "current-opponent";

        Workspace? workspace;
        bool hasPublishedOpponentsPanel;
        bool hasPublishedCurrentOpponentPanel;

        public void Initialize(IPluginContext context)
        {
            hasPublishedOpponentsPanel = false;
            hasPublishedCurrentOpponentPanel = false;
        }

        public void Dispose()
        {
            if (hasPublishedOpponentsPanel)
            {
                workspace!.RemovePanel(OpponentsPanelKey);
                hasPublishedOpponentsPanel = false;
            }
            if (hasPublishedCurrentOpponentPanel)
            {
                workspace!.RemovePanel(CurrentOpponentPanelKey);
                hasPublishedCurrentOpponentPanel = false;
            }
        }

        [ResponseAnalyzer<GameApi.TeamStadium.OpponentList>]
        public ValueTask AnalyzeOpponentList(TeamStadiumOpponentListResponse response)
        {
            var rows = response.data.opponent_info_array.Select(opponent =>
            {
                var user = opponent.user_info;
                return $"#{opponent.strength}: {user.name} 育成数 {user.single_mode_play_count}";
            });
            var workspace = this.workspace ??= Workspace.Create(nameof(TeamStadiumOpponentListResponseAnalyzer));
            workspace.SetPanel(
                OpponentsPanelKey,
                "对手列表",
                WorkspaceContent.Text(string.Join(Environment.NewLine, rows)));
            hasPublishedOpponentsPanel = true;
            return ValueTask.CompletedTask;
        }

        [ResponseAnalyzer<GameApi.TeamStadium.DecideFrameOrder>]
        public ValueTask AnalyzeDecideFrameOrder(TeamStadiumDecideFrameOrderResponse response)
        {
            var workspace = this.workspace ??= Workspace.Create(nameof(TeamStadiumOpponentListResponseAnalyzer));
            workspace.SetPanel(CurrentOpponentPanelKey, "当前对手", AnalyzeOpponent(response.data.opponent_info_copy));
            hasPublishedCurrentOpponentPanel = true;
            return ValueTask.CompletedTask;
        }

        [ResponseAnalyzer<GameApi.TeamStadium.Start>]
        public ValueTask AnalyzeStart(TeamStadiumStartResponse response)
        {
            var workspace = this.workspace ??= Workspace.Create(nameof(TeamStadiumOpponentListResponseAnalyzer));
            workspace.SetPanel(CurrentOpponentPanelKey, "当前对手", AnalyzeOpponent(response.data.opponent_info_copy));
            hasPublishedCurrentOpponentPanel = true;
            return ValueTask.CompletedTask;
        }

        static WorkspaceContent AnalyzeOpponent(TeamStadiumOpponent opponent)
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

            return WorkspaceContent.Text(string.Join(Environment.NewLine,
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
