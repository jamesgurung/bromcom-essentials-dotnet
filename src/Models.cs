using System.Diagnostics;

namespace BromcomEssentials;

[DebuggerDisplay("{Forename,nq} {Surname,nq} {TutorGroup,nq}")]
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

[DebuggerDisplay("{Name,nq} ({Relationship,nq})")]
public sealed class ParentContact
{
  public string? Name { get; init; }
  public string? Telephone { get; init; }
  public string? Email { get; init; }
  public string? Relationship { get; init; }
}

[DebuggerDisplay("{Name,nq} ({Subject,nq})")]
public sealed class StudentClass
{
  public required string Name { get; init; }
  public string? Subject { get; init; }
}

[DebuggerDisplay("{Period,nq}: {Class,nq}")]
public sealed class StudentTimetableEntry
{
  public string? Period { get; init; }
  public string? Class { get; init; }
  public string? Room { get; init; }
  public string? TeacherCode { get; init; }
}

[DebuggerDisplay("{StudentId}: {Percentage}%")]
public sealed class StudentWeeklyAttendance
{
  public int StudentId { get; init; }
  public IReadOnlyList<SessionAttendance> Attendances { get; init; } = [];
  public decimal Percentage { get; init; }
}

[DebuggerDisplay("{StudentId}: {Date} {PeriodName,nq} {Code,nq}")]
public sealed class PeriodAttendance
{
  public int StudentId { get; init; }
  public DateOnly Date { get; init; }
  public required string PeriodName { get; init; }
  public string? Code { get; init; }
  public string? Comment { get; init; }
  public AttendanceCategory Category { get; init; }
}

[DebuggerDisplay("{DayOfWeek} {Session}: {Code,nq}")]
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

[DebuggerDisplay("{Forename,nq} {Surname,nq}")]
public sealed class Staff
{
  public int Id { get; init; }
  public string? Title { get; init; }
  public string? Forename { get; init; }
  public string? Surname { get; init; }
  public string? Email { get; init; }
  public string? StaffCode { get; init; }
  public string? JobTitle { get; init; }
  public int? LineManagerId { get; init; }
  public IReadOnlyList<string> Classes { get; init; } = [];
  public IReadOnlyList<StaffTimetableEntry> Timetable { get; init; } = [];
}

[DebuggerDisplay("{Period,nq}: {Class,nq}")]
public sealed class StaffTimetableEntry
{
  public string? Period { get; init; }
  public string? Class { get; init; }
  public string? Room { get; init; }
}

[DebuggerDisplay("{EmployeeId}: {Start}")]
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

[DebuggerDisplay("{Date} {PeriodId,nq}: {ClassName,nq} {CoveredRoom,nq} -> {CoveringRoom,nq}")]
public sealed class RoomCover
{
  public int Id { get; init; }
  public DateOnly Date { get; init; }
  public string? PeriodId { get; init; }
  public string? Reason { get; init; }
  public string? ClassName { get; init; }
  public string? CoveredRoom { get; init; }
  public string? CoveringRoom { get; init; }
}

[DebuggerDisplay("{Date} {PeriodId,nq}: {ClassName,nq} {CoveredStaffId} -> {CoveringStaffId}")]
public sealed class StaffCover
{
  public int Id { get; init; }
  public DateOnly Date { get; init; }
  public string? PeriodId { get; init; }
  public string? Reason { get; init; }
  public string? ClassName { get; init; }
  public int CoveredStaffId { get; init; }
  public int? CoveringStaffId { get; init; }
  public string? AbsenceType { get; init; }
  public string? CoverStatus { get; init; }
}

[DebuggerDisplay("{StudentId}: {ConsentType,nq}")]
public sealed class ParentalConsent
{
  public int StudentId { get; init; }
  public string? ConsentType { get; init; }
}

[DebuggerDisplay("{Code,nq}: {Name,nq}")]
public sealed class BehaviourType
{
  public int Id { get; init; }
  public string? Code { get; init; }
  public string? Name { get; init; }
}

[DebuggerDisplay("{Id}")]
public sealed class BehaviourEvent
{
  public int Id { get; init; }
  public int StudentId { get; init; }
  public int EventTypeId { get; init; }
  public int StaffId { get; init; }
  public int? ClassId { get; init; }
  public int? LocationId { get; init; }
  public DateOnly Date { get; init; }
  public int Points { get; init; }
  public string? Comment { get; init; }
  public string? InternalComment { get; init; }
}

[DebuggerDisplay("{Name,nq}")]
public sealed class Department
{
  public int Id { get; init; }
  public string? Name { get; init; }
  public int? HeadOfDepartmentId { get; init; }
  public IReadOnlyList<int> LeaderIds { get; init; } = [];
  public IReadOnlyList<int> TeacherIds { get; init; } = [];
  public IReadOnlyList<Subject> Subjects { get; init; } = [];
}

[DebuggerDisplay("{Name,nq}")]
public sealed class Subject
{
  public int Id { get; init; }
  public string? Name { get; init; }
  public string? Code { get; init; }
}

[DebuggerDisplay("{StudentId}: {Type,nq} {Subject,nq} = {Result,nq}")]
public sealed class AssessmentResult
{
  public int StudentId { get; init; }
  public required string Type { get; init; }
  public int? YearGroup { get; init; }
  public string? Term { get; init; }
  public string? Subject { get; init; }
  public required string Result { get; init; }
}
