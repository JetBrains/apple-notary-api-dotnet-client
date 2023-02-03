// ReSharper disable InconsistentNaming

namespace JetBrains.AppStore.NotaryApi.Schema;

public struct ErrorResponse
{
    public string description;
    public string[] labels;
    public string name;
}