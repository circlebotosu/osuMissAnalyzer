using BMAPI.v1;
using osuDodgyMomentsFinder;
using ReplayAPI;

namespace circlebot.MissAnalyser.Models;

// Analyzer is optional: it is only built for osu!std and can be null if std hit-detection fails.
// Frametime and hold-time analysis work from Replay.ReplayFrames regardless.
public record LoadedReplay(Replay Replay, Beatmap Beatmap, ReplayAnalyzer? Analyzer);
