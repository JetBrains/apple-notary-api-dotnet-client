using System.IO;
using System.Net.Mime;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using JetBrains.AppStore.NotaryApi.Schema;

namespace JetBrains.AppStore.NotaryApi;

// Notary Submissions Apple S3 client
public static class NotarySubmissionsS3Client
{
    // ReSharper disable once MemberCanBePrivate.Global
    public static readonly RegionEndpoint DefaultS3Region = RegionEndpoint.USWest2;  // https://developer.apple.com/documentation/security/notarizing_macos_software_before_distribution/customizing_the_notarization_workflow

    /// <summary>
    /// Uploads submission to Apple S3 bucket.
    /// </summary>
    /// <param name="fileContent"></param>
    /// <param name="submissionAttributes"></param>
    /// <param name="useAccelerateEndpoint"></param>
    /// <returns>PutObject as returned by S3</returns>
    public static async Task<PutObjectResponse> UploadSubmission(Stream fileContent, NewSubmissionResponse.Data.Attributes submissionAttributes, bool useAccelerateEndpoint = true)
    {
        using var s3Client = new AmazonS3Client(submissionAttributes.awsAccessKeyId, submissionAttributes.awsSecretAccessKey, submissionAttributes.awsSessionToken, new AmazonS3Config { UseAccelerateEndpoint = useAccelerateEndpoint, RegionEndpoint = DefaultS3Region });
        var putObjectRequest = new PutObjectRequest
        {
            BucketName = submissionAttributes.bucket,
            Key = submissionAttributes.object_,
            InputStream = fileContent,
            Headers =
            {
                ContentType = MediaTypeNames.Application.Octet,
                ContentLength = fileContent.Length
            }
        };
        return await s3Client.PutObjectAsync(putObjectRequest).ConfigureAwait(false);
    }
}