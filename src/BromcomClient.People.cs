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
  /// <returns>The image bytes returned by Bromcom.</returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="photoId"/> is blank.</exception>
  /// <exception cref="ObjectDisposedException">Thrown when the client has been disposed.</exception>
  /// <exception cref="HttpRequestException">Thrown when the image request returns an unsuccessful status code.</exception>
  public Task<byte[]> GetPhotoAsync(string photoId, CancellationToken cancellationToken = default)
  {
    ObjectDisposedException.ThrowIf(_disposed, this);
    ArgumentException.ThrowIfNullOrWhiteSpace(photoId);
    return _transport.GetPhotoAsync(photoId, cancellationToken);
  }

  /// <summary>Gets student documents, with optional creation date and student filters.</summary>
  /// <param name="schoolId">The Bromcom school identifier.</param>
  /// <param name="startDate">Optional inclusive creation start date, with no upper bound.</param>
  /// <param name="studentIds">Optional student identifiers to filter by. Null or empty includes all students.</param>
  /// <param name="cancellationToken">A token that can cancel the request.</param>
  /// <returns>A list of documents ordered by student identifier, creation date, and document identifier.</returns>
  /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="schoolId"/> is not positive.</exception>
  /// <exception cref="ObjectDisposedException">Thrown when the client has been disposed.</exception>
  /// <exception cref="HttpRequestException">Thrown when the Bromcom API returns an unsuccessful status code.</exception>
  /// <exception cref="InvalidOperationException">Thrown when the Bromcom API response is invalid or unsuccessful.</exception>
  public async Task<IReadOnlyList<StudentDocument>> GetStudentDocumentsAsync(int schoolId, DateOnly? startDate = null,
    IList<int>? studentIds = null, CancellationToken cancellationToken = default)
  {
    ValidateRequest(schoolId);

    var filter = startDate is DateOnly start ? FormattableString.Invariant($"createdDate>='{start:yyyy-MM-dd}T00:00:00'") : string.Empty;
    var entityFilter = AppendStudentEntityFilter(filter, studentIds);
    var documents = await GetAsync<StudentDocumentContract>("/v2/StudentDocumentObjects", schoolId, entityFilter, null, cancellationToken).ConfigureAwait(false);

    return documents.Select(row => new StudentDocument
    {
      Id = row.ObjectId,
      StudentId = row.StudentId,
      Name = CleanString(row.DocumentName),
      Type = CleanString(row.DocumentType),
      FileName = CleanString(row.FileName),
      CreatedDate = ParseDateTime(row.CreatedDate),
      OwnerId = row.OwnerId
    }).OrderBy(x => x.StudentId).ThenBy(x => x.CreatedDate).ThenBy(x => x.Id).ToList();
  }

  /// <summary>Gets the file content for a student document.</summary>
  /// <param name="schoolId">The Bromcom school identifier.</param>
  /// <param name="documentId">The document object identifier returned by <see cref="GetStudentDocumentsAsync"/>.</param>
  /// <param name="cancellationToken">A token that can cancel the request.</param>
  /// <returns>The decoded document bytes.</returns>
  /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="schoolId"/> or <paramref name="documentId"/> is not positive.</exception>
  /// <exception cref="ObjectDisposedException">Thrown when the client has been disposed.</exception>
  /// <exception cref="HttpRequestException">Thrown when the Bromcom API returns an unsuccessful status code.</exception>
  /// <exception cref="InvalidOperationException">Thrown when the Bromcom API response is invalid or contains no content.</exception>
  public Task<byte[]> GetStudentDocumentContentAsync(int schoolId, int documentId, CancellationToken cancellationToken = default)
  {
    ValidateRequest(schoolId);
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(documentId);
    return _transport.GetStudentDocumentContentAsync(schoolId, documentId, cancellationToken);
  }

  /// <summary>Gets student communication logs, with optional student, date, and direction filters.</summary>
  /// <param name="schoolId">The Bromcom school identifier.</param>
  /// <param name="studentId">Optional student identifier to filter by.</param>
  /// <param name="startDate">Optional inclusive contact start date, with no upper bound.</param>
  /// <param name="direction">Optional communication direction to filter by.</param>
  /// <param name="cancellationToken">A token that can cancel the request.</param>
  /// <returns>A list of communication logs ordered by student identifier and timestamp.</returns>
  /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="schoolId"/> is not positive.</exception>
  /// <exception cref="ObjectDisposedException">Thrown when the client has been disposed.</exception>
  /// <exception cref="HttpRequestException">Thrown when the Bromcom API returns an unsuccessful status code.</exception>
  /// <exception cref="InvalidOperationException">Thrown when the Bromcom API response is invalid or unsuccessful.</exception>
  public async Task<IReadOnlyList<StudentCommunicationLog>> GetStudentCommunicationLogsAsync(int schoolId, int? studentId = null,
    DateOnly? startDate = null, CommunicationLogDirection? direction = null, CancellationToken cancellationToken = default)
  {
    ValidateRequest(schoolId);

    var filters = new List<string>();
    if (studentId is int id) filters.Add(FormattableString.Invariant($"studentID={id}"));
    if (startDate is DateOnly start) filters.Add(FormattableString.Invariant($"contactedOn>='{start:yyyy-MM-dd}T00:00:00'"));
    if (direction is not null) filters.Add($"direction='{direction}'");

    var logs = await GetAsync<StudentCommunicationLogContract>("/v2/StudentCommunicationLogs", schoolId, string.Join(" and ", filters), null, cancellationToken).ConfigureAwait(false);

    return logs.Select(row => new StudentCommunicationLog
    {
      StudentId = row.StudentId,
      ContactName = CleanString(row.ContactName),
      ContactId = row.ContactId,
      RelationType = CleanString(row.RelationType),
      StaffId = row.EmployeeId,
      StaffName = CleanString(row.ContactedBy),
      Timestamp = ParseDateTime(row.ContactedOn),
      Direction = CleanString(row.Direction)?.ToUpperInvariant() switch
      {
        "OUTBOUND" => CommunicationLogDirection.Outbound,
        "INBOUND" => CommunicationLogDirection.Inbound,
        _ => null
      },
      Message = CleanString(row.Reason),
      Outcome = CleanString(row.Outcome),
      Notes = CleanString(row.Notes)
    }).OrderBy(x => x.StudentId).ThenBy(x => x.Timestamp).ToList();
  }

  /// <summary>Gets medical conditions, excluding blank and NON condition names, with an optional person filter.</summary>
  /// <param name="schoolId">The Bromcom school identifier.</param>
  /// <param name="personIds">Optional person identifiers to filter by. Null or empty includes all people.</param>
  /// <param name="cancellationToken">A token that can cancel the request.</param>
  /// <returns>A list of medical conditions ordered by person identifier, description, and type.</returns>
  /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="schoolId"/> is not positive.</exception>
  /// <exception cref="ObjectDisposedException">Thrown when the client has been disposed.</exception>
  /// <exception cref="HttpRequestException">Thrown when the Bromcom API returns an unsuccessful status code.</exception>
  /// <exception cref="InvalidOperationException">Thrown when the Bromcom API response is invalid or unsuccessful.</exception>
  public async Task<IReadOnlyList<PersonMedicalCondition>> GetMedicalConditionsAsync(int schoolId, IList<int>? personIds = null,
    CancellationToken cancellationToken = default)
  {
    ValidateRequest(schoolId);

    var entityFilter = AppendStudentEntityFilter(string.Empty, personIds, "personID");
    var conditions = await GetAsync<PersonMedicalConditionContract>("/v2/PersonMedicalConditions", schoolId, entityFilter, null, cancellationToken).ConfigureAwait(false);

    return conditions.Where(row => CleanString(row.MedicalConditionName) is string name && !name.Equals("NON", StringComparison.OrdinalIgnoreCase))
      .Select(row => new PersonMedicalCondition
      {
        PersonId = row.PersonId,
        Description = CleanString(row.MedicalConditionDescription),
        Type = CleanString(row.MedicalConditionTypeDescription),
        Notes = CleanString(row.MedicalConditionRemark),
        InformationReceivedOn = ParseDateTime(row.InformationReceivedOn)
      }).OrderBy(x => x.PersonId).ThenBy(x => x.Description).ThenBy(x => x.Type).ToList();
  }

  /// <summary>Gets dietary needs with an optional person filter.</summary>
  /// <param name="schoolId">The Bromcom school identifier.</param>
  /// <param name="personIds">Optional person identifiers to filter by. Null or empty includes all people.</param>
  /// <param name="cancellationToken">A token that can cancel the request.</param>
  /// <returns>A list of dietary needs ordered by person identifier and description.</returns>
  /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="schoolId"/> is not positive.</exception>
  /// <exception cref="ObjectDisposedException">Thrown when the client has been disposed.</exception>
  /// <exception cref="HttpRequestException">Thrown when the Bromcom API returns an unsuccessful status code.</exception>
  /// <exception cref="InvalidOperationException">Thrown when the Bromcom API response is invalid or unsuccessful.</exception>
  public async Task<IReadOnlyList<PersonDietaryNeed>> GetDietaryNeedsAsync(int schoolId, IList<int>? personIds = null,
    CancellationToken cancellationToken = default)
  {
    ValidateRequest(schoolId);

    var entityFilter = AppendStudentEntityFilter(string.Empty, personIds, "personID");
    var needs = await GetAsync<PersonDietaryNeedContract>("/v2/PersonDietaryNeeds", schoolId, entityFilter, null, cancellationToken).ConfigureAwait(false);

    return needs.Select(row => new PersonDietaryNeed
    {
      PersonId = row.PersonId,
      Description = CleanString(row.DietaryNeedDescription),
      Notes = CleanString(row.DietaryNeedRemark),
      InformationReceivedOn = ParseDateTime(row.InformationReceivedOn)
    }).OrderBy(x => x.PersonId).ThenBy(x => x.Description).ToList();
  }

  /// <summary>Gets disabilities with an optional person filter.</summary>
  /// <param name="schoolId">The Bromcom school identifier.</param>
  /// <param name="personIds">Optional person identifiers to filter by. Null or empty includes all people.</param>
  /// <param name="cancellationToken">A token that can cancel the request.</param>
  /// <returns>A list of disabilities ordered by person identifier and description.</returns>
  /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="schoolId"/> is not positive.</exception>
  /// <exception cref="ObjectDisposedException">Thrown when the client has been disposed.</exception>
  /// <exception cref="HttpRequestException">Thrown when the Bromcom API returns an unsuccessful status code.</exception>
  /// <exception cref="InvalidOperationException">Thrown when the Bromcom API response is invalid or unsuccessful.</exception>
  public async Task<IReadOnlyList<PersonDisability>> GetDisabilitiesAsync(int schoolId, IList<int>? personIds = null,
    CancellationToken cancellationToken = default)
  {
    ValidateRequest(schoolId);

    var entityFilter = AppendStudentEntityFilter(string.Empty, personIds, "personID");
    var disabilities = await GetAsync<PersonDisabilityContract>("/v2/Disabilities", schoolId, entityFilter, null, cancellationToken).ConfigureAwait(false);

    return disabilities.Select(row => new PersonDisability
    {
      PersonId = row.PersonId,
      Description = CleanString(row.DisabilityDescription)
    }).OrderBy(x => x.PersonId).ThenBy(x => x.Description).ToList();
  }

  /// <summary>Gets students for a school, including contacts, imported KS2 results, classes, and timetables.</summary>
  /// <param name="schoolId">The Bromcom school identifier.</param>
  /// <param name="cancellationToken">A token that can cancel the request.</param>
  /// <returns>A list of students ordered by surname, forename, year group, and tutor group.</returns>
  /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="schoolId"/> is not positive.</exception>
  /// <exception cref="ObjectDisposedException">Thrown when the client has been disposed.</exception>
  /// <exception cref="HttpRequestException">Thrown when the Bromcom API returns an unsuccessful status code.</exception>
  /// <exception cref="InvalidOperationException">Thrown when the Bromcom API response is invalid or unsuccessful.</exception>
  public async Task<IReadOnlyList<Student>> GetStudentsAsync(int schoolId, CancellationToken cancellationToken = default)
  {
    ValidateRequest(schoolId);

    var students = await GetAsync<StudentFlatViewContract>("/v2/StudentFlatView", schoolId, null, "enrolment", cancellationToken).ConfigureAwait(false);
    if (students.Count == 0) return [];

    var studentIds = students.Select(row => row.StudentId).ToHashSet();
    var studentFilter = BuildIdRangeFilter("studentID", studentIds);
    var contactsTask = GetStudentContactsByStudentIdAsync(schoolId, studentIds, studentFilter, cancellationToken);
    var assessmentsTask = GetAsync<StudentImportedCtfAssessmentContract>("/v2/StudentImportedCtfAssessments", schoolId,
      $"stage='KS2' and (component='MSS' or component='RSS') and {studentFilter}", null, cancellationToken);
    var classesTask = GetAsync<YearGroupSubjectStudentContract>("/v2/YearGroupSubjectStudents", schoolId,
      $"{BuildDefaultEntityFilter(null)} and {studentFilter}", null, cancellationToken);
    var timetableTask = GetTimetableRowsAsync(schoolId, cancellationToken, studentIds);
    await Task.WhenAll(contactsTask, assessmentsTask, classesTask, timetableTask).ConfigureAwait(false);

    var contactsByStudentId = await contactsTask.ConfigureAwait(false);
    var assessments = await assessmentsTask.ConfigureAwait(false);
    var assessmentResults = assessments.Where(row => studentIds.Contains(row.StudentId) && !string.IsNullOrWhiteSpace(row.Result))
      .GroupBy(row => (row.StudentId, row.Component))
      .ToDictionary(g => g.Key, g => CleanString(g.OrderByDescending(row => row.ResultDate).First().Result));
    var classes = await classesTask.ConfigureAwait(false);
    var classesByStudentId = classes.Where(x => studentIds.Contains(x.StudentId) && !string.IsNullOrWhiteSpace(x.ClassName)).GroupBy(x => x.StudentId).ToDictionary(
      g => g.Key,
      g => g.Select(x => new StudentClass { Name = CleanClassName(x.ClassName)!, Subject = CleanString(x.SubjectDescription) })
        .DistinctBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList());

    var timetableRows = await timetableTask.ConfigureAwait(false);
    var timetableByStudentId = timetableRows
      .Where(x => !string.IsNullOrWhiteSpace(x.WeekDayPeriod) && !string.IsNullOrWhiteSpace(x.ClassName) && x.StudentId is not null)
      .GroupBy(x => x.StudentId!.Value)
      .ToDictionary(
        g => g.Key,
        g => g.OrderBy(x => x.PeriodStartDate).GroupBy(x => x.WeekDayPeriod, StringComparer.OrdinalIgnoreCase).Select(g => g.First())
          .Select(x => new StudentTimetableEntry { Period = x.WeekDayPeriod, Class = CleanClassName(x.ClassName), Room = CleanRoom(x.LocationName), TeacherCode = x.StaffCode }).ToList());

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
      KS2Reading = assessmentResults.GetValueOrDefault((row.StudentId, "RSS")),
      KS2Maths = assessmentResults.GetValueOrDefault((row.StudentId, "MSS")),
      Contacts = contactsByStudentId.TryGetValue(row.StudentId, out var contacts) ? contacts : [],
      Classes = classesByStudentId.TryGetValue(row.StudentId, out var classes) ? classes : [],
      Timetable = timetableByStudentId.TryGetValue(row.StudentId, out var timetable) ? timetable : []
    }).OrderBy(s => s.Surname).ThenBy(s => s.Forename).ThenBy(s => s.YearGroup).ThenBy(s => s.TutorGroup).ToList();
  }

  /// <summary>Gets staff for a school, including classes and timetables.</summary>
  /// <param name="schoolId">The Bromcom school identifier.</param>
  /// <param name="cancellationToken">A token that can cancel the request.</param>
  /// <returns>A list of staff ordered by surname, forename, and staff code.</returns>
  /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="schoolId"/> is not positive.</exception>
  /// <exception cref="ObjectDisposedException">Thrown when the client has been disposed.</exception>
  /// <exception cref="HttpRequestException">Thrown when the Bromcom API returns an unsuccessful status code.</exception>
  /// <exception cref="InvalidOperationException">Thrown when the Bromcom API response is invalid or unsuccessful.</exception>
  public async Task<IReadOnlyList<Staff>> GetStaffAsync(int schoolId, CancellationToken cancellationToken = default)
  {
    ValidateRequest(schoolId);

    var staffTask = GetAsync<StaffContract>("/v2/Staff", schoolId, null, null, cancellationToken);
    var lineManagersTask = GetAsync<StaffLineManagerContract>("/v2/StaffLineManagers", schoolId, null, null, cancellationToken);
    var timetableTask = GetTimetableRowsAsync(schoolId, cancellationToken);
    await Task.WhenAll(staffTask, lineManagersTask, timetableTask).ConfigureAwait(false);

    var staff = await staffTask.ConfigureAwait(false);
    var lineManagers = await lineManagersTask.ConfigureAwait(false);
    var lineManagerIdsByStaffId = lineManagers.GroupBy(row => row.EmployeeId).ToDictionary(g => g.Key, g => g.First().LineManagerEmployeeId);
    var timetableRows = await timetableTask.ConfigureAwait(false);
    var classesByStaffId = timetableRows
      .Where(x => x.StaffId is not null && !string.IsNullOrWhiteSpace(x.ClassName))
      .GroupBy(x => x.StaffId!.Value)
      .ToDictionary(
        g => g.Key,
        g => g.Select(x => CleanClassName(x.ClassName)!).Distinct(StringComparer.OrdinalIgnoreCase).ToList());
    var timetableByStaffId = timetableRows
      .Where(x => x.StaffId is not null && !string.IsNullOrWhiteSpace(x.WeekDayPeriod) && !string.IsNullOrWhiteSpace(x.TimetableEntry))
      .GroupBy(x => x.StaffId!.Value)
      .ToDictionary(
        g => g.Key,
        g => g.OrderBy(x => x.PeriodStartDate).GroupBy(x => x.WeekDayPeriod, StringComparer.OrdinalIgnoreCase).Select(g => g.First())
          .Select(x => new StaffTimetableEntry { Period = x.WeekDayPeriod, Class = CleanClassName(x.TimetableEntry), Room = CleanRoom(x.LocationName) }).ToList());

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

  private async Task<Dictionary<int, List<StudentContact>>> GetStudentContactsByStudentIdAsync(int schoolId, HashSet<int> studentIds,
    string studentFilter, CancellationToken cancellationToken)
  {
    var rows = await GetAsync<StudentContactContract>("/v2/StudentContacts", schoolId, studentFilter, null, cancellationToken).ConfigureAwait(false);
    var links = rows.Where(row => studentIds.Contains(row.StudentId)).ToList();
    if (links.Count == 0) return [];

    var contactIds = links.Select(row => row.ContactId).ToHashSet();
    var personFilter = BuildIdRangeFilter("personID", contactIds);
    var peopleTask = GetAsync<PersonContract>("/v2/People", schoolId, personFilter, null, cancellationToken);
    var communicationsTask = GetAsync<PersonCommunicationsContract>("/v2/PersonCommunications", schoolId, personFilter, null, cancellationToken);
    await Task.WhenAll(peopleTask, communicationsTask).ConfigureAwait(false);

    var people = await peopleTask.ConfigureAwait(false);
    var communications = await communicationsTask.ConfigureAwait(false);
    var peopleById = people.Where(row => contactIds.Contains(row.PersonId)).DistinctBy(row => row.PersonId).ToDictionary(row => row.PersonId);
    var communicationsById = communications.Where(row => contactIds.Contains(row.PersonId)).DistinctBy(row => row.PersonId).ToDictionary(row => row.PersonId);

    return links.GroupBy(row => row.StudentId).ToDictionary(group => group.Key, group => group.OrderBy(row => row.ContactPriority)
      .ThenBy(row => row.ContactId).Select(row =>
    {
      peopleById.TryGetValue(row.ContactId, out var person);
      communicationsById.TryGetValue(row.ContactId, out var communication);
      var seenTelephones = new HashSet<string>(StringComparer.Ordinal);
      var telephones = new[] { row.ContactPriorityTelephoneNumber, communication?.MobileTelephone, communication?.HomeTelephone, communication?.WorkTelephone }
        .Select(CleanTelephone).OfType<string>().Where(seenTelephones.Add).ToArray();

      return new StudentContact
      {
        Id = row.ContactId,
        Title = CleanString(person?.Prefix),
        Forename = CleanString(person?.PreferredFirstName) ?? CleanString(person?.FirstName),
        Surname = CleanString(person?.PreferredLastName) ?? CleanString(person?.LastName),
        Telephones = telephones,
        Email = (CleanString(communication?.HomeEmail) ?? CleanString(communication?.WorkEmail))?.ToLowerInvariant(),
        Relationship = CleanString(row.ContactRelationType),
        Priority = row.ContactPriority,
        Address = CleanString(communication?.HomeAddress),
        IsAddressConfidential = person?.IsDiscloseAddress ?? false,
        HasParentalResponsibility = row.ParentResponsibility
      };
    }).ToList());
  }

  private async Task<List<TimetableContract>> GetTimetableRowsAsync(int schoolId, CancellationToken cancellationToken, HashSet<int>? studentIds = null)
  {
    var timetableRows = new List<TimetableContract>();
    var daysFound = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var rowCount = 0;
    var today = DateTime.UtcNow.Date;
    var maxDate = today.AddDays(35);
    var endpoint = studentIds is null ? "/v2/Timetable" : "/v2/StudentTimetables";
    var scopeFilter = studentIds is null ? "isCover=0" : BuildIdRangeFilter("studentID", studentIds);

    for (var windowStart = today; windowStart <= maxDate; windowStart = windowStart.AddDays(7))
    {
      var windowEnd = windowStart.AddDays(7);
      var entityFilter = FormattableString.Invariant($"periodStartDate>='{windowStart:yyyy-MM-dd}' and periodStartDate<'{windowEnd:yyyy-MM-dd}' and {scopeFilter}");
      var windowRows = await GetAsync<TimetableContract>(endpoint, schoolId, entityFilter, null, cancellationToken).ConfigureAwait(false);
      if (studentIds is not null)
        windowRows = windowRows.Where(row => row.StudentId is int studentId && studentIds.Contains(studentId)).ToList();

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
