using System.Text.Json.Serialization;

namespace Usage4Claude.Models;

public class Account
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string SessionKey { get; set; } = string.Empty;
    public string OrganizationId { get; set; } = string.Empty;  // UUID format string
    public string OrganizationName { get; set; } = string.Empty;
    public string? Alias { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    public string DisplayName => Alias ?? OrganizationName;
}
