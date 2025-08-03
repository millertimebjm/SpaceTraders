using System.Text.Json.Serialization;

namespace SpaceTraders.Models;

public class Data<T>
{
    [JsonPropertyName("data")]
    public IEnumerable<T> DataList { get; set; } = new List<T>();
    public Meta? Meta { get; set; } = null;

}