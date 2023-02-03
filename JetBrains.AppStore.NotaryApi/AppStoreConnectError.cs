using System.Net.Http;
using System.Threading.Tasks;

namespace JetBrains.AppStore.NotaryApi;

/*
 Examples:
 1. ```
 Unauthenticated

 Request ID: STROI4FOOK4V6BARDAPVG5ZWYI.0.0
 ```
 2. ```
 {
    "errors":[
       {
          "id":"4a0dfae8-5432-4fe1-99ac-74bd14f8137c",
          "status":"400",
          "code":"BAD_REQUEST",
          "title":"Improperly formatted request",
          "detail":"Provided sha256 digest of the submission must be a 64-digit hexadecimal string"
       }
    ]
 }
 ```
 3. ```
 {
    "errors":[
       {
          "id":"4a0dfae8-5432-4fe1-99ac-74bd14f8137c",
          "status":"400",
          "code":"BAD_REQUEST",
          "title":"Improperly formatted request",
          "detail":"Must specify both submissionName and sha256 digest of the submission file contents"
       }
    ]
 }
 ```
 4. (empty string)
 */
public class AppStoreConnectError
{
    // ReSharper disable once MemberCanBePrivate.Global
    public readonly HttpResponseMessage Source;

    public readonly string Data;

    public AppStoreConnectError(HttpResponseMessage source, string data)
    {
        Source = source;
        Data = data;
    }

    public static async Task<AppStoreConnectError> FromResponseAsync(HttpResponseMessage source)
    {
       return new AppStoreConnectError(source, await source.Content.ReadAsStringAsync().ConfigureAwait(false));
    }
}