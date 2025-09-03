using System.Globalization;

namespace Infrastructure.Utils
{
    public static class DateExtensions
    {
        private static readonly string[] ViDateFormats = { "dd/MM/yyyy", "d/M/yyyy" };

        public static DateTime? ToViDate(this string date)
        {
            if (string.IsNullOrWhiteSpace(date))
                return null;

            if (DateTime.TryParseExact(date.Trim(), ViDateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
                return result;

            return null;
        }

        public static DateTime ChangeTime(this DateTime dateTime, int hours, int minutes, int seconds)
        {
            return new DateTime(
                dateTime.Year,
                dateTime.Month,
                dateTime.Day,
                hours,
                minutes,
                seconds,
                0,
                dateTime.Kind);
        }

        public static DateRange? ToViDateRange(this string? dtrange)
        {
            if (string.IsNullOrWhiteSpace(dtrange))
                return null;

            var arrdt = dtrange.Split(new[] { '-', '~' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (arrdt.Length == 2)
            {
                var start = arrdt[0].ToViDate();
                var end = arrdt[1].ToViDate();
                if (start.HasValue && end.HasValue)
                {
                    var first = start.Value < end.Value ? start.Value : end.Value;
                    var second = start.Value < end.Value ? end.Value : start.Value;
                    return new DateRange(
                        first.ChangeTime(0, 0, 0),
                        second.ChangeTime(23, 59, 59)
                    );
                }
            }
            else if (arrdt.Length == 1)
            {
                var single = arrdt[0].ToViDate();
                if (single.HasValue)
                {
                    var start = single.Value.ChangeTime(0, 0, 0);
                    var end = single.Value.ChangeTime(23, 59, 59);
                    return new DateRange(start, end);
                }
            }
            return null;
        }

        public static string ToViDate(this DateTime date)
        {
            return date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        }
        public static string ToViDateTime(this DateTime date)
        {
            return string.Format("{0:HH:mm dd/MM/yyyy}", date);
        }
    }


    public readonly struct DateRange
    {
        public DateRange(DateTime start, DateTime end)
        {
            Start = start;
            End = end;
        }
        public DateTime Start { get; }
        public DateTime End { get; }

        public override string ToString()
        {
            return $"{Start.ToViDate()} - {End.ToViDate()}";
        }
    }

}
