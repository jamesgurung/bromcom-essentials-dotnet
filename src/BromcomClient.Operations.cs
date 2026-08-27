using System.Globalization;

namespace BromcomEssentials;

public partial class BromcomClient
{
  /// <summary>Gets staff absences that overlap a date range.</summary>
  /// <param name="schoolId">The Bromcom school identifier.</param>
  /// <param name="startDate">The first date in the range.</param>
  /// <param name="endDate">The last date in the range. When omitted, only <paramref name="startDate"/> is used.</param>
  /// <param name="cancellationToken">A token that can cancel the request.</param>
  /// <returns>A list of staff absences ordered by start date, employee identifier, and absence identifier.</returns>
  /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="schoolId"/> is not positive or <paramref name="endDate"/> precedes <paramref name="startDate"/>.</exception>
  /// <exception cref="ObjectDisposedException">Thrown when the client has been disposed.</exception>
  /// <exception cref="HttpRequestException">Thrown when the Bromcom API returns an unsuccessful status code.</exception>
  /// <exception cref="InvalidOperationException">Thrown when the Bromcom API response is invalid or unsuccessful.</exception>
  public async Task<IReadOnlyList<StaffAbsence>> GetStaffAbsencesAsync(int schoolId, DateOnly startDate, DateOnly? endDate = null,
    CancellationToken cancellationToken = default)
  {
    ValidateRequest(schoolId);
    ValidateDateRange(startDate, endDate);

    var start = startDate.ToDateTime(TimeOnly.MinValue);
    var end = (endDate ?? startDate).ToDateTime(new TimeOnly(23, 59, 59));
    var entityFilter = FormattableString.Invariant($"startDate<='{end:yyyy-MM-ddTHH:mm:ss}' and (endDate>='{start:yyyy-MM-ddTHH:mm:ss}' or isnull(endDate,'')='')");
    var absences = await GetAsync<StaffAbsenceContract>("/v2/StaffAbsences", schoolId, entityFilter, null, cancellationToken).ConfigureAwait(false);

    return absences.Select(row => new
    {
      Row = row,
      HasStartTime = DateTime.TryParse(row.StartDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var startTime),
      StartTime = startTime,
      EndTime = DateTime.TryParse(row.EndDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var endTime) ? endTime : (DateTime?)null
    }).Where(x => x.HasStartTime).Select(x => new StaffAbsence
    {
      Id = x.Row.StaffAbsenceId,
      EmployeeId = x.Row.EmployeeId,
      Type = CleanString(x.Row.StaffAbsenceCodeDescription),
      Notes = CleanString(x.Row.Notes),
      Duration = x.Row.Duration,
      Start = x.StartTime,
      End = x.EndTime
    }).OrderBy(x => x.Start).ThenBy(x => x.EmployeeId).ThenBy(x => x.Id).ToList();
  }

  /// <summary>Gets student detentions for a date range.</summary>
  /// <param name="schoolId">The Bromcom school identifier.</param>
  /// <param name="startDate">The first date in the range.</param>
  /// <param name="endDate">The last date in the range. When omitted, only <paramref name="startDate"/> is used.</param>
  /// <param name="cancellationToken">A token that can cancel the request.</param>
  /// <returns>A list of detentions ordered by start date, student identifier, and detention identifier.</returns>
  /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="schoolId"/> is not positive or <paramref name="endDate"/> precedes <paramref name="startDate"/>.</exception>
  /// <exception cref="ObjectDisposedException">Thrown when the client has been disposed.</exception>
  /// <exception cref="HttpRequestException">Thrown when the Bromcom API returns an unsuccessful status code.</exception>
  /// <exception cref="InvalidOperationException">Thrown when the Bromcom API response is invalid or unsuccessful.</exception>
  public async Task<IReadOnlyList<Detention>> GetDetentionsAsync(int schoolId, DateOnly startDate, DateOnly? endDate = null,
    CancellationToken cancellationToken = default)
  {
    ValidateRequest(schoolId);
    ValidateDateRange(startDate, endDate);

    var entityFilter = BuildDateRangeEntityFilter("detentionStartDate", startDate, endDate ?? startDate);
    var detentions = await GetAsync<DetentionContract>("/v2/StudentDetentions", schoolId, entityFilter, null, cancellationToken).ConfigureAwait(false);

    return detentions.Select(row => new
    {
      Row = row,
      Start = ParseDateTime(row.DetentionStartDate),
      End = ParseDateTime(row.DetentionEndDate)
    }).Where(x => x.Start is not null).Select(x => new Detention
    {
      Id = x.Row.DetentionId,
      StudentId = x.Row.StudentId,
      Type = CleanString(x.Row.DetentionTypeName),
      Description = CleanString(x.Row.DetentionTypeDescription),
      Start = x.Start.GetValueOrDefault(),
      End = x.End,
      EmployeeId = x.Row.EmployeeId,
      LocationId = x.Row.LocationId,
      Mark = CleanString(x.Row.Mark),
      IsScheduled = ParseBooleanFlag(x.Row.IsDetentionScheduled),
      IsAuthorised = ParseBooleanFlag(x.Row.IsAuthorised),
      IsEscalated = ParseBooleanFlag(x.Row.IsEscalated),
      PeriodName = CleanString(x.Row.PeriodDisplayName),
      EventRecordId = x.Row.EventRecordId,
      Source = CleanString(x.Row.DetentionSource)
    }).OrderBy(x => x.Start).ThenBy(x => x.StudentId).ThenBy(x => x.Id).ToList();
  }

  /// <summary>Gets room cover arrangements for a date range.</summary>
  /// <param name="schoolId">The Bromcom school identifier.</param>
  /// <param name="startDate">The first date in the range.</param>
  /// <param name="endDate">The last date in the range. When omitted, only <paramref name="startDate"/> is used.</param>
  /// <param name="cancellationToken">A token that can cancel the request.</param>
  /// <returns>A list of room cover arrangements ordered by date, period, and cover identifier.</returns>
  /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="schoolId"/> is not positive or <paramref name="endDate"/> precedes <paramref name="startDate"/>.</exception>
  /// <exception cref="ObjectDisposedException">Thrown when the client has been disposed.</exception>
  /// <exception cref="HttpRequestException">Thrown when the Bromcom API returns an unsuccessful status code.</exception>
  /// <exception cref="InvalidOperationException">Thrown when the Bromcom API response is invalid or unsuccessful.</exception>
  public async Task<IReadOnlyList<RoomCover>> GetRoomCoversAsync(int schoolId, DateOnly startDate, DateOnly? endDate = null,
    CancellationToken cancellationToken = default)
  {
    ValidateRequest(schoolId);
    ValidateDateRange(startDate, endDate);

    var entityFilter = BuildDateRangeEntityFilter("coverDate", startDate, endDate ?? startDate);
    var covers = await GetAsync<RoomCoverContract>("/v2/RoomCovers", schoolId, entityFilter, null, cancellationToken).ConfigureAwait(false);

    return covers.Select(row => new { Row = row, Date = ParseDateOnly(row.CoverDate) }).Where(x => x.Date is not null).Select(x => new RoomCover
    {
      Id = x.Row.CoverId,
      Date = x.Date.GetValueOrDefault(),
      PeriodId = CleanString(x.Row.PeriodName),
      Reason = CleanString(x.Row.CoverReasonDescription),
      ClassName = CleanClassName(x.Row.CoveredActivity),
      CoveredRoom = CleanRoom(x.Row.CoveredRoomName),
      CoveringRoom = CleanRoom(x.Row.CoveringRoomName)
    }).OrderBy(x => x.Date).ThenBy(x => x.PeriodId).ThenBy(x => x.Id).ToList();
  }

  /// <summary>Gets staff cover arrangements for a date range.</summary>
  /// <param name="schoolId">The Bromcom school identifier.</param>
  /// <param name="startDate">The first date in the range.</param>
  /// <param name="endDate">The last date in the range. When omitted, only <paramref name="startDate"/> is used.</param>
  /// <param name="cancellationToken">A token that can cancel the request.</param>
  /// <returns>A list of staff cover arrangements ordered by date, period, and cover identifier.</returns>
  /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="schoolId"/> is not positive or <paramref name="endDate"/> precedes <paramref name="startDate"/>.</exception>
  /// <exception cref="ObjectDisposedException">Thrown when the client has been disposed.</exception>
  /// <exception cref="HttpRequestException">Thrown when the Bromcom API returns an unsuccessful status code.</exception>
  /// <exception cref="InvalidOperationException">Thrown when the Bromcom API response is invalid or unsuccessful.</exception>
  public async Task<IReadOnlyList<StaffCover>> GetStaffCoversAsync(int schoolId, DateOnly startDate, DateOnly? endDate = null,
    CancellationToken cancellationToken = default)
  {
    ValidateRequest(schoolId);
    ValidateDateRange(startDate, endDate);

    var entityFilter = BuildDateRangeEntityFilter("coverDate", startDate, endDate ?? startDate);
    var covers = await GetAsync<StaffCoverContract>("/v2/StaffCovers", schoolId, entityFilter, null, cancellationToken).ConfigureAwait(false);

    return covers.Select(row => new { Row = row, Date = ParseDateOnly(row.CoverDate) }).Where(x => x.Date is not null).Select(x => new StaffCover
    {
      Id = x.Row.CoverId,
      Date = x.Date.GetValueOrDefault(),
      PeriodId = CleanString(x.Row.PeriodName),
      Reason = CleanString(x.Row.CoverReasonDescription),
      ClassName = CleanClassName(x.Row.CoveredActivity),
      CoveredStaffId = x.Row.CoveredEmployeeId,
      CoveringStaffId = x.Row.CoveringEmployeeId,
      AbsenceType = CleanString(x.Row.StaffAbsenceType),
      CoverStatus = CleanString(x.Row.CoverStatus)
    }).OrderBy(x => x.Date).ThenBy(x => x.PeriodId).ThenBy(x => x.Id).ToList();
  }

  /// <summary>Gets granted parental consent records for students.</summary>
  /// <param name="schoolId">The Bromcom school identifier.</param>
  /// <param name="consentType">Optional parental consent type to filter by.</param>
  /// <param name="cancellationToken">A token that can cancel the request.</param>
  /// <returns>A list of granted parental consent records ordered by student identifier and consent type.</returns>
  /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="schoolId"/> is not positive.</exception>
  /// <exception cref="ObjectDisposedException">Thrown when the client has been disposed.</exception>
  /// <exception cref="HttpRequestException">Thrown when the Bromcom API returns an unsuccessful status code.</exception>
  /// <exception cref="InvalidOperationException">Thrown when the Bromcom API response is invalid or unsuccessful.</exception>
  public async Task<IReadOnlyList<ParentalConsent>> GetParentalConsentAsync(int schoolId, string? consentType = null,
    CancellationToken cancellationToken = default)
  {
    ValidateRequest(schoolId);

    var filters = new List<string> { "latestConsentStatus='Granted'" };
    if (consentType is not null) filters.Add($"parentalConsentTypeName='{EscapeEntityFilterValue(consentType)}'");
    var consents = await GetAsync<ParentalConsentContract>("/v2/StudentParentalConsent", schoolId, string.Join(" and ", filters), null, cancellationToken).ConfigureAwait(false);

    return consents.Select(row => new ParentalConsent
    {
      StudentId = row.StudentId,
      ConsentType = CleanString(row.ParentalConsentTypeName)
    }).OrderBy(x => x.StudentId).ThenBy(x => x.ConsentType).ToList();
  }

  /// <summary>Gets active behaviour event types.</summary>
  /// <param name="schoolId">The Bromcom school identifier.</param>
  /// <param name="cancellationToken">A token that can cancel the request.</param>
  /// <returns>A list of behaviour event types ordered by name, code, and identifier.</returns>
  /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="schoolId"/> is not positive.</exception>
  /// <exception cref="ObjectDisposedException">Thrown when the client has been disposed.</exception>
  /// <exception cref="HttpRequestException">Thrown when the Bromcom API returns an unsuccessful status code.</exception>
  /// <exception cref="InvalidOperationException">Thrown when the Bromcom API response is invalid or unsuccessful.</exception>
  public async Task<IReadOnlyList<BehaviourType>> GetBehaviourTypesAsync(int schoolId, CancellationToken cancellationToken = default)
  {
    ValidateRequest(schoolId);

    var today = DateTime.Today;
    var entityFilter = FormattableString.Invariant($"startDate<='{today:yyyy-MM-ddTHH:mm:ss}' and (endDate>='{today:yyyy-MM-ddTHH:mm:ss}' or isnull(endDate,'')='')");
    var types = await GetAsync<BehaviourTypeContract>("/v2/BehaviourEvents", schoolId, entityFilter, null, cancellationToken).ConfigureAwait(false);

    return types.Select(row => new BehaviourType
    {
      Id = row.EventId,
      Code = CleanString(row.EventName),
      Name = CleanString(row.EventDescription)
    }).OrderBy(x => x.Name).ThenBy(x => x.Code).ThenBy(x => x.Id).ToList();
  }

  /// <summary>Gets behaviour event records for a date range.</summary>
  /// <param name="schoolId">The Bromcom school identifier.</param>
  /// <param name="startDate">The first date in the range.</param>
  /// <param name="endDate">The last date in the range. When omitted, only <paramref name="startDate"/> is used.</param>
  /// <param name="cancellationToken">A token that can cancel the request.</param>
  /// <returns>A list of behaviour event records ordered by date, student identifier, and event identifier.</returns>
  /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="schoolId"/> is not positive or <paramref name="endDate"/> precedes <paramref name="startDate"/>.</exception>
  /// <exception cref="ObjectDisposedException">Thrown when the client has been disposed.</exception>
  /// <exception cref="HttpRequestException">Thrown when the Bromcom API returns an unsuccessful status code.</exception>
  /// <exception cref="InvalidOperationException">Thrown when the Bromcom API response is invalid or unsuccessful.</exception>
  public async Task<IReadOnlyList<BehaviourEvent>> GetBehaviourEventsAsync(int schoolId, DateOnly startDate, DateOnly? endDate = null,
    CancellationToken cancellationToken = default)
  {
    ValidateRequest(schoolId);
    ValidateDateRange(startDate, endDate);

    var entityFilter = BuildDateRangeEntityFilter("eventDate", startDate, endDate ?? startDate);
    var events = await GetAsync<BehaviourEventContract>("/v2/BehaviourEventRecords", schoolId, entityFilter, null, cancellationToken).ConfigureAwait(false);

    return events.Select(row => new { Row = row, DateTime = ParseDateTime(row.EventDate) }).Where(x => x.DateTime is not null).Select(x => new BehaviourEvent
    {
      Id = x.Row.EventRecordId,
      StudentId = x.Row.StudentId,
      EventTypeId = x.Row.EventId,
      StaffId = x.Row.OwnerId,
      ClassId = x.Row.ClassId,
      LocationId = x.Row.LocationId,
      Date = x.DateTime.GetValueOrDefault(),
      Points = x.Row.Adjustment,
      Comment = CleanString(x.Row.Comment),
      InternalComment = CleanString(x.Row.InternalComment)
    }).OrderBy(x => x.Date).ThenBy(x => x.StudentId).ThenBy(x => x.Id).ToList();
  }

  /// <summary>Creates or updates a behaviour event record.</summary>
  /// <param name="schoolId">The Bromcom school identifier.</param>
  /// <param name="ev">The behaviour event to send to Bromcom.</param>
  /// <param name="cancellationToken">A token that can cancel the request.</param>
  /// <returns>A task that completes when the event has been accepted by the API.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="ev"/> is <see langword="null"/>.</exception>
  /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="schoolId"/> is not positive.</exception>
  /// <exception cref="ObjectDisposedException">Thrown when the client has been disposed.</exception>
  /// <exception cref="HttpRequestException">Thrown when the Bromcom API returns an unsuccessful status code.</exception>
  public async Task SetBehaviourEventAsync(int schoolId, BehaviourEvent ev, CancellationToken cancellationToken = default)
  {
    ValidateRequest(schoolId);
    ArgumentNullException.ThrowIfNull(ev);

    var eventDate = ev.Date == default ? DateTime.UtcNow : ev.Date;
    if (eventDate.Kind == DateTimeKind.Local) eventDate = eventDate.ToUniversalTime();
    if (eventDate.Kind == DateTimeKind.Unspecified) eventDate = DateTime.SpecifyKind(eventDate, DateTimeKind.Utc);

    var payload = new BehaviourEventPostContract
    {
      SchoolId = schoolId,
      StudentId = ev.StudentId,
      EventRecordId = ev.Id,
      EventId = ev.EventTypeId,
      OwnerId = ev.StaffId,
      ClassId = ev.ClassId ?? 0,
      LocationId = ev.LocationId ?? 0,
      EventDate = eventDate.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture),
      Adjustment = ev.Points,
      Comment = ev.Comment,
      InternalComment = ev.InternalComment
    };

    await _transport.PostAsync("/v2/BehaviourEventRecords", payload, cancellationToken).ConfigureAwait(false);
  }

  /// <summary>Gets departments, subjects, and department staff membership.</summary>
  /// <param name="schoolId">The Bromcom school identifier.</param>
  /// <param name="cancellationToken">A token that can cancel the request.</param>
  /// <returns>A list of departments ordered by name.</returns>
  /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="schoolId"/> is not positive.</exception>
  /// <exception cref="ObjectDisposedException">Thrown when the client has been disposed.</exception>
  /// <exception cref="HttpRequestException">Thrown when the Bromcom API returns an unsuccessful status code.</exception>
  /// <exception cref="InvalidOperationException">Thrown when the Bromcom API response is invalid or unsuccessful.</exception>
  public async Task<IReadOnlyList<Department>> GetDepartmentsAsync(int schoolId, CancellationToken cancellationToken = default)
  {
    ValidateRequest(schoolId);

    var departmentSubjects = await GetAsync<DepartmentContract>("/v2/Departments", schoolId, null, null, cancellationToken).ConfigureAwait(false);
    var departmentTeachers = await GetAsync<DepartmentTeacherContract>("/v2/DepartmentTeachers", schoolId, null, null, cancellationToken).ConfigureAwait(false);
    var subjects = await GetAsync<SubjectContract>("/v2/Subjects", schoolId, null, null, cancellationToken).ConfigureAwait(false);

    var subjectsById = subjects.ToDictionary(s => s.SubjectId);
    var teachersByDepartmentId = departmentTeachers.ToLookup(t => t.DepartmentId);

    return departmentSubjects.GroupBy(row => row.DepartmentId).Select(g => new Department
    {
      Id = g.Key,
      Name = CleanString(g.First()?.CollectionName),
      Subjects = g.Where(r => subjectsById.ContainsKey(r.SubjectId)).DistinctBy(r => r.SubjectId).Select(r =>
      {
        var subject = subjectsById[r.SubjectId];
        return new Subject { Id = subject.SubjectId, Name = CleanString(subject.SubjectName), Code = CleanString(subject.Abbreviation) };
      }).ToList(),
      HeadOfDepartmentId = teachersByDepartmentId[g.Key]
        .FirstOrDefault(t => string.Equals(CleanString(t.CollectionRoleTypeDescription), "Head of Department", StringComparison.OrdinalIgnoreCase))?.PersonId,
      LeaderIds = teachersByDepartmentId[g.Key]
        .Where(t => string.Equals(CleanString(t.CollectionRoleTypeDescription), "Head of Department", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(CleanString(t.CollectionRoleTypeDescription), "Deputy HoD", StringComparison.OrdinalIgnoreCase)).Select(t => t.PersonId).Distinct().ToList(),
      TeacherIds = teachersByDepartmentId[g.Key].Select(t => t.PersonId).Distinct().ToList()
    }).OrderBy(d => d.Name).ToList();
  }

  /// <summary>Gets assessment results for an academic year, with optional term and year group filters.</summary>
  /// <param name="schoolId">The Bromcom school identifier.</param>
  /// <param name="academicYearStart">The calendar year in which the academic year starts.</param>
  /// <param name="term">Optional term name to filter by.</param>
  /// <param name="yearGroup">Optional year group to filter by.</param>
  /// <param name="gradesOnly">Whether to return only grade results.</param>
  /// <param name="cancellationToken">A token that can cancel the request.</param>
  /// <returns>A list of assessment results ordered by student, year group, term, subject, and result.</returns>
  /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="schoolId"/> is not positive or <paramref name="academicYearStart"/> is outside 1900–9998.</exception>
  /// <exception cref="ObjectDisposedException">Thrown when the client has been disposed.</exception>
  /// <exception cref="HttpRequestException">Thrown when the Bromcom API returns an unsuccessful status code.</exception>
  /// <exception cref="InvalidOperationException">Thrown when the Bromcom API response is invalid or unsuccessful.</exception>
  public async Task<IReadOnlyList<AssessmentResult>> GetResultsAsync(int schoolId, int academicYearStart, string? term = null, int? yearGroup = null,
    bool gradesOnly = false,
    CancellationToken cancellationToken = default)
  {
    ValidateRequest(schoolId);
    if (academicYearStart is < 1900 or > 9998)
      throw new ArgumentOutOfRangeException(nameof(academicYearStart), "Academic year start must be between 1900 and 9998.");

    var entityFilter = BuildAssessmentResultsEntityFilter(academicYearStart, term, yearGroup, gradesOnly);
    var results = await GetAsync<AssessmentResultContract>("/v2/AssociationAssessmentResultsRaw", schoolId, entityFilter, null, cancellationToken).ConfigureAwait(false);

    return results.Where(row => !string.IsNullOrWhiteSpace(row.AssessmentTypeName) && !string.IsNullOrWhiteSpace(row.Result)).Select(row => new AssessmentResult
    {
      StudentId = row.StudentId,
      Type = CleanString(row.AssessmentTypeName)!,
      YearGroup = ParseNullableInt(row.YearGroupName),
      Term = CleanString(row.TermName),
      Subject = CleanString(row.SubjectName),
      Result = CleanString(row.Result)!
    }).OrderBy(r => r.StudentId).ThenBy(r => r.YearGroup).ThenBy(r => r.Term).ThenBy(r => r.Subject).ThenBy(r => r.Result).ToList();
  }
}
