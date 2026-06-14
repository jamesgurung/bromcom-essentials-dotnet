namespace BromcomEssentials;

public sealed class Student
{
  public int Id { get; init; }
  public string? Forename { get; init; }
  public string? Surname { get; init; }
  public string? Gender { get; init; }
  public DateOnly? DateOfBirth { get; init; }
  public string? Email { get; init; }
  public string? Upn { get; init; }
  public int? ExamNumber { get; init; }
  public int? AdmissionNumber { get; init; }
  public string? EthnicCode { get; init; }
  public string? SendStatusCode { get; init; }
  public bool IsGiftedAndTalented { get; init; }
  public bool IsFsmEver6 { get; init; }
  public bool IsEal { get; init; }
  public bool IsLookedAfter { get; init; }
  public bool IsPupilPremium { get; init; }
  public string? EnrolmentStatus { get; init; }
  public int? YearGroup { get; init; }
  public string? TutorGroup { get; init; }
  public decimal? Attendance { get; init; }
  public IReadOnlyList<ParentContact> Parents { get; init; } = [];
  public IReadOnlyList<StudentClass> Classes { get; init; } = [];
  public IReadOnlyList<StudentTimetableEntry> Timetable { get; init; } = [];
}

public sealed class ParentContact
{
  public string? Name { get; init; }
  public string? Telephone { get; init; }
  public string? Email { get; init; }
  public string? Relationship { get; init; }
}

public sealed class StudentClass
{
  public required string Name { get; init; }
  public string? Subject { get; init; }
}

public sealed class StudentTimetableEntry
{
  public string? Period { get; init; }
  public string? Class { get; init; }
  public string? Room { get; init; }
  public string? TeacherCode { get; init; }
}

public sealed class StudentWeeklyAttendance
{
  public int StudentId { get; init; }
  public IReadOnlyList<SessionAttendance> Attendances { get; init; } = [];
  public decimal Percentage { get; init; }
}

public sealed class PeriodAttendance
{
  public int StudentId { get; init; }
  public DateOnly Date { get; init; }
  public required string PeriodName { get; init; }
  public string? Code { get; init; }
  public string? Comment { get; init; }
  public AttendanceCategory Category { get; init; }
}

public sealed class SessionAttendance
{
  public DayOfWeek DayOfWeek { get; init; }
  public SessionType Session { get; init; }
  public string? Code { get; init; }
  public AttendanceCategory Category { get; init; }
}

public enum SessionType
{
  AM,
  PM
}

public enum AttendanceCategory
{
  NotEntered,
  Present,
  ApprovedEducationalActivity,
  AuthorisedAbsence,
  UnauthorisedAbsence,
  NotPossibleAttendance,
  Invalid
}

public sealed class Staff
{
  public int Id { get; init; }
  public string? Title { get; init; }
  public string? Forename { get; init; }
  public string? Surname { get; init; }
  public string? Email { get; init; }
  public string? StaffCode { get; init; }
  public string? JobTitle { get; init; }
  public IReadOnlyList<string> Classes { get; init; } = [];
  public IReadOnlyList<StaffTimetableEntry> Timetable { get; init; } = [];
}

public sealed class StaffTimetableEntry
{
  public string? Period { get; init; }
  public string? Class { get; init; }
  public string? Room { get; init; }
}

public sealed class StaffAbsence
{
  public int Id { get; init; }
  public int EmployeeId { get; init; }
  public string? Type { get; init; }
  public string? Notes { get; init; }
  public decimal Duration { get; init; }
  public DateTime Start { get; init; }
  public DateTime? End { get; init; }
}

public sealed class Department
{
  public int Id { get; init; }
  public string? Name { get; init; }
  public int? HeadOfDepartmentId { get; init; }
  public IReadOnlyList<int> LeaderIds { get; init; } = [];
  public IReadOnlyList<int> TeacherIds { get; init; } = [];
  public IReadOnlyList<Subject> Subjects { get; init; } = [];
}

public sealed class Subject
{
  public int Id { get; init; }
  public string? Name { get; init; }
  public string? Code { get; init; }
}

public sealed class AssessmentResult
{
  public int StudentId { get; init; }
  public required string Type { get; init; }
  public int? YearGroup { get; init; }
  public string? Term { get; init; }
  public string? Subject { get; init; }
  public required string Result { get; init; }
}
