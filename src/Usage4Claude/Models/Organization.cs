using System.Text.Json.Serialization;

namespace Usage4Claude.Models;

// GET /api/organizations returns [Organization]
public class Organization
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("uuid")]
    public string Uuid { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public string? UpdatedAt { get; set; }

    [JsonPropertyName("capabilities")]
    public List<string>? Capabilities { get; set; }
}
