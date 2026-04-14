using System.Globalization;
using System.Text.Json;

namespace BromcomEssentials;

public class BromcomClient : IDisposable
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
        g => g.Select(x => new StudentClass { Name = CleanString(x.ClassName)!, Subject = CleanString(x.SubjectDescription) })
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
            .Select(x => new StudentTimetableEntry { Period = x.WeekDayPeriod, Class = x.ClassName, Room = CleanRoom(x.LocationName), TeacherCode = x.StaffCode }).ToList());
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

  public async Task<IReadOnlyList<Staff>> GetStaffAsync(int schoolId, bool includeClassesAndTimetable = false, CancellationToken cancellationToken = default)
  {
    ObjectDisposedException.ThrowIf(_disposed, this);

    var staff = await GetAsync<StaffContract>("/v2/Staff", schoolId, null, null, cancellationToken);
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
          g => g.Select(x => CleanString(x.ClassName)!).Distinct(StringComparer.OrdinalIgnoreCase).ToList());
      timetableByStaffId = timetableRows
        .Where(x => !string.IsNullOrWhiteSpace(x.WeekDayPeriod) && !string.IsNullOrWhiteSpace(x.TimetableEntry))
        .GroupBy(x => x.StaffId)
        .ToDictionary(
          g => g.Key,
          g => g.OrderBy(x => x.PeriodStartDate).GroupBy(x => x.WeekDayPeriod, StringComparer.OrdinalIgnoreCase).Select(g => g.First())
            .Select(x => new StaffTimetableEntry { Period = x.WeekDayPeriod, Class = CleanString(x.TimetableEntry), Room = CleanRoom(x.LocationName)}).ToList());
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
      Timetable = timetableByStaffId.TryGetValue(row.StaffId, out var timetable) ? timetable : [],
      Classes = classesByStaffId.TryGetValue(row.StaffId, out var classes) ? classes : []
    }).OrderBy(s => s.Surname).ThenBy(s => s.Forename).ThenBy(s => s.StaffCode).ToList();
  }

  public async Task<IReadOnlyList<Department>> GetDepartmentsAsync(int schoolId, CancellationToken cancellationToken = default)
  {
    ObjectDisposedException.ThrowIf(_disposed, this);

    var departmentSubjects = await GetAsync<DepartmentContract>("/v2/Departments", schoolId, null, null, cancellationToken);
    var departmentTeachers = await GetAsync<DepartmentTeacherContract>("/v2/DepartmentTeachers", schoolId, null, null, cancellationToken);
    var subjects = await GetAsync<SubjectContract>("/v2/Subjects", schoolId, null, null, cancellationToken);

    var subjectCodesById = subjects.Where(s => !string.IsNullOrWhiteSpace(s.Abbreviation)).ToDictionary(s => s.SubjectId, s => CleanString(s.Abbreviation));
    var teachersByDepartmentId = departmentTeachers.ToLookup(t => t.DepartmentId);

    return departmentSubjects.GroupBy(row => row.DepartmentId).Select(g => new Department
    {
      Id = g.Key,
      Name = CleanString(g.First()?.CollectionName),
      SubjectCodes = g.Where(r => subjectCodesById.ContainsKey(r.SubjectId)).Select(r => subjectCodesById[r.SubjectId]!).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
      HeadOfDepartmentId = teachersByDepartmentId[g.Key]
        .FirstOrDefault(t => string.Equals(CleanString(t.CollectionRoleTypeDescription), "Head of Department", StringComparison.OrdinalIgnoreCase))?.PersonId,
      LeaderIds = teachersByDepartmentId[g.Key]
        .Where(t => string.Equals(CleanString(t.CollectionRoleTypeDescription), "Head of Department", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(CleanString(t.CollectionRoleTypeDescription), "Deputy HoD", StringComparison.OrdinalIgnoreCase)).Select(t => t.PersonId).Distinct().ToList(),
      TeacherIds = teachersByDepartmentId[g.Key].Select(t => t.PersonId).Distinct().ToList()
    }).OrderBy(d => d.Name).ToList();
  }

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

  public void Dispose()
  {
    if (_disposed) return;
    if (_ownsHttpClient) _httpClient.Dispose();
    _disposed = true;
    GC.SuppressFinalize(this);
  }
}
