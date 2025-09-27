namespace NugetWatch.Services
{
    public class NugetPackageChangeEventArg
    {
        public NugetPackageData? PackageDataOld { get; set; }
        public NugetPackageData PackageDataNew { get; set; }
    }
}
