using Newtonsoft.Json;

// ReSharper disable InconsistentNaming
// ReSharper disable UnassignedField.Global

namespace JetBrains.AppStore.NotaryApi.Schema;

public struct NewSubmissionResponse
{
    public Data data;
    public Meta meta;

    public struct Data
    {
        public Attributes attributes;
        public string id;
        public string type;

        public struct Attributes
        {
            public string awsAccessKeyId;
            public string awsSecretAccessKey;
            public string awsSessionToken;
            public string bucket;
            [JsonProperty("object")] public string object_;
        }
    }

    public struct Meta
    {
    }
}