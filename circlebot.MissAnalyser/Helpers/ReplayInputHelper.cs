using System.Text.RegularExpressions;
using SysFile = System.IO.File;

namespace circlebot.MissAnalyser.Helpers;

// Resolves a replay to a file path the third-party parser can read directly. A cache-file
// reference points straight at the shared replay-cache volume Backend already wrote to - no copy.
// A direct upload gets written into replaysDirectory once, deduped by content md5.
public static partial class ReplayInputHelper
{
    private const string ReplayCachePath = "/replay-cache";

    [GeneratedRegex(@"^\d+-\d+\.osr$")]
    private static partial Regex CacheFileNameRegex();

    public static async Task<string?> ResolvePathAsync(IFormFile? file, string? cacheFileName,
        string replaysDirectory)
    {
        if (cacheFileName is not null)
        {
            if (!CacheFileNameRegex().IsMatch(cacheFileName))
                return null;

            var cachedPath = Path.Combine(ReplayCachePath, cacheFileName);
            return SysFile.Exists(cachedPath) ? cachedPath : null;
        }

        if (file is null)
            return null;

        await using var stream = file.OpenReadStream();
        var bytes = new byte[stream.Length];
        _ = await stream.ReadAsync(bytes);

        var md5 = CryptoHelper.GetMd5String(bytes);
        var replayPath = Path.Combine(replaysDirectory, $"{md5}.osr");
        if (!SysFile.Exists(replayPath))
            await SysFile.WriteAllBytesAsync(replayPath, bytes);

        return replayPath;
    }
}
