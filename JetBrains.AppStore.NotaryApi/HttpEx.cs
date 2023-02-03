using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;

namespace JetBrains.AppStore.NotaryApi;

public static class HttpEx
{
    public const string MediaTypeApplicationJson = "application/json";

    public static async Task<T> ReadAsJsonBySchemaAsync<T>(this HttpContent content, T schema)
    {
        if (content.Headers.ContentType.MediaType is MediaTypeApplicationJson or MediaTypeNames.Application.Octet)  // Note(k15tfu): in fact they are all of type application/octet-stream
            return JsonConvert.DeserializeAnonymousType(await content.ReadAsStringAsync().ConfigureAwait(false), schema);

        throw new Exception($"unexpected media type {content.Headers.ContentType.MediaType}");
    }

    public static HttpRequestMessage WithAuthorization(this HttpRequestMessage message, AppStoreConnectAuth auth)
    {
        if (new PemReader(new StringReader(auth.PrivateKey)).ReadObject() is not ECPrivateKeyParameters ecPrivateKeyParameters)
            throw new Exception("invalid private key format");
        var q = ecPrivateKeyParameters.Parameters.G.Multiply(ecPrivateKeyParameters.D).Normalize();  // https://github.com/dotnet/core/issues/2037#issuecomment-436340605
        var x = q.AffineXCoord.GetEncoded();
        var y = q.AffineYCoord.GetEncoded();
        var d = ecPrivateKeyParameters.D.ToByteArrayUnsigned();
        var msEcp = new ECParameters { Curve = ECCurve.NamedCurves.nistP256, Q = { X = x, Y = y }, D = d };
        var tokenHandler = new JsonWebTokenHandler { SetDefaultTimesOnTokenCreation = false };
        var token = tokenHandler.CreateToken(new SecurityTokenDescriptor
        {
            SigningCredentials = new SigningCredentials(new ECDsaSecurityKey(ECDsa.Create(msEcp)) { KeyId = auth.KeyId }, SecurityAlgorithms.EcdsaSha256),
            Issuer = auth.IssuerId,
            IssuedAt = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddMinutes(5),
            Audience = NotaryClient.DefaultAppStoreConnectAudience
        });
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return message;
    }

    public static HttpRequestMessage WithAccept(this HttpRequestMessage message, string mediaType)
    {
        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(mediaType));
        return message;
    }
}