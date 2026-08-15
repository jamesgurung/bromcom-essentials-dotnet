using System.Globalization;
using System.Text.RegularExpressions;

namespace BromcomEssentials;

public partial class BromcomClient
{
  private static string BuildDefaultEntityFilter(string? dateFieldPrefix)
  {
    var today = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    return $"{dateFieldPrefix}startDate <='{today}' and ({dateFieldPrefix}endDate>='{today}' or isnull({dateFieldPrefix}endDate,'')='')";
  }

  private static string BuildBasicAttendanceEntityFilter(DateOnly startDate, DateOnly endDate, string? periodName, IList<int>? studentIds)
  {
    var start = startDate.ToDateTime(TimeOnly.MinValue);
    var end = endDate.ToDateTime(new TimeOnly(23, 59, 59));
    var filters = new List<string>
    {
      FormattableString.Invariant($"calendarStartDate>='{start:yyyy-MM-ddTHH:mm:ss}'"),
      FormattableString.Invariant($"calendarStartDate<='{end:yyyy-MM-ddTHH:mm:ss}'")
    };

    if (periodName is not null) filters.Add($"periodDisplayName='{EscapeEntityFilterValue(periodName)}'");
    if (studentIds is { Count: > 0 }) filters.Add($"({string.Join(" or ", studentIds.Distinct().Select(id => $"studentID={id.ToString(CultureInfo.InvariantCulture)}"))})");

    return string.Join(" and ", filters);
  }

  private static string BuildDateRangeEntityFilter(string fieldName, DateOnly startDate, DateOnly endDate)
  {
    var start = startDate.ToDateTime(TimeOnly.MinValue);
    var end = endDate.ToDateTime(new TimeOnly(23, 59, 59));
    return FormattableString.Invariant($"{fieldName}>='{start:yyyy-MM-ddTHH:mm:ss}' and {fieldName}<='{end:yyyy-MM-ddTHH:mm:ss}'");
  }

  private static string BuildAssessmentResultsEntityFilter(int academicYearStart, string? term, int? yearGroup, bool gradesOnly)
  {
    var start = new DateOnly(academicYearStart, 9, 1);
    var endExclusive = new DateOnly(academicYearStart + 1, 9, 1);
    var filters = new List<string>
    {
      FormattableString.Invariant($"enteredDate>='{start:yyyy-MM-dd}'"),
      FormattableString.Invariant($"enteredDate<'{endExclusive:yyyy-MM-dd}'")
    };

    if (term is not null) filters.Add($"termName='{EscapeEntityFilterValue(term)}'");
    if (yearGroup is not null) filters.Add($"yearGroupName='{yearGroup.Value.ToString(CultureInfo.InvariantCulture)}'");
    if (gradesOnly) filters.Add("isGrade='True'");

    return string.Join(" and ", filters);
  }

  private static void ValidateDateRange(DateOnly startDate, DateOnly? endDate)
  {
    if (endDate is not null && endDate < startDate)
      throw new ArgumentOutOfRangeException(nameof(endDate), "End date cannot precede start date.");
  }

  private static string EscapeEntityFilterValue(string value) => value.Replace("'", "''", StringComparison.Ordinal);

  private static string? CleanString(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

  private static DateOnly? ParseDateOnly(string? value) => value is null || value.Length < 10
    ? null : (DateOnly.TryParseExact(value[..10], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : null);

  private static DateTime? ParseDateTime(string? value) => value is null || value.Length < 19
    ? null : (DateTime.TryParseExact(value[..19], "yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var date) ? date : null);

  private static bool ParseBooleanFlag(string? value) => value?.Equals("yes", StringComparison.OrdinalIgnoreCase) ?? false;

  private static int? ParseNullableInt(string? value) => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;

  private static string? ParseEnrolmentStatus(string? statusCode) => statusCode switch
  {
    "C" => "Single Registration",
    "M" => "Main - Dual Registration",
    "S" => "Subsidiary - Dual Registration",
    "G" => "Guest",
    _ => null
  };

  private static string? CleanRoom(string? value)
  {
    var cleaned = CleanString(value);
    return (cleaned is null || string.Equals(cleaned, "UNKNOWN", StringComparison.OrdinalIgnoreCase) || string.Equals(cleaned, "DEFAULT", StringComparison.OrdinalIgnoreCase))
      ? null : cleaned;
  }

  private static string? CleanTutorGroup(string? value)
  {
    var cleaned = CleanString(value);
    return (cleaned is null || string.Equals(cleaned, "NoTutorGrp", StringComparison.OrdinalIgnoreCase)) ? null : cleaned;
  }

  private static string? CleanTelephone(string? value)
  {
    var cleaned = CleanString(value);
    if (cleaned is null) return null;
    var startsWithPlus = cleaned.StartsWith('+');
    var digitsOnly = new string(cleaned.Where(char.IsDigit).ToArray());
    return startsWithPlus ? $"+{digitsOnly}" : digitsOnly;
  }

  private static string? CleanParentName(string? value)
  {
    var cleaned = CleanString(value);
    if (cleaned is null) return null;

    var titles = new[] { "Br", "Dame", "Dr", "Fr", "Hon", "Lady", "Lord", "Miss", "Mr", "Mrs", "Ms", "Prof", "Pstr", "Rev", "Sir", "Sr" };

    var parts = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    if (parts.Length == 0) return null;

    var hasTitle = titles.Contains(parts[0], StringComparer.OrdinalIgnoreCase);
    var title = hasTitle ? parts[0] : null;
    var index = hasTitle ? 1 : 0;

    if (index >= parts.Length) return title;
    if (parts.Length - index == 1) return string.Join(" ", new[] { title, parts[index] }.Where(x => !string.IsNullOrWhiteSpace(x)));

    var firstInitial = char.ToUpperInvariant(parts[index][0]).ToString();
    var surnameStart = index + 1;
    while (surnameStart < parts.Length && parts[surnameStart].Length == 1) surnameStart++;
    var surname = surnameStart < parts.Length ? string.Join(" ", parts[surnameStart..]) : null;

    return string.Join(" ", new[] { title, firstInitial, surname }.Where(x => !string.IsNullOrWhiteSpace(x)));
  }

  private static string? CleanClassName(string? value)
  {
    var cleaned = CleanString(value);
    return cleaned is null ? null : ClassNameYearSuffixRegex().Replace(cleaned, string.Empty);
  }

  [GeneratedRegex(@"\s+\(\d{2}/\d{2}\)$", RegexOptions.CultureInvariant)]
  private static partial Regex ClassNameYearSuffixRegex();
}
