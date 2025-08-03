using System.Text.Json.Serialization;

namespace SpaceTraders.Models;

public class DataSingle<T>
{
    [JsonPropertyName("data")]
    public T? Datum { get; set; } = default;
}