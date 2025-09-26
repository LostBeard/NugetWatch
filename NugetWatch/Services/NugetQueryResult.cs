namespace NugetWatch.Services
{
    public class NugetQueryResult
    {
        public long TotalHits { get; set; }
        public NugetPackageData[] Data { get; set; }
    }
}
