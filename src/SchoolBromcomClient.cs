namespace BromcomEssentials;

/// <summary>Bromcom client bound to a school.</summary>
public sealed class SchoolBromcomClient : IDisposable
{
  private readonly BromcomClient _client;
  private readonly int _schoolId;

  /// <summary>Initialises a new instance of the <see cref="SchoolBromcomClient"/> class.</summary>
  /// <param name="applicationId">The Bromcom Partner API application ID.</param>
  /// <param name="applicationSecret">The Bromcom Partner API application secret.</param>
  /// <param name="schoolId">The Bromcom school identifier.</param>
  /// <param name="httpClient">Optional HTTP client to use for requests. When omitted, the client creates and owns one.</param>
  /// <exception cref="ArgumentException">Thrown when <paramref name="applicationId"/> or <paramref name="applicationSecret"/> is blank.</exception>
  /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="schoolId"/> is not positive.</exception>
  public SchoolBromcomClient(string applicationId, string applicationSecret, int schoolId, HttpClient? httpClient = null)
  {
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(schoolId);
    _client = new(applicationId, applicationSecret, httpClient);
    _schoolId = schoolId;
  }

  /// <summary>Gets students, with optional class and timetable data.</summary>
  public Task<IReadOnlyList<Student>> GetStudentsAsync(bool includeClasses = false, bool includeTimetable = false,
    CancellationToken cancellationToken = default) =>
    _client.GetStudentsAsync(_schoolId, includeClasses, includeTimetable, cancellationToken);

  /// <summary>Gets staff, with optional class and timetable data.</summary>
  public Task<IReadOnlyList<Staff>> GetStaffAsync(bool includeClassesAndTimetable = false, CancellationToken cancellationToken = default) =>
    _client.GetStaffAsync(_schoolId, includeClassesAndTimetable, cancellationToken);

  /// <summary>Gets person and photo identifiers.</summary>
  public Task<List<PersonPhotoId>> GetPhotoIdsAsync(CancellationToken cancellationToken = default) =>
    _client.GetPhotoIdsAsync(_schoolId, cancellationToken);

  /// <summary>Gets the image for an encrypted photo identifier.</summary>
  public Task<byte[]> GetPhotoAsync(string photoId, CancellationToken cancellationToken = default) =>
    _client.GetPhotoAsync(photoId, cancellationToken);

  /// <summary>Gets clubs that are active today.</summary>
  public Task<IReadOnlyList<Club>> GetClubsAsync(CancellationToken cancellationToken = default) =>
    _client.GetClubsAsync(_schoolId, cancellationToken);

  /// <summary>Gets clubs that are active at any point in an inclusive date range.</summary>
  public Task<IReadOnlyList<Club>> GetClubsAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default) =>
    _client.GetClubsAsync(_schoolId, startDate, endDate, cancellationToken);

  /// <summary>Gets club attendance records for a date range, with an optional student filter.</summary>
  public Task<IReadOnlyList<ClubStudentAttendance>> GetClubAttendancesAsync(DateOnly startDate, DateOnly? endDate = null,
    IList<int>? studentIds = null, CancellationToken cancellationToken = default) =>
    _client.GetClubAttendancesAsync(_schoolId, startDate, endDate, studentIds, cancellationToken);

  /// <summary>Gets morning and afternoon attendance marks for the week containing a date, with an optional student filter.</summary>
  public Task<IReadOnlyList<StudentWeeklyAttendance>> GetAttendancesByWeekAsync(DateOnly date,
    IList<int>? studentIds = null, CancellationToken cancellationToken = default) =>
    _client.GetAttendancesByWeekAsync(_schoolId, date, studentIds, cancellationToken);

  /// <summary>Gets period attendance marks for a date range, with optional period and student filters.</summary>
  public Task<IReadOnlyList<PeriodAttendance>> GetAttendancesAsync(DateOnly startDate, DateOnly? endDate = null, IList<string>? periodNames = null,
    IList<int>? studentIds = null, CancellationToken cancellationToken = default) =>
    _client.GetAttendancesAsync(_schoolId, startDate, endDate, periodNames, studentIds, cancellationToken);

  /// <summary>Gets user defined field data, with optional entity type, identifier, field type, and value filters.</summary>
  public Task<IReadOnlyList<UdfData>> GetUdfDataAsync(string? entityType = null, int? id = null, string? type = null,
    string? value = null, CancellationToken cancellationToken = default) =>
    _client.GetUdfDataAsync(_schoolId, entityType, id, type, value, cancellationToken);

  /// <summary>Gets staff absences that overlap a date range.</summary>
  public Task<IReadOnlyList<StaffAbsence>> GetStaffAbsencesAsync(DateOnly startDate, DateOnly? endDate = null,
    CancellationToken cancellationToken = default) =>
    _client.GetStaffAbsencesAsync(_schoolId, startDate, endDate, cancellationToken);

  /// <summary>Gets student detentions for a date range, with an optional student filter.</summary>
  public Task<IReadOnlyList<Detention>> GetDetentionsAsync(DateOnly startDate, DateOnly? endDate = null,
    IList<int>? studentIds = null, CancellationToken cancellationToken = default) =>
    _client.GetDetentionsAsync(_schoolId, startDate, endDate, studentIds, cancellationToken);

  /// <summary>Gets room covers for a date range.</summary>
  public Task<IReadOnlyList<RoomCover>> GetRoomCoversAsync(DateOnly startDate, DateOnly? endDate = null,
    CancellationToken cancellationToken = default) =>
    _client.GetRoomCoversAsync(_schoolId, startDate, endDate, cancellationToken);

  /// <summary>Gets staff covers for a date range.</summary>
  public Task<IReadOnlyList<StaffCover>> GetStaffCoversAsync(DateOnly startDate, DateOnly? endDate = null,
    CancellationToken cancellationToken = default) =>
    _client.GetStaffCoversAsync(_schoolId, startDate, endDate, cancellationToken);

  /// <summary>Gets parental consent records, with optional consent type and student filters.</summary>
  public Task<IReadOnlyList<ParentalConsent>> GetParentalConsentAsync(string? consentType = null,
    IList<int>? studentIds = null, CancellationToken cancellationToken = default) =>
    _client.GetParentalConsentAsync(_schoolId, consentType, studentIds, cancellationToken);

  /// <summary>Gets distinct behaviour event types.</summary>
  public Task<IReadOnlyList<BehaviourType>> GetBehaviourTypesAsync(CancellationToken cancellationToken = default) =>
    _client.GetBehaviourTypesAsync(_schoolId, cancellationToken);

  /// <summary>Gets behaviour events for a date range, with an optional student filter.</summary>
  public Task<IReadOnlyList<BehaviourEvent>> GetBehaviourEventsAsync(DateOnly startDate, DateOnly? endDate = null,
    IList<int>? studentIds = null, CancellationToken cancellationToken = default) =>
    _client.GetBehaviourEventsAsync(_schoolId, startDate, endDate, studentIds, cancellationToken);

  /// <summary>Creates or updates a behaviour event record.</summary>
  public Task SetBehaviourEventAsync(BehaviourEvent ev, CancellationToken cancellationToken = default) =>
    _client.SetBehaviourEventAsync(_schoolId, ev, cancellationToken);

  /// <summary>Gets departments, subjects, and department staff membership.</summary>
  public Task<IReadOnlyList<Department>> GetDepartmentsAsync(CancellationToken cancellationToken = default) =>
    _client.GetDepartmentsAsync(_schoolId, cancellationToken);

  /// <summary>Gets assessment columns, with optional term and year group filters.</summary>
  public Task<IReadOnlyList<AssessmentColumn>> GetColumnsAsync(string? term = null, int? yearGroup = null,
    CancellationToken cancellationToken = default) =>
    _client.GetColumnsAsync(_schoolId, term, yearGroup, cancellationToken);

  /// <summary>Gets assessment results with optional academic year, term, year group, and student filters.</summary>
  public Task<IReadOnlyList<AssessmentResult>> GetResultsAsync(int? academicYearStart = null, string? term = null, int? yearGroup = null,
    bool gradesOnly = false, IList<int>? studentIds = null, CancellationToken cancellationToken = default) =>
    _client.GetResultsAsync(_schoolId, academicYearStart, term, yearGroup, gradesOnly, studentIds, cancellationToken);

  /// <summary>Creates or updates an assessment result with the current UTC result and modification dates.</summary>
  public Task SetResultAsync(AssessmentResultRequest result, CancellationToken cancellationToken = default) =>
    _client.SetResultAsync(_schoolId, result, cancellationToken);

  /// <summary>Releases the owned client.</summary>
  public void Dispose()
  {
    _client.Dispose();
    GC.SuppressFinalize(this);
  }
}
