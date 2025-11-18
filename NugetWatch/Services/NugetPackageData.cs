using System.Text.Json.Serialization;

namespace NugetWatch.Services
{
    public class NugetPackageVersionData
    {
        /// <summary>
        /// When this package version was published
        /// </summary>
        public DateTimeOffset Published { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool Listed { get; set; }
    }
    public class NugetPackageData
    {

        //public List<NugetPackageData> OldData { get; set; }
        /// <summary>
        /// When the package was  last committed
        /// </summary>
        public DateTimeOffset? CommitTimeStamp { get; set; }
        /// <summary>
        /// Returns true if the package version reported in this data is older than the specified amount of time
        /// </summary>
        /// <param name="timeSpan"></param>
        /// <returns></returns>
        public bool PackageIsStale(TimeSpan timeSpan) => CommitTimeStamp != null && DateTimeOffset.Now - CommitTimeStamp > timeSpan;
        /// <summary>
        /// When the local data last changed
        /// </summary>
        public DateTimeOffset DataTimeStamp { get; set; } = DateTimeOffset.Now;
        /// <summary>
        /// DataTimeStamp in unix time
        /// </summary>
        public long DataTimeStampLong => DataTimeStamp.ToUnixTimeMilliseconds();
        /// <summary>
        /// When the package was first seen
        /// </summary>
        public DateTimeOffset FirstSeen { get; set; } = DateTimeOffset.Now;
        /// <summary>
        /// FirstSeenLong in unix time
        /// </summary>
        public long FirstSeenLong => FirstSeen.ToUnixTimeMilliseconds();

        public bool Active(TimeSpan timeSpan) => DataTimeStamp > FirstSeen && DateTimeOffset.Now - DataTimeStamp < timeSpan;
        /// <summary>
        /// Used as the key in the indexedDB.<br/>
        /// Package name and data timestamp
        /// </summary>
        public string DataKey => $"{Title} - {DataTimeStamp}";

        [JsonPropertyName("@id")]
        public string _Id { get; set; }

        [JsonPropertyName("@type")]
        public string _Type { get; set; }

        public string Registration { get; set; }
        public string Id { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        public string Summary { get; set; }
        public string Title { get; set; }
        public string IconUrl { get; set; }
        public string LicenseUrl { get; set; }
        public string ProjectUrl { get; set; }
        public string[] Tags { get; set; }
        public string? Author => Authors?.FirstOrDefault();
        public string[] Authors { get; set; }
        public string? Owner => Owners?.FirstOrDefault();
        public string[] Owners { get; set; }
        public long TotalDownloads { get; set; }
        public bool Verified { get; set; }
        public NugetPackageVersion[] Versions { get; set; }
        public bool Changed(NugetPackageData other)
        {
            if (other == null) return true;
            if (other._Id != _Id) return true;
            if (other._Type != _Type) return true;
            if (other.Registration != Registration) return true;
            if (other.Id != Id) return true;
            if (other.Version != Version) return true;
            if (other.Description != Description) return true;
            if (other.Summary != Summary) return true;
            if (other.Title != Title) return true;
            if (other.IconUrl != IconUrl) return true;
            if (other.LicenseUrl != LicenseUrl) return true;
            if (other.ProjectUrl != ProjectUrl) return true;
            if (!other.Tags.SequenceEqual(Tags)) return true;
            if (!other.Authors.SequenceEqual(Authors)) return true;
            if (!Owners.SequenceEqual(Owners)) return true;
            if (other.TotalDownloads != TotalDownloads) return true;
            if (other.Verified != Verified) return true;
            if (other.Versions.Length != Versions.Length) return true;
            var keyedVersionsOther = other.Versions.ToDictionary(o => o.Version, o => o);
            var keyedVersions = Versions.ToDictionary(o => o.Version, o => o);
            foreach (var kvp in keyedVersionsOther)
            {
                if (!keyedVersions.TryGetValue(kvp.Key, out var oldV)) return true;
                if (kvp.Value.Changed(oldV)) return true;
            }
            return false;
        }
    }
}
