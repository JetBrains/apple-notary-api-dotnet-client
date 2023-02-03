using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace JetBrains.AppStore.NotaryApi;

internal static class Extensions
{
    public static async Task<Exception> WithDataAsync(this Exception exception, HttpResponseMessage resp)
    {
        exception.Data.Add(nameof(resp), resp.ToString());
        exception.Data.Add(nameof(resp.Content), await resp.Content.ReadAsStringAsync().ConfigureAwait(false));
        return exception;
    }
}