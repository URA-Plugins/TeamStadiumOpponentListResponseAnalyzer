using Gallop;
using Gallop.Endpoints;
using MessagePack;
using UmamusumeResponseAnalyzer.Plugin;
using UmamusumeResponseAnalyzer.TerminalGui;

RepositoryChecks.Run();
using var ui = new WorkspaceSmokeSession();
PluginLifecycleSmoke.Run(ui);
using var directory = new TempCurrentDirectory("opponent-list-panel");
using var context = new RuntimePluginContext(ui.Application);
IPlugin plugin = new TeamStadiumOpponentListResponseAnalyzer.TeamStadiumOpponentListResponseAnalyzer();
Workspace? published = null;
try
{
    plugin.Initialize(context);
    var response = new TeamStadiumOpponentListResponse
    {
        data = new()
        {
            opponent_info_array =
            [
                new()
                {
                    strength = 3,
                    user_info = new() { name = "[red]markup[/]", single_mode_play_count = 42 }
                }
            ]
        }
    };
    var endpoint = GameEndpointCatalog.ByEndpointType[typeof(GameApi.TeamStadium.OpponentList)];
    var registration = PluginManager.CreateAttributeRegistrations(plugin).Single(candidate => candidate.EndpointType == endpoint.EndpointType);
    var originalOut = Console.Out;
    var originalError = Console.Error;
    using var output = new StringWriter();
    try
    {
        Console.SetOut(output);
        Console.SetError(output);
        await registration.Handler(new(endpoint, MessagePackSerializer.Serialize(response), new(null, null, null, null, null, null)));
    }
    finally
    {
        Console.SetOut(originalOut);
        Console.SetError(originalError);
    }
    if (output.ToString().Length != 0)
        throw new InvalidOperationException("Opponent analyzer wrote directly to the process console.");
    published = Workspace.Create("TeamStadiumOpponentListResponseAnalyzer");
    var screen = ui.CaptureScreen();
    if (!ReferenceEquals(Workspace.Current, published)
        || !screen.Contains("对手列表", StringComparison.Ordinal)
        || !screen.Contains("#3: [red]markup[/] 育成数 42", StringComparison.Ordinal))
        throw new InvalidOperationException("Opponent framebuffer did not preserve the panel, opponent values and literal markup.");
}
finally
{
    await plugin.DisposeAsync();
}
var retained = Workspace.Create("TeamStadiumOpponentListResponseAnalyzer");
retained.SwitchTo();
if (!ReferenceEquals(retained, published) || ui.CaptureScreen().Contains("[red]markup[/]", StringComparison.Ordinal))
    throw new InvalidOperationException("Dispose must retain the shared workspace and remove its opponent panel.");
retained.Remove();
Console.WriteLine("PASS opponent attribute dispatch, literal framebuffer and panel disposal");
