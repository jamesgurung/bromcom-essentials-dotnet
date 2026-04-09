using System.Text.Json.Serialization;

namespace BromcomEssentials;

internal sealed class ApiResponse<TData>
{
  public bool Success { get; set; }
  public List<TData>? Data { get; set; }
}

internal sealed class StudentFlatViewContract
{
  public int StudentId { get; set; }
  public string? PreferredFirstName { get; set; }
  public string? PreferredLastName { get; set; }
  public string? GenderCode { get; set; }
  public string? DateOfBirth { get; set; }
  public string? StudentEmail { get; set; }
  public string? Upn { get; set; }
  public string? ExamNumber { get; set; }
  public string? AdmissionNumber { get; set; }
  public string? EthnicityCode { get; set; }
  public string? ProvisionName { get; set; }
  public string? GntFlag { get; set; }
  public string? EverFsm6Flag { get; set; }
  public string? InCareFlag { get; set; }
  public string? EalFlag { get; set; }
  public bool? PremiumPupilFlag { get; set; }
  public string? EnrolmentStateName { get; set; }
  public decimal? PresentPercentageWithEA { get; set; }
  public string? YearGroup { get; set; }
  public string? TutorGroupName { get; set; }
  public string? Contact1Name { get; set; }
  public string? Contact1Telephone { get; set; }
  public string? Contact1Email { get; set; }
  public string? Contact1Relationship { get; set; }
  public string? Contact1ParentalResponsibility { get; set; }
  public string? Contact2Name { get; set; }
  public string? Contact2Telephone { get; set; }
  public string? Contact2Email { get; set; }
  public string? Contact2Relationship { get; set; }
  public string? Contact2ParentalResponsibility { get; set; }
  public string? Contact3Name { get; set; }
  public string? Contact3Telephone { get; set; }
  public string? Contact3Email { get; set; }
  public string? Contact3Relationship { get; set; }
  public string? Contact3ParentalResponsibility { get; set; }
}

internal sealed class YearGroupSubjectStudentContract
{
  public int StudentId { get; set; }
  public string? ClassName { get; set; }
  public string? SubjectDescription { get; set; }
}

internal sealed class StaffContract
{
  public int StaffId { get; set; }
  public string? Title { get; set; }
  public string? PreferredFirstName { get; set; }
  public string? PreferredLastName { get; set; }
  public string? WorkEmail { get; set; }
  public string? StaffCode { get; set; }
  public string? JobTitle { get; set; }
}

internal sealed class TimetableContract
{
  public int StaffId { get; set; }
  public int? StudentId { get; set; }
  public string? ClassName { get; set; }
  public string? LocationName { get; set; }
  public string? WeekNumber { get; set { field = value; _hasWeekDayPeriod = false; } }
  public int TimetableDay { get; set { field = value; _hasWeekDayPeriod = false; } }
  public string? PeriodName { get; set { field = value; _hasWeekDayPeriod = false; } }
  public string? TimetableEntry { get; set; }
  public string? PeriodStartDate { get; set; }

  private string? _weekDayPeriod;
  private bool _hasWeekDayPeriod;
  [JsonIgnore]
  public string? WeekDayPeriod
  {
    get
    {
      if (_hasWeekDayPeriod) return _weekDayPeriod;
      _weekDayPeriod = int.TryParse(WeekNumber, out var week) && int.TryParse(PeriodName, out var period) ? $"W{week:00}:D{TimetableDay:00}:{period:00}" : null;
      _hasWeekDayPeriod = true;
      return _weekDayPeriod;
    }
  }

  [JsonIgnore]
  public string? StaffCode { get; set; }

  public string? ClassStaffRoom
  {
    set
    {
      if (string.IsNullOrWhiteSpace(value)) return;
      var parts = value.Split('\n');
      if (parts.Length < 2) return;
      ClassName = string.IsNullOrWhiteSpace(parts[0]) ? null : parts[0].Trim();
      StaffCode = string.IsNullOrWhiteSpace(parts[1]) ? null : parts[1].Trim();
    }
  }
}

internal sealed class DepartmentContract
{
  public int DepartmentId { get; set; }
  public int SubjectId { get; set; }
  public string? CollectionName { get; set; }
}

internal sealed class DepartmentTeacherContract
{
  public int DepartmentId { get; set; }
  public int PersonId { get; set; }
  public string? CollectionRoleTypeDescription { get; set; }
}

internal sealed class SubjectContract
{
  public int SubjectId { get; set; }
  public string? Abbreviation { get; set; }
}
