namespace HospitalManagement.API.Time;

/// <summary>
/// D6 time convention (ARCHITECTURE.md §5): instants are stored/transported as
/// UTC; hospital business dates are IST calendar dates. This is the single
/// place IST math happens.
/// </summary>
public static class IstClock
{
    private static readonly TimeZoneInfo Ist = Resolve();

    private static TimeZoneInfo Resolve()
    {
        // Linux/macOS use IANA ids; Windows dev boxes use the Windows id.
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata"); }
        catch (TimeZoneNotFoundException)
        { return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"); }
    }

    public static DateTime NowIst() =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Ist);

    /// <summary>Today's IST calendar date, as a midnight DateTime for DATE columns.</summary>
    public static DateTime TodayIstDate() => NowIst().Date;

    /// <summary>[startUtc, endUtcExclusive) covering one IST calendar day. Index-friendly.</summary>
    public static (DateTime StartUtc, DateTime EndUtc) UtcRangeForIstDay(DateTime istDate)
    {
        var startIst = DateTime.SpecifyKind(istDate.Date, DateTimeKind.Unspecified);
        var startUtc = TimeZoneInfo.ConvertTimeToUtc(startIst, Ist);
        return (startUtc, startUtc.AddDays(1));
    }

    public static (DateTime StartUtc, DateTime EndUtc) TodayRangeUtc() =>
        UtcRangeForIstDay(TodayIstDate());

    public static (DateTime StartUtc, DateTime EndUtc) TomorrowRangeUtc() =>
        UtcRangeForIstDay(TodayIstDate().AddDays(1));

    /// <summary>Convert a UTC instant to IST wall-clock.</summary>
    public static DateTime UtcToIst(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Ist);

    /// <summary>Convert an IST wall-clock value (Kind ignored) to its UTC instant.</summary>
    public static DateTime IstToUtc(DateTime istWall) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(istWall, DateTimeKind.Unspecified), Ist);
}
