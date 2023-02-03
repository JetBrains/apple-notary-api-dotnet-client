// ReSharper disable InconsistentNaming
// ReSharper disable UnassignedField.Global

namespace JetBrains.AppStore.NotaryApi.Schema;

public struct SubmissionResponse
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
            public string createdDate;
            public string name;
            public string status;
        }
    }

    public struct Meta
    {
    }
}