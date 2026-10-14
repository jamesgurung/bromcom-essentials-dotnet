namespace BromcomEssentials;

public partial class BromcomClient
{
  /// <summary>Gets clubs that are active today.</summary>
  /// <param name="schoolId">The Bromcom school identifier.</param>
  /// <param name="cancellationToken">A token that can cancel the request.</param>
  /// <returns>A list of active clubs ordered by name and identifier.</returns>
  /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="schoolId"/> is not positive.</exception>
  /// <exception cref="ObjectDisposedException">Thrown when the client has been disposed.</exception>
  /// <exception cref="HttpRequestException">Thrown when the Bromcom API returns an unsuccessful status code.</exception>
  /// <exception cref="InvalidOperationException">Thrown when the Bromcom API response is invalid or unsuccessful.</exception>
  public async Task<IReadOnlyList<Club>> GetClubsAsync(int schoolId, CancellationToken cancellationToken = default)
  {
    ValidateRequest(schoolId);

    return await GetClubsCoreAsync(schoolId, null, cancellationToken).ConfigureAwait(false);
  }

  /// <summary>Gets clubs that are active at any point in an inclusive date range.</summary>
  /// <param name="schoolId">The Bromcom school identifier.</param>
  /// <param name="startDate">The first date in the range.</param>
  /// <param name="endDate">The last date in the range.</param>
  /// <param name="cancellationToken">A token that can cancel the request.</param>
  /// <returns>A list of active clubs ordered by name and identifier.</returns>
  /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="schoolId"/> is not positive or <paramref name="endDate"/> precedes <paramref name="startDate"/>.</exception>
  /// <exception cref="ObjectDisposedException">Thrown when the client has been disposed.</exception>
  /// <exception cref="HttpRequestException">Thrown when the Bromcom API returns an unsuccessful status code.</exception>
  /// <exception cref="InvalidOperationException">Thrown when the Bromcom API response is invalid or unsuccessful.</exception>
  public async Task<IReadOnlyList<Club>> GetClubsAsync(int schoolId, DateOnly startDate, DateOnly endDate,
    CancellationToken cancellationToken = default)
  {
    ValidateRequest(schoolId);
    ValidateDateRange(startDate, endDate);

    var start = startDate.ToDateTime(TimeOnly.MinValue);
    var end = endDate.ToDateTime(new TimeOnly(23, 59, 59));
    var entityFilter = FormattableString.Invariant($"clubStartDate<='{end:yyyy-MM-ddTHH:mm:ss}' and (clubEndDate>='{start:yyyy-MM-ddTHH:mm:ss}' or isnull(clubEndDate,'')='')");
    return await GetClubsCoreAsync(schoolId, entityFilter, cancellationToken).ConfigureAwait(false);
  }

  private async Task<IReadOnlyList<Club>> GetClubsCoreAsync(int schoolId, string? entityFilter, CancellationToken cancellationToken)
  {
    var clubs = await GetAsync<ClubDetailsContract>("/v2/ClubDetails", schoolId, entityFilter, "club", cancellationToken).ConfigureAwait(false);

    return clubs.Select(row => new { Row = row, StartDate = ParseDateOnly(row.ClubStartDate) })
      .Where(x => x.StartDate is not null)
      .Select(x => new Club
      {
        Id = x.Row.CollectionId,
        Name = CleanString(x.Row.ClubName),
        Description = CleanString(x.Row.ClubDescription),
        StartDate = x.StartDate.GetValueOrDefault(),
        EndDate = ParseDateOnly(x.Row.ClubEndDate),
        Time = CleanString(x.Row.ClubTime),
        DayOfWeek = Enum.TryParse<DayOfWeek>(x.Row.DayOfWeek, true, out var dayOfWeek) ? dayOfWeek : null,
        Length = ParseNullableInt(x.Row.ClubLength),
        ReservedSpaces = x.Row.ReservedSpaces,
        MembershipLimit = x.Row.MembershipLimit,
        McasLiveFrom = ParseDateTime(x.Row.WhenLiveOnMcas),
        McasLiveUntil = ParseDateTime(x.Row.LiveOnMcasEndDate),
        StaffName = CleanString(x.Row.ClubStaffFullName),
        Room = CleanRoom(x.Row.ClubRoom),
        AssociatedGroups = x.Row.AssociatedGroupName?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList() ?? [],
        IsWaitingListEnabled = x.Row.IsWaitingListEnabledBoolen,
        IsTrip = x.Row.IsTripBoolen
      })
      .OrderBy(x => x.Name)
      .ThenBy(x => x.Id)
      .ToList();
  }

  /// <summary>Gets club attendance records for a date range, with an optional student filter.</summary>
  /// <param name="schoolId">The Bromcom school identifier.</param>
  /// <param name="startDate">The first attendance date in the range.</param>
  /// <param name="endDate">The last attendance date in the range. When omitted, only <paramref name="startDate"/> is used.</param>
  /// <param name="studentIds">Optional student identifiers to filter by.</param>
  /// <param name="cancellationToken">A token that can cancel the request.</param>
  /// <returns>A list of club attendance records ordered by club identifier, student identifier, and attendance date.</returns>
  /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="schoolId"/> is not positive or <paramref name="endDate"/> precedes <paramref name="startDate"/>.</exception>
  /// <exception cref="ObjectDisposedException">Thrown when the client has been disposed.</exception>
  /// <exception cref="HttpRequestException">Thrown when the Bromcom API returns an unsuccessful status code.</exception>
  /// <exception cref="InvalidOperationException">Thrown when the Bromcom API response is invalid or unsuccessful.</exception>
  public async Task<IReadOnlyList<ClubStudentAttendance>> GetClubAttendancesAsync(int schoolId, DateOnly startDate, DateOnly? endDate = null,
    IList<int>? studentIds = null, CancellationToken cancellationToken = default)
  {
    ValidateRequest(schoolId);
    ValidateDateRange(startDate, endDate);

    var entityFilter = BuildDateRangeEntityFilter("attendanceDate", startDate, endDate ?? startDate);
    entityFilter = AppendStudentEntityFilter(entityFilter, studentIds);
    var attendances = await GetAsync<ClubStudentAttendanceContract>("/v2/ClubStudentsAndAttendance", schoolId, entityFilter, null, cancellationToken).ConfigureAwait(false);

    return attendances.Select(row => new { Row = row, MembershipStartDate = ParseDateOnly(row.StudentClubMembershipStartDate) })
      .Where(x => x.MembershipStartDate is not null)
      .Select(x => new ClubStudentAttendance
      {
        StudentId = x.Row.StudentId,
        ClubId = x.Row.ClubId,
        MembershipStartDate = x.MembershipStartDate.GetValueOrDefault(),
        MembershipEndDate = ParseDateOnly(x.Row.StudentClubMembershipEndDate),
        Mark = CleanString(x.Row.Mark),
        AttendanceDate = ParseDateOnly(x.Row.AttendanceDate)
      })
      .OrderBy(x => x.ClubId)
      .ThenBy(x => x.StudentId)
      .ThenBy(x => x.AttendanceDate)
      .ToList();
  }
}
