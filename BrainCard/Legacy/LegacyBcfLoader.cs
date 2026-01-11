using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace BrainCard.Legacy;

public static class LegacyBcfLoader
{
    public static async Task<IReadOnlyList<LegacySavedImage>> LoadAsync(string filename)
    {
        if (string.IsNullOrWhiteSpace(filename))
        {
            return Array.Empty<LegacySavedImage>();
        }

        if (!File.Exists(filename))
        {
            return Array.Empty<LegacySavedImage>();
        }

        var json = await Task.Run(() => File.ReadAllText(filename)).ConfigureAwait(false);

        List<LegacySavedImage> items;
        try
        {
            items = await Task.Run(() => JsonConvert.DeserializeObject<List<LegacySavedImage>>(json)).ConfigureAwait(false);
        }
        catch
        {
            items = null;
        }

        return items ?? (IReadOnlyList<LegacySavedImage>)Array.Empty<LegacySavedImage>();
    }
}
