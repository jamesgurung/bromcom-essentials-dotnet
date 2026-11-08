using System.Globalization;

namespace BromcomEssentials;

public partial class BromcomClient
{
  /// <summary>Gets attendance periods starting within a date range, with an optional end date.</summary>
  /// <param name="schoolId">The Bromcom school identifier.</param>
  /// <param name="startDate">The first date in the range.</param>
  /// <param name="endDate">The last date in the range. When omitted, the range has no upper bound.</param>
  /// <param name="cancellationToken">A token that can cancel the request.</param>
  /// <returns>A list of attendance periods ordered by start date and identifier.</returns>
  /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="schoolId"/> is not positive or <paramref name="endDate"/> precedes <paramref name="startDate"/>.</exception>
  /// <exception cref="ObjectDisposedException">Thrown when the client has been disposed.</exception>
  /// <exception cref="HttpRequestException">Thrown when the Bromcom API returns an unsuccessful status code.</exception>
  /// <exception cref="InvalidOperationException">Thrown when the Bromcom API response is invalid or unsuccessful.</exception>
  public async Task<IReadOnlyList<AttendancePeriod>> GetAttendancePeriodsAsync(int schoolId, DateOnly startDate, DateOnly? endDate = null,
    CancellationToken cancellationToken = default)
  {
    ValidateRequest(schoolId);
    ValidateDateRange(startDate, endDate);

    var start = startDate.ToDateTime(TimeOnly.MinValue);
    var entityFilter = endDate is not null
      ? BuildDateRangeEntityFilter("startDate", startDate, endDate.Value)
      : FormattableString.Invariant($"startDate>='{start:yyyy-MM-ddTHH:mm:ss}'");
    var periods = await GetAsync<CalendarContract>("/v2/Calendars", schoolId, entityFilter, null, cancellationToken).ConfigureAwait(false);

    return periods.Select(row => new
    {
      row.CalendarId,
      Start = ParseDateTime(row.StartDate),
      End = ParseDateTime(row.EndDate)
    }).Where(x => x.Start is not null && x.End is not null).Select(x => new AttendancePeriod
    {
      Id = x.CalendarId,
      Start = x.Start.GetValueOrDefault(),
      End = x.End.GetValueOrDefault()
    }).OrderBy(x => x.Start).ThenBy(x => x.Id).ToList();
  }

  /// <summary>Gets morning and afternoon attendance marks for the week containing a date, with an optional student filter.</summary>
  /// <param name="schoolId">The Bromcom school identifier.</param>
  /// <param name="date">A date in the attendance week to retrieve.</param>
  /// <param name="studentIds">Optional student identifiers to filter by.</param>
  /// <param name="cancellationToken">A token that can cancel the request.</param>
  /// <returns>A list of weekly attendance records ordered by student identifier.</returns>
  /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="schoolId"/> is not positive.</exception>
  /// <exception cref="ObjectDisposedException">Thrown when the client has been disposed.</exception>
  /// <exception cref="HttpRequestException">Thrown when the Bromcom API returns an unsuccessful status code.</exception>
  /// <exception cref="InvalidOperationException">Thrown when the Bromcom API response is invalid or unsuccessful.</exception>
  public async Task<IReadOnlyList<StudentWeeklyAttendance>> GetAttendancesByWeekAsync(int schoolId, DateOnly date,
    IList<int>? studentIds = null, CancellationToken cancellationToken = default)
  {
    ValidateRequest(schoolId);

    var dateTime = date.ToDateTime(new TimeOnly(12, 0));
    var entityFilter = FormattableString.Invariant($"startDate<='{dateTime:yyyy-MM-ddTHH:mm:ss}' and endDate>='{dateTime:yyyy-MM-ddTHH:mm:ss}'");
    entityFilter = AppendStudentEntityFilter(entityFilter, studentIds);
    var attendances = await GetAsync<StudentAttendanceByWeekContract>("/v2/StudentAttendanceByWeek", schoolId, entityFilter, null, cancellationToken).ConfigureAwait(false);

    return attendances.Select(row =>
    {
      var attendances = new SessionAttendance[10] {
        BuildSessionAttendance(DayOfWeek.Monday, SessionType.AM, row.MonAM),
        BuildSessionAttendance(DayOfWeek.Monday, SessionType.PM, row.MonPM),
        BuildSessionAttendance(DayOfWeek.Tuesday, SessionType.AM, row.TueAM),
        BuildSessionAttendance(DayOfWeek.Tuesday, SessionType.PM, row.TuePM),
        BuildSessionAttendance(DayOfWeek.Wednesday, SessionType.AM, row.WedAM),
        BuildSessionAttendance(DayOfWeek.Wednesday, SessionType.PM, row.WedPM),
        BuildSessionAttendance(DayOfWeek.Thursday, SessionType.AM, row.ThuAM),
        BuildSessionAttendance(DayOfWeek.Thursday, SessionType.PM, row.ThuPM),
        BuildSessionAttendance(DayOfWeek.Friday, SessionType.AM, row.FriAM),
        BuildSessionAttendance(DayOfWeek.Friday, SessionType.PM, row.FriPM)
      };
      var presentSessions = attendances.Count(a => a.Category is AttendanceCategory.Present or AttendanceCategory.ApprovedEducationalActivity);
      var absentSessions = attendances.Count(a => a.Category is AttendanceCategory.AuthorisedAbsence or AttendanceCategory.UnauthorisedAbsence);
      var totalSessions = presentSessions + absentSessions;
      var percentage = totalSessions > 0 ? Math.Round((decimal)presentSessions / totalSessions * 100, 2) : 0m;
      return new StudentWeeklyAttendance
      {
        StudentId = row.StudentId,
        Attendances = attendances,
        Percentage = percentage
      };
    }).OrderBy(x => x.StudentId).ToList();
  }

  /// <summary>Gets period attendance marks for a date range, with optional period and student filters.</summary>
  /// <param name="schoolId">The Bromcom school identifier.</param>
  /// <param name="startDate">The first date in the range.</param>
  /// <param name="endDate">The last date in the range. When omitted, only <paramref name="startDate"/> is used.</param>
  /// <param name="periodNames">Optional period display names to filter by.</param>
  /// <param name="studentIds">Optional student identifiers to filter by.</param>
  /// <param name="cancellationToken">A token that can cancel the request.</param>
  /// <returns>A list of period attendance marks ordered by student identifier, date, and period name.</returns>
  /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="schoolId"/> is not positive or <paramref name="endDate"/> precedes <paramref name="startDate"/>.</exception>
  /// <exception cref="ObjectDisposedException">Thrown when the client has been disposed.</exception>
  /// <exception cref="HttpRequestException">Thrown when the Bromcom API returns an unsuccessful status code.</exception>
  /// <exception cref="InvalidOperationException">Thrown when the Bromcom API response is invalid or unsuccessful.</exception>
  public async Task<IReadOnlyList<PeriodAttendance>> GetAttendancesAsync(int schoolId, DateOnly startDate, DateOnly? endDate = null, IList<string>? periodNames = null,
    IList<int>? studentIds = null, CancellationToken cancellationToken = default)
  {
    ValidateRequest(schoolId);
    ValidateDateRange(startDate, endDate);

    var entityFilter = BuildBasicAttendanceEntityFilter(startDate, endDate ?? startDate, periodNames, studentIds);
    var attendances = await GetAsync<BasicAttendanceContract>("/v2/BasicAttendance", schoolId, entityFilter, null, cancellationToken).ConfigureAwait(false);

    return attendances.Select(row => new
    {
      row.StudentId,
      Date = ParseDateOnly(row.CalendarStartDate) ?? startDate,
      PeriodName = CleanString(row.PeriodDisplayName),
      Code = CleanString(row.Mark),
      Comment = CleanString(row.AttendanceComment)
    })
      .Where(row => row.PeriodName is not null)
      .Select(row => new PeriodAttendance
      {
        StudentId = row.StudentId,
        Date = row.Date,
        PeriodName = row.PeriodName!,
        Code = row.Code,
        Comment = row.Comment,
        Category = BuildAttendanceCategory(row.Code)
      })
      .OrderBy(x => x.StudentId)
      .ThenBy(x => x.Date)
      .ThenBy(x => x.PeriodName)
      .ToList();
  }

  /// <summary>Sets an attendance mark with the current UTC date and time and a blank nature of activity.</summary>
  /// <param name="schoolId">The Bromcom school identifier.</param>
  /// <param name="studentId">The student identifier.</param>
  /// <param name="calendarId">The attendance period identifier.</param>
  /// <param name="attendanceMark">The attendance mark to record.</param>
  /// <param name="minutesLate">The number of minutes late.</param>
  /// <param name="attendanceComment">The attendance comment.</param>
  /// <param name="attendanceTakenBy">The identifier of the person recording attendance.</param>
  /// <param name="cancellationToken">A token that can cancel the request.</param>
  /// <returns>A task that completes when the attendance has been accepted by the API.</returns>
  /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="schoolId"/> is not positive.</exception>
  /// <exception cref="ObjectDisposedException">Thrown when the client has been disposed.</exception>
  /// <exception cref="HttpRequestException">Thrown when the Bromcom API returns an unsuccessful status code.</exception>
  public async Task SetAttendanceAsync(int schoolId, int studentId, int calendarId,
    string? attendanceMark, int minutesLate, string? attendanceComment, int attendanceTakenBy,
    CancellationToken cancellationToken = default)
  {
    ValidateRequest(schoolId);

    var payload = new AttendancePostContract
    {
      SchoolId = schoolId,
      StudentId = studentId,
      CalendarId = calendarId,
      AttendanceMark = attendanceMark,
      MinutesLate = minutesLate,
      AttendanceComment = attendanceComment,
      AttendanceTakenBy = attendanceTakenBy,
      AttendanceTakenDateTime = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture),
      NatureOfActivity = ""
    };

    await _transport.PostAsync("/v2/Attendances", payload, cancellationToken).ConfigureAwait(false);
  }

  private static SessionAttendance BuildSessionAttendance(DayOfWeek dayOfWeek, SessionType session, string? value)
  {
    var code = string.IsNullOrWhiteSpace(value) ? null : value;
    return new()
    {
      DayOfWeek = dayOfWeek,
      Session = session,
      Code = code,
      Category = BuildAttendanceCategory(code)
    };
  }

  private static AttendanceCategory BuildAttendanceCategory(string? code) => code is null
    ? AttendanceCategory.NotEntered
    : (code[0] switch
    {
      '/' or '\\' or 'L' => AttendanceCategory.Present,
      'B' or 'K' or 'P' or 'V' or 'W' => AttendanceCategory.ApprovedEducationalActivity,
      'C' or 'E' or 'I' or 'J' or 'M' or 'R' or 'S' or 'T' => AttendanceCategory.AuthorisedAbsence,
      'G' or 'N' or 'O' or 'U' => AttendanceCategory.UnauthorisedAbsence,
      'D' or 'Q' or 'X' or 'Y' or 'Z' or '#' => AttendanceCategory.NotPossibleAttendance,
      _ => AttendanceCategory.Invalid
    });
}
