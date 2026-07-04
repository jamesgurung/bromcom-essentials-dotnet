using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BromcomEssentials;

/// <summary>Client for retrieving basic data from the Bromcom Partner API.</summary>
public partial class BromcomClient : IDisposable
{
  private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
  {
    PropertyNameCaseInsensitive = true,
    TypeInfoResolver = BromcomJsonContext.Default
  };

  private readonly HttpClient _httpClient;
  private readonly bool _ownsHttpClient;
  private readonly string _applicationId;
  private readonly string _applicationSecret;
  private bool _disposed;

  /// <summary>Initialises a new instance of the <see cref="BromcomClient"/> class.</summary>
  /// <param name="applicationId">The Bromcom Partner API application ID.</param>
  /// <param name="applicationSecret">The Bromcom Partner API application secret.</param>
  /// <param name="httpClient">Optional HTTP client to use for requests. When omitted, the client creates and owns one.</param>
  /// <exception cref="ArgumentException">Thrown when <paramref name="applicationId"/> or <paramref name="applicationSecret"/> is blank.</exception>
  public BromcomClient(string applicationId, string applicationSecret, HttpClient? httpClient = null)
  {
    _ownsHttpClient = httpClient is null;
    _httpClient = httpClient ?? new();
    _applicationId = string.IsNullOrWhiteSpace(applicationId)
      ? throw new ArgumentException("Application ID is required.", nameof(applicationId))
      : applicationId;
    _applicationSecret = string.IsNullOrWhiteSpace(applicationSecret)
      ? throw new ArgumentException("Application secret is required.", nameof(applicationSecret))
      : applicationSecret;
  }

  /// <summary>Gets students for a school, with optional class and timetable data.</summary>
  /// <param name="schoolId">The Bromcom school identifier.</param>
  /// <param name="includeClasses">Whether to include class memberships for each student.</param>
  /// <param name="includeTimetable">Whether to include timetable entries for each student.</param>
  /// <param name="cancellationToken">A token that can cancel the request.</param>
  /// <returns>A list of students ordered by surname, forename, year group, and tutor group.</returns>
  /// <exception cref="ObjectDisposedException">Thrown when the client has been disposed.</exception>
  /// <exception cref="HttpRequestException">Thrown when the Bromcom API returns an unsuccessful status code.</exception>
  /// <exception cref="InvalidOperationException">Thrown when the Bromcom API response is invalid or unsuccessful.</exception>
  public async Task<IReadOnlyList<Student>> GetStudentsAsync(int schoolId, bool includeClasses = false, bool includeTimetable = false,
    CancellationToken cancellationToken = default)
  {
    ObjectDisposedException.ThrowIf(_disposed, this);

    var students = await GetAsync<StudentFlatViewContract>("/v2/StudentFlatView", schoolId, null, "enrolment", cancellationToken);
    var classesByStudentId = new Dictionary<int, List<StudentClass>>();
    var timetableByStudentId = new Dictionary<int, List<StudentTimetableEntry>>();

    if (includeClasses)
    {
      var classes = await GetAsync<YearGroupSubjectStudentContract>("/v2/YearGroupSubjectStudents", schoolId, null, null, cancellationToken);
      classesByStudentId = classes.Where(x => !string.IsNullOrWhiteSpace(x.ClassName)).GroupBy(x => x.StudentId).ToDictionary(
        g => g.Key,
        g => g.Select(x => new StudentClass { Name = CleanClassName(x.ClassName)!, Subject = CleanString(x.SubjectDescription) })
          .DistinctBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList());
    }

    if (includeTimetable)
    {
      var timetableRows = await GetTimetableRowsAsync(schoolId, true, cancellationToken);
      timetableByStudentId = timetableRows
        .Where(x => !string.IsNullOrWhiteSpace(x.WeekDayPeriod) && !string.IsNullOrWhiteSpace(x.ClassName) && x.StudentId is not null)
        .GroupBy(x => x.StudentId!.Value)
        .ToDictionary(
          g => g.Key,
          g => g.OrderBy(x => x.PeriodStartDate).GroupBy(x => x.WeekDayPeriod, StringComparer.OrdinalIgnoreCase).Select(g => g.First())
            .Select(x => new StudentTimetableEntry { Period = x.WeekDayPeriod, Class = CleanClassName(x.ClassName), Room = CleanRoom(x.LocationName), TeacherCode = x.StaffCode }).ToList());
    }

    return students.DistinctBy(row => row.StudentId).Select(row => new Student
    {
      Id = row.StudentId,
      Forename = CleanString(row.PreferredFirstName) ?? CleanString(row.FirstName),
      Surname = CleanString(row.PreferredLastName) ?? CleanString(row.LastName),
      Gender = CleanString(row.GenderCode),
      DateOfBirth = row.DateOfBirth is null || row.DateOfBirth.Length < 10
        ? null : (DateOnly.TryParseExact(row.DateOfBirth[..10], "yyyy-MM-dd", out var dob) ? dob : null),
      Email = CleanString(row.StudentEmail)?.ToLowerInvariant(),
      Upn = CleanString(row.Upn),
      ExamNumber = ParseNullableInt(row.ExamNumber),
      AdmissionNumber = ParseNullableInt(row.AdmissionNumber),
      EthnicCode = CleanString(row.EthnicityCode),
      SendStatusCode = CleanString(row.ProvisionName),
      IsGiftedAndTalented = ParseBooleanFlag(row.GntFlag),
      IsFsmEver6 = ParseBooleanFlag(row.EverFsm6Flag),
      IsLookedAfter = ParseBooleanFlag(row.InCareFlag),
      IsEal = ParseBooleanFlag(row.EalFlag),
      IsPupilPremium = row.PremiumPupilFlag == true,
      EnrolmentStatus = ParseEnrolmentStatus(row.EnrolmentStateName),
      Attendance = row.PresentPercentageWithEA,
      YearGroup = ParseNullableInt(row.YearGroup),
      TutorGroup = CleanTutorGroup(row.TutorGroupName),
      Parents = BuildParentContacts(row),
      Classes = classesByStudentId.TryGetValue(row.StudentId, out var classes) ? classes : [],
      Timetable = timetableByStudentId.TryGetValue(row.StudentId, out var timetable) ? timetable : []
    }).OrderBy(s => s.Surname).ThenBy(s => s.Forename).ThenBy(s => s.YearGroup).ThenBy(s => s.TutorGroup).ToList();
  }

  /// <summary>Gets staff for a school, with optional class and timetable data.</summary>
  /// <param name="schoolId">The Bromcom school identifier.</param>
  /// <param name="includeClassesAndTimetable">Whether to include class names and timetable entries for each staff member.</param>
  /// <param name="cancellationToken">A token that can cancel the request.</param>
  /// <returns>A list of staff ordered by surname, forename, and staff code.</returns>
  /// <exception cref="ObjectDisposedException">Thrown when the client has been disposed.</exception>
  /// <exception cref="HttpRequestException">Thrown when the Bromcom API returns an unsuccessful status code.</exception>
  /// <exception cref="InvalidOperationException">Thrown when the Bromcom API response is invalid or unsuccessful.</exception>
  public async Task<IReadOnlyList<Staff>> GetStaffAsync(int schoolId, bool includeClassesAndTimetable = false, CancellationToken cancellationToken = default)
  {
    ObjectDisposedException.ThrowIf(_disposed, this);

    var staff = await GetAsync<StaffContract>("/v2/Staff", schoolId, null, null, cancellationToken);
    var lineManagers = await GetAsync<StaffLineManagerContract>("/v2/StaffLineManagers", schoolId, null, null, cancellationToken);
    var lineManagerIdsByStaffId = lineManagers.GroupBy(row => row.EmployeeId).ToDictionary(g => g.Key, g => g.First().LineManagerEmployeeId);
    var classesByStaffId = new Dictionary<int, List<string>>();
    var timetableByStaffId = new Dictionary<int, List<StaffTimetableEntry>>();

    if (includeClassesAndTimetable)
    {
      var timetableRows = await GetTimetableRowsAsync(schoolId, false, cancellationToken);
      classesByStaffId = timetableRows
        .Where(x => !string.IsNullOrWhiteSpace(x.ClassName))
        .GroupBy(x => x.StaffId)
        .ToDictionary(
          g => g.Key,
          g => g.Select(x => CleanClassName(x.ClassName)!).Distinct(StringComparer.OrdinalIgnoreCase).ToList());
      timetableByStaffId = timetableRows
        .Where(x => !string.IsNullOrWhiteSpace(x.WeekDayPeriod) && !string.IsNullOrWhiteSpace(x.TimetableEntry))
        .GroupBy(x => x.StaffId)
        .ToDictionary(
          g => g.Key,
          g => g.OrderBy(x => x.PeriodStartDate).GroupBy(x => x.WeekDayPeriod, StringComparer.OrdinalIgnoreCase).Select(g => g.First())
            .Select(x => new StaffTimetableEntry { Period = x.WeekDayPeriod, Class = CleanClassName(x.TimetableEntry), Room = CleanRoom(x.LocationName)}).ToList());
    }

    return staff.Where(row => !string.IsNullOrWhiteSpace(row.StaffCode)).DistinctBy(row => row.StaffId).Select(row => new Staff
    {
      Id = row.StaffId,
      Title = CleanString(row.Title),
      Forename = CleanString(row.PreferredFirstName) ?? CleanString(row.FirstName),
      Surname = CleanString(row.PreferredLastName) ?? CleanString(row.LastName),
      Email = CleanString(row.WorkEmail)?.ToLowerInvariant(),
      StaffCode = CleanString(row.StaffCode),
      JobTitle = CleanString(row.JobTitle),
      LineManagerId = lineManagerIdsByStaffId.TryGetValue(row.StaffId, out var lineManagerId) ? lineManagerId : null,
      Timetable = timetableByStaffId.TryGetValue(row.StaffId, out var timetable) ? timetable : [],
      Classes = classesByStaffId.TryGetValue(row.StaffId, out var classes) ? classes : []
    }).OrderBy(s => s.Surname).ThenBy(s => s.Forename).ThenBy(s => s.StaffCode).ToList();
  }

  /// <summary>Gets staff absences that overlap a date range.</summary>
  /// <param name="schoolId">The Bromcom school identifier.</param>
  /// <param name="startDate">The first date in the range.</param>
  /// <param name="endDate">The last date in the range. When omitted, only <paramref name="startDate"/> is used.</param>
  /// <param name="cancellationToken">A token that can cancel the request.</param>
  /// <returns>A list of staff absences ordered by start date, employee identifier, and absence identifier.</returns>
  /// <exception cref="ObjectDisposedException">Thrown when the client has been disposed.</exception>
  /// <exception cref="HttpRequestException">Thrown when the Bromcom API returns an unsuccessful status code.</exception>
  /// <exception cref="InvalidOperationException">Thrown when the Bromcom API response is invalid or unsuccessful.</exception>
  public async Task<IReadOnlyList<StaffAbsence>> GetStaffAbsencesAsync(int schoolId, DateOnly startDate, DateOnly? endDate = null,
    CancellationToken cancellationToken = default)
  {
    ObjectDisposedException.ThrowIf(_disposed, this);

    var start = startDate.ToDateTime(TimeOnly.MinValue);
    var end = (endDate ?? startDate).ToDateTime(new TimeOnly(23, 59, 59));
    var entityFilter = $"startDate<='{end:yyyy-MM-ddTHH:mm:ss}' and (endDate>='{start:yyyy-MM-ddTHH:mm:ss}' or isnull(endDate,'')='')";
    var absences = await GetAsync<StaffAbsenceContract>("/v2/StaffAbsences", schoolId, entityFilter, null, cancellationToken);

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

  /// <summary>Gets room cover arrangements for a date range.</summary>
  /// <param name="schoolId">The Bromcom school identifier.</param>
  /// <param name="startDate">The first date in the range.</param>
  /// <param name="endDate">The last date in the range. When omitted, only <paramref name="startDate"/> is used.</param>
  /// <param name="cancellationToken">A token that can cancel the request.</param>
  /// <returns>A list of room cover arrangements ordered by date, period, and cover identifier.</returns>
  /// <exception cref="ObjectDisposedException">Thrown when the client has been disposed.</exception>
  /// <exception cref="HttpRequestException">Thrown when the Bromcom API returns an unsuccessful status code.</exception>
  /// <exception cref="InvalidOperationException">Thrown when the Bromcom API response is invalid or unsuccessful.</exception>
  public async Task<IReadOnlyList<RoomCover>> GetRoomCoversAsync(int schoolId, DateOnly startDate, DateOnly? endDate = null,
    CancellationToken cancellationToken = default)
  {
    ObjectDisposedException.ThrowIf(_disposed, this);

    var entityFilter = BuildDateRangeEntityFilter("coverDate", startDate, endDate ?? startDate);
    var covers = await GetAsync<RoomCoverContract>("/v2/RoomCovers", schoolId, entityFilter, null, cancellationToken);

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
  /// <exception cref="ObjectDisposedException">Thrown when the client has been disposed.</exception>
  /// <exception cref="HttpRequestException">Thrown when the Bromcom API returns an unsuccessful status code.</exception>
  /// <exception cref="InvalidOperationException">Thrown when the Bromcom API response is invalid or unsuccessful.</exception>
  public async Task<IReadOnlyList<StaffCover>> GetStaffCoversAsync(int schoolId, DateOnly startDate, DateOnly? endDate = null,
    CancellationToken cancellationToken = default)
  {
    ObjectDisposedException.ThrowIf(_disposed, this);

    var entityFilter = BuildDateRangeEntityFilter("coverDate", startDate, endDate ?? startDate);
    var covers = await GetAsync<StaffCoverContract>("/v2/StaffCovers", schoolId, entityFilter, null, cancellationToken);

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
  /// <exception cref="ObjectDisposedException">Thrown when the client has been disposed.</exception>
  /// <exception cref="HttpRequestException">Thrown when the Bromcom API returns an unsuccessful status code.</exception>
  /// <exception cref="InvalidOperationException">Thrown when the Bromcom API response is invalid or unsuccessful.</exception>
  public async Task<IReadOnlyList<ParentalConsent>> GetParentalConsentAsync(int schoolId, string? consentType = null,
    CancellationToken cancellationToken = default)
  {
    ObjectDisposedException.ThrowIf(_disposed, this);

    var filters = new List<string> { "latestConsentStatus='Granted'" };
    if (consentType is not null) filters.Add($"parentalConsentTypeName='{EscapeEntityFilterValue(consentType)}'");
    var consents = await GetAsync<ParentalConsentContract>("/v2/StudentParentalConsent", schoolId, string.Join(" and ", filters), null, cancellationToken);

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
  /// <exception cref="ObjectDisposedException">Thrown when the client has been disposed.</exception>
  /// <exception cref="HttpRequestException">Thrown when the Bromcom API returns an unsuccessful status code.</exception>
  /// <exception cref="InvalidOperationException">Thrown when the Bromcom API response is invalid or unsuccessful.</exception>
  public async Task<IReadOnlyList<BehaviourType>> GetBehaviourTypesAsync(int schoolId, CancellationToken cancellationToken = default)
  {
    ObjectDisposedException.ThrowIf(_disposed, this);

    var today = DateTime.Today;
    var entityFilter = $"startDate<='{today:yyyy-MM-ddTHH:mm:ss}' and (endDate>='{today:yyyy-MM-ddTHH:mm:ss}' or isnull(endDate,'')='')";
    var types = await GetAsync<BehaviourTypeContract>("/v2/BehaviourEvents", schoolId, entityFilter, null, cancellationToken);

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
  /// <exception cref="ObjectDisposedException">Thrown when the client has been disposed.</exception>
  /// <exception cref="HttpRequestException">Thrown when the Bromcom API returns an unsuccessful status code.</exception>
  /// <exception cref="InvalidOperationException">Thrown when the Bromcom API response is invalid or unsuccessful.</exception>
  public async Task<IReadOnlyList<BehaviourEvent>> GetBehaviourEventsAsync(int schoolId, DateOnly startDate, DateOnly? endDate = null,
    CancellationToken cancellationToken = default)
  {
    ObjectDisposedException.ThrowIf(_disposed, this);

    var entityFilter = BuildDateRangeEntityFilter("eventDate", startDate, endDate ?? startDate);
    var events = await GetAsync<BehaviourEventContract>("/v2/BehaviourEventRecords", schoolId, entityFilter, null, cancellationToken);

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
  /// <exception cref="ObjectDisposedException">Thrown when the client has been disposed.</exception>
  /// <exception cref="HttpRequestException">Thrown when the Bromcom API returns an unsuccessful status code.</exception>
  public async Task SetBehaviourEventAsync(int schoolId, BehaviourEvent ev, CancellationToken cancellationToken = default)
  {
    ObjectDisposedException.ThrowIf(_disposed, this);
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

    using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.bromcomcloud.com/v2/BehaviourEventRecords");
    request.Headers.Add("ApplicationId", _applicationId);
    request.Headers.Add("ApplicationSecret", _applicationSecret);
    request.Headers.Add("Accept", "application/json");
    request.Content = JsonContent.Create(payload, options: _jsonOptions);

    using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    if (!response.IsSuccessStatusCode)
      throw new HttpRequestException($"Request to '/v2/BehaviourEventRecords' failed with status {(int)response.StatusCode} ({response.StatusCode}).");
  }

  /// <summary>Gets departments, subjects, and department staff membership.</summary>
  /// <param name="schoolId">The Bromcom school identifier.</param>
  /// <param name="cancellationToken">A token that can cancel the request.</param>
  /// <returns>A list of departments ordered by name.</returns>
  /// <exception cref="ObjectDisposedException">Thrown when the client has been disposed.</exception>
  /// <exception cref="HttpRequestException">Thrown when the Bromcom API returns an unsuccessful status code.</exception>
  /// <exception cref="InvalidOperationException">Thrown when the Bromcom API response is invalid or unsuccessful.</exception>
  public async Task<IReadOnlyList<Department>> GetDepartmentsAsync(int schoolId, CancellationToken cancellationToken = default)
  {
    ObjectDisposedException.ThrowIf(_disposed, this);

    var departmentSubjects = await GetAsync<DepartmentContract>("/v2/Departments", schoolId, null, null, cancellationToken);
    var departmentTeachers = await GetAsync<DepartmentTeacherContract>("/v2/DepartmentTeachers", schoolId, null, null, cancellationToken);
    var subjects = await GetAsync<SubjectContract>("/v2/Subjects", schoolId, null, null, cancellationToken);

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
  /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="academicYearStart"/> is less than 1900.</exception>
  /// <exception cref="ObjectDisposedException">Thrown when the client has been disposed.</exception>
  /// <exception cref="HttpRequestException">Thrown when the Bromcom API returns an unsuccessful status code.</exception>
  /// <exception cref="InvalidOperationException">Thrown when the Bromcom API response is invalid or unsuccessful.</exception>
  public async Task<IReadOnlyList<AssessmentResult>> GetResultsAsync(int schoolId, int academicYearStart, string? term = null, int? yearGroup = null,
    bool gradesOnly = false,
    CancellationToken cancellationToken = default)
  {
    ObjectDisposedException.ThrowIf(_disposed, this);
    if (academicYearStart < 1900) throw new ArgumentOutOfRangeException(nameof(academicYearStart), "Academic year start must be a valid year.");

    var entityFilter = BuildAssessmentResultsEntityFilter(academicYearStart, term, yearGroup, gradesOnly);
    var results = await GetAsync<AssessmentResultContract>("/v2/AssociationAssessmentResultsRaw", schoolId, entityFilter, null, cancellationToken);

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

  /// <summary>Gets morning and afternoon attendance marks for the week containing a date.</summary>
  /// <param name="schoolId">The Bromcom school identifier.</param>
  /// <param name="date">A date in the attendance week to retrieve.</param>
  /// <param name="cancellationToken">A token that can cancel the request.</param>
  /// <returns>A list of weekly attendance records ordered by student identifier.</returns>
  /// <exception cref="ObjectDisposedException">Thrown when the client has been disposed.</exception>
  /// <exception cref="HttpRequestException">Thrown when the Bromcom API returns an unsuccessful status code.</exception>
  /// <exception cref="InvalidOperationException">Thrown when the Bromcom API response is invalid or unsuccessful.</exception>
  public async Task<IReadOnlyList<StudentWeeklyAttendance>> GetAttendancesByWeekAsync(int schoolId, DateOnly date, CancellationToken cancellationToken = default)
  {
    ObjectDisposedException.ThrowIf(_disposed, this);

    var dateTime = date.ToDateTime(new TimeOnly(12, 0));
    var entityFilter = $"startDate<='{dateTime:yyyy-MM-ddTHH:mm:ss}' and endDate>='{dateTime:yyyy-MM-ddTHH:mm:ss}'";
    var attendances = await GetAsync<StudentAttendanceByWeekContract>("/v2/StudentAttendanceByWeek", schoolId, entityFilter, null, cancellationToken);

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
  /// <param name="periodName">Optional period display name to filter by.</param>
  /// <param name="studentIds">Optional student identifiers to filter by.</param>
  /// <param name="cancellationToken">A token that can cancel the request.</param>
  /// <returns>A list of period attendance marks ordered by student identifier, date, and period name.</returns>
  /// <exception cref="ObjectDisposedException">Thrown when the client has been disposed.</exception>
  /// <exception cref="HttpRequestException">Thrown when the Bromcom API returns an unsuccessful status code.</exception>
  /// <exception cref="InvalidOperationException">Thrown when the Bromcom API response is invalid or unsuccessful.</exception>
  public async Task<IReadOnlyList<PeriodAttendance>> GetAttendancesAsync(int schoolId, DateOnly startDate, DateOnly? endDate = null, string? periodName = null,
    IList<int>? studentIds = null, CancellationToken cancellationToken = default)
  {
    ObjectDisposedException.ThrowIf(_disposed, this);

    var entityFilter = BuildBasicAttendanceEntityFilter(startDate, endDate ?? startDate, periodName, studentIds);
    var attendances = await GetAsync<BasicAttendanceContract>("/v2/BasicAttendance", schoolId, entityFilter, null, cancellationToken);

    return attendances.Select(row => new
    {
      row.StudentId,
      Date = row.CalendarStartDate is null || row.CalendarStartDate.Length < 10
        ? startDate : (DateOnly.TryParseExact(row.CalendarStartDate[..10], "yyyy-MM-dd", out var date) ? date : startDate),
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

  private static List<ParentContact> BuildParentContacts(StudentFlatViewContract student)
  {
    var parents = new List<ParentContact>(3);
    TryAddParent(parents, student.Contact1ParentalResponsibility, student.Contact1Name, student.Contact1Telephone, student.Contact1Email, student.Contact1Relationship);
    TryAddParent(parents, student.Contact2ParentalResponsibility, student.Contact2Name, student.Contact2Telephone, student.Contact2Email, student.Contact2Relationship);
    TryAddParent(parents, student.Contact3ParentalResponsibility, student.Contact3Name, student.Contact3Telephone, student.Contact3Email, student.Contact3Relationship);
    return parents;
  }

  private static void TryAddParent(List<ParentContact> parents, string? parentalResponsibility, string? name, string? telephone, string? email, string? relationship)
  {
    if (!ParseBooleanFlag(parentalResponsibility)) return;
    parents.Add(new ParentContact
    {
      Name = CleanParentName(name),
      Telephone = CleanTelephone(telephone),
      Email = CleanString(email)?.ToLowerInvariant(),
      Relationship = CleanString(relationship)
    });
  }

  private async Task<List<TimetableContract>> GetTimetableRowsAsync(int schoolId, bool isStudents, CancellationToken cancellationToken)
  {
    var timetableRows = new List<TimetableContract>();
    var daysFound = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var rowCount = 0;
    var today = DateTime.UtcNow.Date;
    var maxDate = today.AddDays(35);
    var endpoint = isStudents ? "/v2/StudentTimetables" : "/v2/Timetable";

    for (var windowStart = today; windowStart <= maxDate; windowStart = windowStart.AddDays(7))
    {
      var windowEnd = windowStart.AddDays(7);
      var entityFilter = $"periodStartDate>='{windowStart:yyyy-MM-dd}' and periodStartDate<'{windowEnd:yyyy-MM-dd}'" +
        (isStudents ? string.Empty : " and isCover=0");
      var windowRows = await GetAsync<TimetableContract>(endpoint, schoolId, entityFilter, null, cancellationToken);
      
      var daysInWindow = windowRows.Select(x => x.WeekDayPeriod).Distinct(StringComparer.OrdinalIgnoreCase).Where(x => x is not null && x.Length >= 7)
        .Select(x => x![..7]).Distinct(StringComparer.OrdinalIgnoreCase);

      var newDaysAdded = false;
      foreach (var day in daysInWindow)
      {
        if (daysFound.Add(day)) newDaysAdded = true;
      }
      timetableRows.AddRange(windowRows);
      if (rowCount > 0 && !newDaysAdded && daysFound.Count >= 5) break;
      rowCount = timetableRows.Count;
    }

    return timetableRows;
  }

  private async Task<List<TModel>> GetAsync<TModel>(string path, int schoolId, string? entityFilter, string? dateFieldPrefix, CancellationToken cancellationToken = default)
  {
    ObjectDisposedException.ThrowIf(_disposed, this);
    if (string.IsNullOrWhiteSpace(path) || !path.StartsWith('/', StringComparison.Ordinal)) throw new ArgumentException("Path must start with '/'.", nameof(path));

    entityFilter ??= BuildDefaultEntityFilter(dateFieldPrefix);
    var results = new List<TModel>();
    var page = 0;
    List<TModel> data;

    do
    {
      var url = $"https://api.bromcomcloud.com{path}?schoolId={schoolId}&entityFilter={Uri.EscapeDataString(entityFilter)}{{page:{page}}}";
      using var request = new HttpRequestMessage(HttpMethod.Get, url);
      request.Headers.Add("ApplicationId", _applicationId);
      request.Headers.Add("ApplicationSecret", _applicationSecret);
      request.Headers.Add("Accept", "application/json");

      using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
      var body = await response.Content.ReadAsStringAsync(cancellationToken);

      if (!response.IsSuccessStatusCode) throw new HttpRequestException($"Request to '{path}' failed with status {(int)response.StatusCode} ({response.StatusCode}).");

      ApiResponse<TModel>? payload;
      try
      {
        payload = JsonSerializer.Deserialize<ApiResponse<TModel>>(body, _jsonOptions);
      }
      catch (JsonException ex)
      {
        throw new InvalidOperationException($"Invalid JSON returned from '{path}'.", ex);
      }
      if (payload is null) throw new InvalidOperationException($"Empty response payload from '{path}'.");
      if (!payload.Success) throw new InvalidOperationException($"API returned success=false for '{path}'.");
      if (payload.Data is null) throw new InvalidOperationException($"Response data was null for '{path}'.");

      data = payload.Data;
      results.AddRange(data);
      page++;
    } while (data.Count == 100000);

    return results;
  }

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
      $"calendarStartDate>='{start:yyyy-MM-ddTHH:mm:ss}'",
      $"calendarStartDate<='{end:yyyy-MM-ddTHH:mm:ss}'"
    };

    if (periodName is not null) filters.Add($"periodDisplayName='{EscapeEntityFilterValue(periodName)}'");
    if (studentIds is { Count: > 0 }) filters.Add($"({string.Join(" or ", studentIds.Distinct().Select(id => $"studentID={id.ToString(CultureInfo.InvariantCulture)}"))})");

    return string.Join(" and ", filters);
  }

  private static string BuildDateRangeEntityFilter(string fieldName, DateOnly startDate, DateOnly endDate)
  {
    var start = startDate.ToDateTime(TimeOnly.MinValue);
    var end = endDate.ToDateTime(new TimeOnly(23, 59, 59));
    return $"{fieldName}>='{start:yyyy-MM-ddTHH:mm:ss}' and {fieldName}<='{end:yyyy-MM-ddTHH:mm:ss}'";
  }

  private static string BuildAssessmentResultsEntityFilter(int academicYearStart, string? term, int? yearGroup, bool gradesOnly)
  {
    var start = new DateOnly(academicYearStart, 9, 1);
    var endExclusive = new DateOnly(academicYearStart + 1, 9, 1);
    var filters = new List<string>
    {
      $"enteredDate>='{start:yyyy-MM-dd}'",
      $"enteredDate<'{endExclusive:yyyy-MM-dd}'"
    };

    if (term is not null) filters.Add($"termName='{EscapeEntityFilterValue(term)}'");
    if (yearGroup is not null) filters.Add($"yearGroupName='{yearGroup.Value.ToString(CultureInfo.InvariantCulture)}'");
    if (gradesOnly) filters.Add("isGrade='True'");

    return string.Join(" and ", filters);
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
    if (cleaned is null) return null;
    return ClassNameYearSuffixRegex().Replace(cleaned, string.Empty);
  }

  [GeneratedRegex(@"\s+\(\d{2}/\d{2}\)$", RegexOptions.CultureInvariant)]
  private static partial Regex ClassNameYearSuffixRegex();

  /// <summary>Releases the owned HTTP client, if this instance created one.</summary>
  public void Dispose()
  {
    if (_disposed) return;
    if (_ownsHttpClient) _httpClient.Dispose();
    _disposed = true;
    GC.SuppressFinalize(this);
  }
}
