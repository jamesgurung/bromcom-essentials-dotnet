namespace BromcomEssentials;

public partial class BromcomClient
{
  /// <summary>Gets person and photo identifiers for a school.</summary>
  /// <param name="schoolId">The Bromcom school identifier.</param>
  /// <param name="cancellationToken">A token that can cancel the request.</param>
  /// <returns>A list of people with photo identifiers.</returns>
  /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="schoolId"/> is not positive.</exception>
  /// <exception cref="ObjectDisposedException">Thrown when the client has been disposed.</exception>
  /// <exception cref="HttpRequestException">Thrown when the Bromcom API returns an unsuccessful status code.</exception>
  /// <exception cref="InvalidOperationException">Thrown when the Bromcom API response is invalid or unsuccessful.</exception>
  public async Task<List<PersonPhotoId>> GetPhotoIdsAsync(int schoolId, CancellationToken cancellationToken = default)
  {
    ValidateRequest(schoolId);

    var photos = await GetAsync<PersonPhotoContract>("/v2/PersonPhotos", schoolId, string.Empty, null, cancellationToken).ConfigureAwait(false);
    return photos.Where(row => row.PersonId is not null && !string.IsNullOrWhiteSpace(row.Photo))
      .Select(row => new PersonPhotoId { PersonId = row.PersonId!.Value, PhotoId = row.Photo! }).ToList();
  }

  /// <summary>Gets the image for an encrypted photo identifier.</summary>
  /// <param name="photoId">The encrypted photo identifier.</param>
  /// <param name="cancellationToken">A token that can cancel the request.</param>
  /// <returns>The image bytes and content type returned by Bromcom.</returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="photoId"/> is blank.</exception>
  /// <exception cref="ObjectDisposedException">Thrown when the client has been disposed.</exception>
  /// <exception cref="HttpRequestException">Thrown when the image request returns an unsuccessful status code.</exception>
  public Task<PersonPhoto> GetPhotoAsync(string photoId, CancellationToken cancellationToken = default)
  {
    ObjectDisposedException.ThrowIf(_disposed, this);
    ArgumentException.ThrowIfNullOrWhiteSpace(photoId);
    return _transport.GetPhotoAsync(photoId, cancellationToken);
  }

  /// <summary>Gets students for a school, with optional class and timetable data.</summary>
  /// <param name="schoolId">The Bromcom school identifier.</param>
  /// <param name="includeClasses">Whether to include class memberships for each student.</param>
  /// <param name="includeTimetable">Whether to include timetable entries for each student.</param>
  /// <param name="cancellationToken">A token that can cancel the request.</param>
  /// <returns>A list of students ordered by surname, forename, year group, and tutor group.</returns>
  /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="schoolId"/> is not positive.</exception>
  /// <exception cref="ObjectDisposedException">Thrown when the client has been disposed.</exception>
  /// <exception cref="HttpRequestException">Thrown when the Bromcom API returns an unsuccessful status code.</exception>
  /// <exception cref="InvalidOperationException">Thrown when the Bromcom API response is invalid or unsuccessful.</exception>
  public async Task<IReadOnlyList<Student>> GetStudentsAsync(int schoolId, bool includeClasses = false, bool includeTimetable = false,
    CancellationToken cancellationToken = default)
  {
    ValidateRequest(schoolId);

    var students = await GetAsync<StudentFlatViewContract>("/v2/StudentFlatView", schoolId, null, "enrolment", cancellationToken).ConfigureAwait(false);
    var classesByStudentId = new Dictionary<int, List<StudentClass>>();
    var timetableByStudentId = new Dictionary<int, List<StudentTimetableEntry>>();

    if (includeClasses)
    {
      var classes = await GetAsync<YearGroupSubjectStudentContract>("/v2/YearGroupSubjectStudents", schoolId, null, null, cancellationToken).ConfigureAwait(false);
      classesByStudentId = classes.Where(x => !string.IsNullOrWhiteSpace(x.ClassName)).GroupBy(x => x.StudentId).ToDictionary(
        g => g.Key,
        g => g.Select(x => new StudentClass { Name = CleanClassName(x.ClassName)!, Subject = CleanString(x.SubjectDescription) })
          .DistinctBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList());
    }

    if (includeTimetable)
    {
      var timetableRows = await GetTimetableRowsAsync(schoolId, true, cancellationToken).ConfigureAwait(false);
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
      DateOfBirth = ParseDateOnly(row.DateOfBirth),
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
  /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="schoolId"/> is not positive.</exception>
  /// <exception cref="ObjectDisposedException">Thrown when the client has been disposed.</exception>
  /// <exception cref="HttpRequestException">Thrown when the Bromcom API returns an unsuccessful status code.</exception>
  /// <exception cref="InvalidOperationException">Thrown when the Bromcom API response is invalid or unsuccessful.</exception>
  public async Task<IReadOnlyList<Staff>> GetStaffAsync(int schoolId, bool includeClassesAndTimetable = false, CancellationToken cancellationToken = default)
  {
    ValidateRequest(schoolId);

    var staff = await GetAsync<StaffContract>("/v2/Staff", schoolId, null, null, cancellationToken).ConfigureAwait(false);
    var lineManagers = await GetAsync<StaffLineManagerContract>("/v2/StaffLineManagers", schoolId, null, null, cancellationToken).ConfigureAwait(false);
    var lineManagerIdsByStaffId = lineManagers.GroupBy(row => row.EmployeeId).ToDictionary(g => g.Key, g => g.First().LineManagerEmployeeId);
    var classesByStaffId = new Dictionary<int, List<string>>();
    var timetableByStaffId = new Dictionary<int, List<StaffTimetableEntry>>();

    if (includeClassesAndTimetable)
    {
      var timetableRows = await GetTimetableRowsAsync(schoolId, false, cancellationToken).ConfigureAwait(false);
      classesByStaffId = timetableRows
        .Where(x => x.StaffId is not null && !string.IsNullOrWhiteSpace(x.ClassName))
        .GroupBy(x => x.StaffId!.Value)
        .ToDictionary(
          g => g.Key,
          g => g.Select(x => CleanClassName(x.ClassName)!).Distinct(StringComparer.OrdinalIgnoreCase).ToList());
      timetableByStaffId = timetableRows
        .Where(x => x.StaffId is not null && !string.IsNullOrWhiteSpace(x.WeekDayPeriod) && !string.IsNullOrWhiteSpace(x.TimetableEntry))
        .GroupBy(x => x.StaffId!.Value)
        .ToDictionary(
          g => g.Key,
          g => g.OrderBy(x => x.PeriodStartDate).GroupBy(x => x.WeekDayPeriod, StringComparer.OrdinalIgnoreCase).Select(g => g.First())
            .Select(x => new StaffTimetableEntry { Period = x.WeekDayPeriod, Class = CleanClassName(x.TimetableEntry), Room = CleanRoom(x.LocationName) }).ToList());
    }

    return staff.Where(row => !string.IsNullOrWhiteSpace(row.StaffCode)).DistinctBy(row => row.StaffId).Select(row => new Staff
    {
      Id = row.StaffId,
      Title = CleanString(row.Title),
      Forename = CleanString(row.PreferredFirstName) ?? CleanString(row.FirstName),
      Surname = CleanString(row.PreferredLastName) ?? CleanString(row.LastName),
      Email = CleanString(row.WorkEmail)?.ToLowerInvariant(),
      TelephoneExtension = row.ExtensionNo,
      StaffCode = CleanString(row.StaffCode),
      JobTitle = CleanString(row.JobTitle),
      LineManagerId = lineManagerIdsByStaffId.TryGetValue(row.StaffId, out var lineManagerId) ? lineManagerId : null,
      Timetable = timetableByStaffId.TryGetValue(row.StaffId, out var timetable) ? timetable : [],
      Classes = classesByStaffId.TryGetValue(row.StaffId, out var classes) ? classes : []
    }).OrderBy(s => s.Surname).ThenBy(s => s.Forename).ThenBy(s => s.StaffCode).ToList();
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
      var entityFilter = FormattableString.Invariant($"periodStartDate>='{windowStart:yyyy-MM-dd}' and periodStartDate<'{windowEnd:yyyy-MM-dd}'") +
        (isStudents ? string.Empty : " and isCover=0");
      var windowRows = await GetAsync<TimetableContract>(endpoint, schoolId, entityFilter, null, cancellationToken).ConfigureAwait(false);

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
}
