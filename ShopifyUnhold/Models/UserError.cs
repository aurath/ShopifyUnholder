using System.Text.Json.Serialization;

namespace ShopifyUnhold.Models;

public class UserError
{
    [JsonPropertyName("field")]
    public string? Field { get; set; }

    [JsonPropertyName("message")]
    public required string Message { get; set; }
}
