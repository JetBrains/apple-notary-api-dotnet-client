using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using JetBrains.AppStore.NotaryApi;

namespace ConsoleClient;

internal static class Program
{
    private static readonly AppStoreConnectAuth ourAppStoreConnectAuth = new(
        "YOUR PRIVATE ISSUER ID",
        "YOUR PRIVATE KEY ID",
        "YOUR PRIVATE KEY"
    );

    private static void Main(string[] args)
    {
        var client = new NotaryClient(NotaryClient.AppleAppStoreConnectNotaryApiRootUrl, ourAppStoreConnectAuth);

        switch (args[0])
        {
        case "submit-software":
        {
            var fileName = args[1];
            var sha256 = BitConverter.ToString(SHA256.Create().ComputeHash(new FileStream(fileName, FileMode.Open))).Replace("-", string.Empty);

            Console.WriteLine($"Notarizing software {new FileInfo(fileName).Name} (sha256 {sha256}) ...");
            var expectedNewSubmissionResponse = client.SubmitSoftware(sha256, new FileInfo(fileName).Name).Result;
            if (expectedNewSubmissionResponse.Result is not {} newSubmissionResponse) throw new Exception(expectedNewSubmissionResponse.Error.Data);
            Console.WriteLine($"Submit software response: id {newSubmissionResponse.data.id}, type {newSubmissionResponse.data.type}");

            Console.WriteLine($"Uploading file {fileName} ({new FileInfo(fileName).Length} bytes) to Apple S3 ...");
            var putObjectResponse = NotarySubmissionsS3Client.UploadSubmission(new FileStream(fileName, FileMode.Open), newSubmissionResponse.data.attributes).Result;
            Console.WriteLine($"File upload status: {putObjectResponse.HttpStatusCode}");

            while (true)
            {
                var expectedSubmissionStatus = client.GetSubmissionStatus(newSubmissionResponse.data.id).Result;
                if (expectedSubmissionStatus.Result is not {} submissionStatus) throw new Exception(expectedSubmissionStatus.Error.Data);
                Console.WriteLine($"Submission status: id {submissionStatus.data.id} type {submissionStatus.data.type} createdDate {submissionStatus.data.attributes.createdDate} name {submissionStatus.data.attributes.name} status {submissionStatus.data.attributes.status}");
                if (submissionStatus.data.attributes.status != SubmissionStatuses.InProgress)
                    break;

                Console.WriteLine("Sleeping 60 seconds ...");
                Thread.Sleep(TimeSpan.FromSeconds(60));
            }

            break;
        }
        case "get-submission-status":
        {
            // get-submission-status cfe5a1dd-fc6d-4bab-9b11-c91532adbe9d
            var expectedSubmissionStatus = client.GetSubmissionStatus(args[1]).Result;
            if (expectedSubmissionStatus.Result is not {} submissionStatus) throw new Exception(expectedSubmissionStatus.Error.Data);
            Console.WriteLine($"Submission status: id {submissionStatus.data.id} type {submissionStatus.data.type} createdDate {submissionStatus.data.attributes.createdDate} name {submissionStatus.data.attributes.name} status {submissionStatus.data.attributes.status}");
            break;
        }
        case "get-submission-log":
        {
            // get-submission-log cfe5a1dd-fc6d-4bab-9b11-c91532adbe9d
            var expectedSubmissionLog = client.GetSubmissionLog(args[1]).Result;
            if (expectedSubmissionLog.Result is not {} submissionLog) throw new Exception(expectedSubmissionLog.Error.Data);
            Console.WriteLine($"Submission log: id {submissionLog.data.id} type {submissionLog.data.type} developerLogUrl {submissionLog.data.attributes.developerLogUrl}");

            using var logReq =new HttpRequestMessage(HttpMethod.Get, submissionLog.data.attributes.developerLogUrl);
            using var httpClient = new HttpClient();
            using var logResp = httpClient.Send(logReq);
            using var logRespContent = logResp.Content;
            var logContent = logRespContent.ReadAsStringAsync().Result;
            Console.WriteLine("LOG FILE:");
            Console.WriteLine(logContent);
            break;
        }
        case "get-previous-submissions":
        {
            // get-previous-submissions
            var expectedPreviousSubmissions = client.GetPreviousSubmissions().Result;
            if (expectedPreviousSubmissions.Result is not {} previousSubmissions) throw new Exception(expectedPreviousSubmissions.Error.Data);
            foreach (var submissionStatus in previousSubmissions.data)
                Console.WriteLine($"One of previous submission: id {submissionStatus.id} type {submissionStatus.type} createdDate {submissionStatus.attributes.createdDate} name {submissionStatus.attributes.name} status {submissionStatus.attributes.status}");
            break;
        }
        default:
            throw new ArgumentOutOfRangeException();
        }
    }
}