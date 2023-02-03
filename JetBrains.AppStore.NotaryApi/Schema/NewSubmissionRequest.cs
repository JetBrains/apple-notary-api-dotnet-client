// ReSharper disable InconsistentNaming

namespace JetBrains.AppStore.NotaryApi.Schema;

public struct NewSubmissionRequest
{
    public Notifications? notifications;
    public string sha256;
    public string submissionName;

    // ReSharper disable once MemberCanBePrivate.Global
    public struct Notifications
    {
        public string channel;
        public string target;
    }
}