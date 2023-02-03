using System;
using JetBrains.Annotations;

namespace JetBrains.AppStore.NotaryApi;

public class AppStoreConnectAuth
{
    [NotNull] public readonly string IssuerId;
    [NotNull] public readonly string KeyId;
    [NotNull] public readonly string PrivateKey;

    public AppStoreConnectAuth([NotNull] string issuerId, [NotNull] string keyId, [NotNull] string privateKey)
    {
        IssuerId = issuerId ?? throw new ArgumentNullException(nameof(issuerId));
        KeyId = keyId ?? throw new ArgumentNullException(nameof(keyId));
        PrivateKey = privateKey ?? throw new ArgumentNullException(nameof(privateKey));
    }
}