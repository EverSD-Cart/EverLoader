using System.Text.Json.Serialization;

namespace EverLoader.Helpers.RetroArch;

public class VersionResult
{
    [JsonPropertyName("version")]
    public string Version { get; set; }
}