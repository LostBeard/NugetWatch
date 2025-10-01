namespace NugetWatch.Services
{
    public static class DateTimeExtensions
    {
        public static DateTime StartOfDay(this DateTime theDate)
        {
            return DateTime.SpecifyKind(theDate.Date, theDate.Kind);
        }
        public static DateTimeOffset StartOfDay(this DateTimeOffset theDate)
        {
            return DateTime.SpecifyKind(theDate.Date, theDate.Offset == TimeSpan.Zero ? DateTimeKind.Utc : DateTimeKind.Local);
        }
        public static DateTime EndOfDay(this DateTime theDate)
        {
            return theDate.StartOfDay().AddDays(1).AddTicks(-1);
        }
        public static DateTimeOffset EndOfDay(this DateTimeOffset theDate)
        {
            return theDate.StartOfDay().AddDays(1).AddTicks(-1);
        }
    }
}
