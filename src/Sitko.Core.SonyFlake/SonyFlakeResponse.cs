using System.Text.Json.Serialization;

namespace Sitko.Core.SonyFlake
{
    public class SonyFlakeResponse
    {
        [JsonPropertyName("id")] public long Id { get; set; }

        [JsonPropertyName("machine-id")] public int MachineId { get; set; }

        [JsonPropertyName("msb")] public int Msb { get; set; }

        [JsonPropertyName("sequenceId")] public int SequenceId { get; set; }

        [JsonPropertyName("time")] public long Time { get; set; }
    }
}
