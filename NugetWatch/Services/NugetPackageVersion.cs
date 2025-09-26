using System.Text.Json.Serialization;

namespace NugetWatch.Services
{
    public class NugetPackageVersion
    {
        public long Downloads { get; set; }
        public string Version { get; set; }
        [JsonPropertyName("@id")]
        public string _Id { get; set; }

        public bool Changed(NugetPackageVersion other)
        {
            if (other == null) return true;
            if (other.Downloads != Downloads) return true;
            if (other.Version != Version) return true;
            if (other._Id != _Id) return true;
            return false;
        }
    }
}
