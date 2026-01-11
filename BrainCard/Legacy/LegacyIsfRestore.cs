using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

#if !BRAIN_CARD_DISABLE_XAML_ISLANDS
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage.Streams;
using Windows.UI.Input.Inking;
#endif

namespace BrainCard.Legacy;

public static class LegacyIsfRestore
{
    public static byte[] TryDecodeInkDataToBytes(string inkData)
    {
        if (string.IsNullOrWhiteSpace(inkData)) return null;

        // InkData is sometimes a JSON-encoded string (e.g. "\"R0lG...\""), sometimes raw base64.
        var candidate = inkData;

        try
        {
            if (candidate.Length >= 2 && ((candidate[0] == '"' && candidate[^1] == '"') || candidate.Contains("\\\"")))
            {
                var unquoted = JsonConvert.DeserializeObject<string>(candidate);
                if (!string.IsNullOrWhiteSpace(unquoted))
                {
                    candidate = unquoted;
                }
            }
        }
        catch
        {
        }

        // Trim whitespace and surrounding quotes just in case.
        candidate = candidate.Trim().Trim('"');

        try
        {
            return Convert.FromBase64String(candidate);
        }
        catch
        {
            return null;
        }
    }

#if !BRAIN_CARD_DISABLE_XAML_ISLANDS
    public static async Task<InkStrokeContainer> TryRestoreInkStrokeContainerAsync(string inkData)
    {
        var bytes = TryDecodeInkDataToBytes(inkData);
        if (bytes == null || bytes.Length == 0) return null;

        try
        {
            using var ms = new MemoryStream(bytes);
            using var stream = ms.AsRandomAccessStream();
            var container = new InkStrokeContainer();
            await container.LoadAsync(stream);
            return container;
        }
        catch
        {
            return null;
        }
    }
#endif
}
