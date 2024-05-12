using BMAPI.v1;
using osuDodgyMomentsFinder;
using OsuMissAnalyzer.Core;
using ReplayAPI;

namespace OsuMissAnalyzer.Server.Models;

public class ServerReplayLoader : IReplayLoader
{
    public Replay Replay { get; set; }
    public Beatmap Beatmap { get; set; }
    public ColourScheme ColourScheme => ColourScheme.Default;
    public ReplayAnalyzer ReplayAnalyzer { get; set; }

    public ServerReplayLoader(string replayPath, string beatmapPath)
    {
        Replay = new Replay(replayPath);
        Beatmap = new Beatmap(beatmapPath);
        ReplayAnalyzer = new ReplayAnalyzer(Beatmap, Replay);
    }
}