using System.Text.RegularExpressions;
using SysFile = System.IO.File;

namespace circlebot.MissAnalyser.Helpers;

// Reads replay bytes either from a directly-uploaded multipart file, or from the shared replay
// cache volume Backend also writes to (avoids re-uploading bytes it already fetched once).
public static partial class ReplayInputHelper
{
    private const string ReplayCachePath = "/replay-cache";

    [GeneratedRegex(@"^\d+-\d+\.osr$")]
    private static partial Regex CacheFileNameRegex();

    public static async Task<byte[]?> ReadAsync(IFormFile? file, string? cacheFileName)
    {
        if (cacheFileName is not null)
        {
            if (!CacheFileNameRegex().IsMatch(cacheFileName))
                return null;

            var path = Path.Combine(ReplayCachePath, cacheFileName);
            return SysFile.Exists(path) ? await SysFile.ReadAllBytesAsync(path) : null;
        }

        if (file is null)
            return null;

        await using var stream = file.OpenReadStream();
        var bytes = new byte[stream.Length];
        _ = await stream.ReadAsync(bytes);
        return bytes;
    }
}
