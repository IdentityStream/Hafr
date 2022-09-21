using System.Text.Json.Serialization;

public class EvaluationModel
{
    public string Template { get; set; } = null!;

    [JsonConverter(typeof(GenericDictionaryJsonConverter))]
    public Dictionary<string, object?> Data { get; set; } = null!;
}
