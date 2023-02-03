using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using JetBrains.AppStore.NotaryApi.Schema;
using Newtonsoft.Json;

namespace JetBrains.AppStore.NotaryApi;

// Apple Notary API client
// https://developer.apple.com/documentation/notaryapi
public class NotaryClient
{
    public const string AppleAppStoreConnectNotaryApiRootUrl = "https://appstoreconnect.apple.com/notary/v2/";

    public const string DefaultAppStoreConnectAudience = "appstoreconnect-v1";  // https://developer.apple.com/documentation/appstoreconnectapi/generating_tokens_for_api_requests

    private static readonly HttpClient ourClient = new();

    private readonly AppStoreConnectAuth myAppStoreConnectAuth;

    private readonly string myRootUrl;

    public NotaryClient(string rootUrl, AppStoreConnectAuth appStoreConnectAuth)
    {
        myRootUrl = rootUrl;
        myAppStoreConnectAuth = appStoreConnectAuth;
    }

    /// <summary>
    /// Start the process of uploading a new version of your software to the notary service.
    ///
    /// POST https://appstoreconnect.apple.com/notary/v2/submissions
    /// </summary>
    /// <param name="sha256"></param>
    /// <param name="submissionName"></param>
    /// <returns>new submission response if successful; error otherwise</returns>
    public async Task<Expected<NewSubmissionResponse>> SubmitSoftware(string sha256, string submissionName)
    {
        var data = new
        {
            // ReSharper disable RedundantAnonymousTypePropertyName
            sha256 = sha256,
            submissionName = submissionName
            // ReSharper restore RedundantAnonymousTypePropertyName
        };
        var req = new HttpRequestMessage(HttpMethod.Post, $"{myRootUrl}submissions")
            .WithAuthorization(myAppStoreConnectAuth)
            .WithAccept(HttpEx.MediaTypeApplicationJson);
        req.Content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, HttpEx.MediaTypeApplicationJson);

        var resp = await ourClient.SendAsync(req).ConfigureAwait(false);
        switch (resp.StatusCode)
        {
        case HttpStatusCode.OK:
        {
            /*
             Examples:
             ```
             {
                "data":{
                   "attributes":{
                      "awsAccessKeyId":"ASIAIOSFODNN7EXAMPLE",
                      "awsSecretAccessKey":"wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
                      "awsSessionToken":"AQoDYXdzEJr...",
                      "bucket":"EXAMPLE-BUCKET",
                      "object":"EXAMPLE-KEY-NAME"
                   },
                   "id":"2efe2717-52ef-43a5-96dc-0797e4ca1041",
                   "type":"submissionsPostResponse"
                },
                "meta":{
                }
             }
             ```
             */
            var schema = default(NewSubmissionResponse);
            var json = await resp.Content.ReadAsJsonBySchemaAsync(schema).ConfigureAwait(false);
            return json;
        }
        case HttpStatusCode.BadRequest:
        case HttpStatusCode.Unauthorized:
        case HttpStatusCode.NotFound:
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
            return Expected<NewSubmissionResponse>.Unexpected(await AppStoreConnectError.FromResponseAsync(resp).ConfigureAwait(false));
        default:
            throw await new Exception($"unexpected status code {resp.StatusCode}").WithDataAsync(resp).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Fetch the status of a software notarization submission.
    ///
    /// GET https://appstoreconnect.apple.com/notary/v2/submissions/{submissionId}
    /// </summary>
    /// <param name="submissionId"></param>
    /// <returns>submission response if successful; error otherwise</returns>
    public async Task<Expected<SubmissionResponse>> GetSubmissionStatus(string submissionId)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, $"{myRootUrl}submissions/{submissionId}")
            .WithAuthorization(myAppStoreConnectAuth)
            .WithAccept(HttpEx.MediaTypeApplicationJson);

        var resp = await ourClient.SendAsync(req).ConfigureAwait(false);
        switch (resp.StatusCode)
        {
        case HttpStatusCode.OK:
        {
            /*
             Examples:
             ```
             {
                "data":{
                   "attributes":{
                      "createdDate":"2022-06-08T01:38:09.498Z",
                      "name":"OvernightTextEditor_11.6.8.zip",
                      "status":"Accepted"
                   },
                   "id":"2efe2717-52ef-43a5-96dc-0797e4ca1041",
                   "type":"submissions"
                },
                "meta":{
                }
             }
             ```
             */
            var schema = default(SubmissionResponse);
            var json = await resp.Content.ReadAsJsonBySchemaAsync(schema).ConfigureAwait(false);
            return json;
        }
        case HttpStatusCode.BadRequest:
        case HttpStatusCode.Unauthorized:
        case HttpStatusCode.Forbidden:
        case HttpStatusCode.NotFound:
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
                      "status":"404",
                      "code":"NOT_FOUND",
                      "title":"The specified resource does not exist",
                      "detail":"There is no resource of type 'submissions' with id '2efe2717-52ef-43a5-96dc-0797e4ca1041'"
                   }
                ]
             }
             ```
             */
            return Expected<SubmissionResponse>.Unexpected(await AppStoreConnectError.FromResponseAsync(resp).ConfigureAwait(false));
        default:
            throw await new Exception($"unexpected status code {resp.StatusCode}").WithDataAsync(resp).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Fetch details about a single completed notarization.
    ///
    /// GET https://appstoreconnect.apple.com/notary/v2/submissions/{submissionId}/logs
    /// </summary>
    /// <param name="submissionId"></param>
    /// <returns>submission response if successful; error otherwise</returns>
    public async Task<Expected<SubmissionLogUrlResponse>> GetSubmissionLog(string submissionId)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, $"{myRootUrl}submissions/{submissionId}/logs")
            .WithAuthorization(myAppStoreConnectAuth)
            .WithAccept(HttpEx.MediaTypeApplicationJson);

        var resp = await ourClient.SendAsync(req).ConfigureAwait(false);
        switch (resp.StatusCode)
        {
        case HttpStatusCode.OK:
        {
            /*
             Examples:
             ```
             {
                "data":{
                   "attributes":{
                      "developerLogUrl":"https://..."
                   },
                   "id":"2efe2717-52ef-43a5-96dc-0797e4ca1041",
                   "type":"submissionsLog"
                },
                "meta":{
                }
             }
             ```
             */
            var schema = default(SubmissionLogUrlResponse);
            var json = await resp.Content.ReadAsJsonBySchemaAsync(schema).ConfigureAwait(false);
            return json;
        }
        case HttpStatusCode.BadRequest:
        case HttpStatusCode.Unauthorized:
        case HttpStatusCode.Forbidden:
        case HttpStatusCode.NotFound:
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
                      "status":"404",
                      "code":"NOT_FOUND",
                      "title":"The specified resource does not exist",
                      "detail":"There is no resource of type 'submissions' with id '2efe2717-52ef-43a5-96dc-0797e4ca1041'"
                   }
                ]
             }
             ```
             */
            return Expected<SubmissionLogUrlResponse>.Unexpected(await AppStoreConnectError.FromResponseAsync(resp).ConfigureAwait(false));
        default:
            throw await new Exception($"unexpected status code {resp.StatusCode}").WithDataAsync(resp).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Fetch a list of your team’s previous notarization submissions.
    ///
    /// GET https://appstoreconnect.apple.com/notary/v2/submissions
    /// </summary>
    /// <returns>submission list if successful; error otherwise</returns>
    public async Task<Expected<SubmissionListResponse>> GetPreviousSubmissions()
    {
        var req = new HttpRequestMessage(HttpMethod.Get, $"{myRootUrl}submissions")
            .WithAuthorization(myAppStoreConnectAuth)
            .WithAccept(HttpEx.MediaTypeApplicationJson);

        var resp = await ourClient.SendAsync(req).ConfigureAwait(false);
        switch (resp.StatusCode)
        {
        case HttpStatusCode.OK:
        {
            /*
             Examples:
             ```
             {
                "data":[
                   {
                      "attributes":{
                         "createdDate":"2021-04-29T01:38:09.498Z",
                         "name":"OvernightTextEditor_11.6.8.zip",
                         "status":"Accepted"
                      },
                      "id":"2efe2717-52ef-43a5-96dc-0797e4ca1041",
                      "type":"submissions"
                   },
                   {
                      "attributes":{
                         "createdDate":"2021-04-23T17:44:54.761Z",
                         "name":"OvernightTextEditor_11.6.7.zip",
                         "status":"Accepted"
                      },
                      "id":"cf0c235a-dad2-4c24-96eb-c876d4cb3a2d",
                      "type":"submissions"
                   },
                   {
                      "attributes":{
                         "createdDate":"2021-04-19T16:56:17.839Z",
                         "name":"OvernightTextEditor_11.6.7.zip",
                         "status":"Invalid"
                      },
                      "id":"38ce81cc-0bf7-454b-91ef-3f7395bf297b",
                      "type":"submissions"
                   }
                ],
                "meta":{
                }
             }
             ```
             */
            var schema = default(SubmissionListResponse);
            var json = await resp.Content.ReadAsJsonBySchemaAsync(schema).ConfigureAwait(false);
            return json;
        }
        case HttpStatusCode.BadRequest:
        case HttpStatusCode.Unauthorized:
        case HttpStatusCode.Forbidden:
        case HttpStatusCode.NotFound:
            /*
             Examples:
             1. ```
             Unauthenticated

             Request ID: STROI4FOOK4V6BARDAPVG5ZWYI.0.0
             ```
             2. (empty string)
             */
            return Expected<SubmissionListResponse>.Unexpected(await AppStoreConnectError.FromResponseAsync(resp).ConfigureAwait(false));
        default:
            throw await new Exception($"unexpected status code {resp.StatusCode}").WithDataAsync(resp).ConfigureAwait(false);
        }
    }

    public class Expected<TResult> where TResult : struct
    {
       public readonly TResult? Result;
       public readonly AppStoreConnectError Error;

       public static implicit operator Expected<TResult>(TResult result) => new Expected<TResult>(result, null);
       public static Expected<TResult> Unexpected(AppStoreConnectError error) => new Expected<TResult>(null, error);

       private Expected(TResult? result, AppStoreConnectError error)
       {
          Result = result;
          Error = error;
       }
    }
}