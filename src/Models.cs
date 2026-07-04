using System.Diagnostics;

namespace BromcomEssentials;

/// <summary>Basic student details and optional related class, timetable, and parent contact data.</summary>
[DebuggerDisplay("{Forename,nq} {Surname,nq} {TutorGroup,nq}")]
public sealed class Student
{
  /// <summary>Gets the Bromcom student identifier.</summary>
  public int Id { get; init; }
  /// <summary>Gets the student's preferred or legal forename.</summary>
  public string? Forename { get; init; }
  /// <summary>Gets the student's preferred or legal surname.</summary>
  public string? Surname { get; init; }
  /// <summary>Gets the student's gender code.</summary>
  public string? Gender { get; init; }
  /// <summary>Gets the student's date of birth.</summary>
  public DateOnly? DateOfBirth { get; init; }
  /// <summary>Gets the student's email address.</summary>
  public string? Email { get; init; }
  /// <summary>Gets the student's unique pupil number.</summary>
  public string? Upn { get; init; }
  /// <summary>Gets the student's exam number.</summary>
  public int? ExamNumber { get; init; }
  /// <summary>Gets the student's admission number.</summary>
  public int? AdmissionNumber { get; init; }
  /// <summary>Gets the student's ethnicity code.</summary>
  public string? EthnicCode { get; init; }
  /// <summary>Gets the student's SEND status code.</summary>
  public string? SendStatusCode { get; init; }
  /// <summary>Gets whether the student is marked as gifted and talented.</summary>
  public bool IsGiftedAndTalented { get; init; }
  /// <summary>Gets whether the student is marked as ever 6 free school meals.</summary>
  public bool IsFsmEver6 { get; init; }
  /// <summary>Gets whether the student is marked as English as an additional language.</summary>
  public bool IsEal { get; init; }
  /// <summary>Gets whether the student is marked as looked after.</summary>
  public bool IsLookedAfter { get; init; }
  /// <summary>Gets whether the student is marked as pupil premium.</summary>
  public bool IsPupilPremium { get; init; }
  /// <summary>Gets the student's enrolment status.</summary>
  public string? EnrolmentStatus { get; init; }
  /// <summary>Gets the student's year group.</summary>
  public int? YearGroup { get; init; }
  /// <summary>Gets the student's tutor group.</summary>
  public string? TutorGroup { get; init; }
  /// <summary>Gets the student's present attendance percentage, when supplied by Bromcom.</summary>
  public decimal? Attendance { get; init; }
  /// <summary>Gets parent contacts with parental responsibility.</summary>
  public IReadOnlyList<ParentContact> Parents { get; init; } = [];
  /// <summary>Gets the student's classes when requested.</summary>
  public IReadOnlyList<StudentClass> Classes { get; init; } = [];
  /// <summary>Gets the student's timetable entries when requested.</summary>
  public IReadOnlyList<StudentTimetableEntry> Timetable { get; init; } = [];
}

/// <summary>Parent or guardian contact details for a student.</summary>
[DebuggerDisplay("{Name,nq} ({Relationship,nq})")]
public sealed class ParentContact
{
  /// <summary>Gets the contact name.</summary>
  public string? Name { get; init; }
  /// <summary>Gets the contact telephone number.</summary>
  public string? Telephone { get; init; }
  /// <summary>Gets the contact email address.</summary>
  public string? Email { get; init; }
  /// <summary>Gets the contact relationship to the student.</summary>
  public string? Relationship { get; init; }
}

/// <summary>A class associated with a student.</summary>
[DebuggerDisplay("{Name,nq} ({Subject,nq})")]
public sealed class StudentClass
{
  /// <summary>Gets the class name.</summary>
  public required string Name { get; init; }
  /// <summary>Gets the class subject.</summary>
  public string? Subject { get; init; }
}

/// <summary>A student timetable entry.</summary>
[DebuggerDisplay("{Period,nq}: {Class,nq}")]
public sealed class StudentTimetableEntry
{
  /// <summary>Gets the timetable period identifier.</summary>
  public string? Period { get; init; }
  /// <summary>Gets the class name.</summary>
  public string? Class { get; init; }
  /// <summary>Gets the room name.</summary>
  public string? Room { get; init; }
  /// <summary>Gets the teacher code.</summary>
  public string? TeacherCode { get; init; }
}

/// <summary>Weekly attendance marks and calculated attendance percentage for a student.</summary>
[DebuggerDisplay("{StudentId}: {Percentage}%")]
public sealed class StudentWeeklyAttendance
{
  /// <summary>Gets the Bromcom student identifier.</summary>
  public int StudentId { get; init; }
  /// <summary>Gets the morning and afternoon session attendance marks for the week.</summary>
  public IReadOnlyList<SessionAttendance> Attendances { get; init; } = [];
  /// <summary>Gets the calculated percentage of present sessions.</summary>
  public decimal Percentage { get; init; }
}

/// <summary>Attendance for a single timetable period.</summary>
[DebuggerDisplay("{StudentId}: {Date} {PeriodName,nq} {Code,nq}")]
public sealed class PeriodAttendance
{
  /// <summary>Gets the Bromcom student identifier.</summary>
  public int StudentId { get; init; }
  /// <summary>Gets the attendance date.</summary>
  public DateOnly Date { get; init; }
  /// <summary>Gets the period display name.</summary>
  public required string PeriodName { get; init; }
  /// <summary>Gets the attendance mark code.</summary>
  public string? Code { get; init; }
  /// <summary>Gets the attendance comment.</summary>
  public string? Comment { get; init; }
  /// <summary>Gets the attendance category inferred from the mark code.</summary>
  public AttendanceCategory Category { get; init; }
}

/// <summary>Attendance for a morning or afternoon session.</summary>
[DebuggerDisplay("{DayOfWeek} {Session}: {Code,nq}")]
public sealed class SessionAttendance
{
  /// <summary>Gets the day of the week.</summary>
  public DayOfWeek DayOfWeek { get; init; }
  /// <summary>Gets the session type.</summary>
  public SessionType Session { get; init; }
  /// <summary>Gets the attendance mark code.</summary>
  public string? Code { get; init; }
  /// <summary>Gets the attendance category inferred from the mark code.</summary>
  public AttendanceCategory Category { get; init; }
}

/// <summary>School session within a day.</summary>
public enum SessionType
{
  /// <summary>Morning session.</summary>
  AM,
  /// <summary>Afternoon session.</summary>
  PM
}

/// <summary>Normalised attendance category inferred from a Bromcom attendance mark.</summary>
public enum AttendanceCategory
{
  /// <summary>No attendance mark has been entered.</summary>
  NotEntered,
  /// <summary>The mark counts as present.</summary>
  Present,
  /// <summary>The mark counts as an approved educational activity.</summary>
  ApprovedEducationalActivity,
  /// <summary>The mark counts as an authorised absence.</summary>
  AuthorisedAbsence,
  /// <summary>The mark counts as an unauthorised absence.</summary>
  UnauthorisedAbsence,
  /// <summary>The mark indicates attendance was not possible.</summary>
  NotPossibleAttendance,
  /// <summary>The mark is not recognised by this SDK.</summary>
  Invalid
}

/// <summary>Basic staff details and optional class and timetable data.</summary>
[DebuggerDisplay("{Forename,nq} {Surname,nq}")]
public sealed class Staff
{
  /// <summary>Gets the Bromcom staff identifier.</summary>
  public int Id { get; init; }
  /// <summary>Gets the staff member title.</summary>
  public string? Title { get; init; }
  /// <summary>Gets the staff member's preferred or legal forename.</summary>
  public string? Forename { get; init; }
  /// <summary>Gets the staff member's preferred or legal surname.</summary>
  public string? Surname { get; init; }
  /// <summary>Gets the staff member email address.</summary>
  public string? Email { get; init; }
  /// <summary>Gets the staff code.</summary>
  public string? StaffCode { get; init; }
  /// <summary>Gets the staff member job title.</summary>
  public string? JobTitle { get; init; }
  /// <summary>Gets the staff identifier for the line manager.</summary>
  public int? LineManagerId { get; init; }
  /// <summary>Gets class names associated with the staff member when requested.</summary>
  public IReadOnlyList<string> Classes { get; init; } = [];
  /// <summary>Gets timetable entries for the staff member when requested.</summary>
  public IReadOnlyList<StaffTimetableEntry> Timetable { get; init; } = [];
}

/// <summary>A staff timetable entry.</summary>
[DebuggerDisplay("{Period,nq}: {Class,nq}")]
public sealed class StaffTimetableEntry
{
  /// <summary>Gets the timetable period identifier.</summary>
  public string? Period { get; init; }
  /// <summary>Gets the class name.</summary>
  public string? Class { get; init; }
  /// <summary>Gets the room name.</summary>
  public string? Room { get; init; }
}

/// <summary>A staff absence in a date range.</summary>
[DebuggerDisplay("{EmployeeId}: {Start}")]
public sealed class StaffAbsence
{
  /// <summary>Gets the Bromcom staff absence identifier.</summary>
  public int Id { get; init; }
  /// <summary>Gets the Bromcom employee identifier.</summary>
  public int EmployeeId { get; init; }
  /// <summary>Gets the absence type.</summary>
  public string? Type { get; init; }
  /// <summary>Gets the absence notes.</summary>
  public string? Notes { get; init; }
  /// <summary>Gets the absence duration.</summary>
  public decimal Duration { get; init; }
  /// <summary>Gets the absence start date and time.</summary>
  public DateTime Start { get; init; }
  /// <summary>Gets the absence end date and time.</summary>
  public DateTime? End { get; init; }
}

/// <summary>A room cover arrangement in a date range.</summary>
[DebuggerDisplay("{Date} {PeriodId,nq}: {ClassName,nq} {CoveredRoom,nq} -> {CoveringRoom,nq}")]
public sealed class RoomCover
{
  /// <summary>Gets the Bromcom cover identifier.</summary>
  public int Id { get; init; }
  /// <summary>Gets the cover date.</summary>
  public DateOnly Date { get; init; }
  /// <summary>Gets the covered period identifier.</summary>
  public string? PeriodId { get; init; }
  /// <summary>Gets the cover reason.</summary>
  public string? Reason { get; init; }
  /// <summary>Gets the covered class name.</summary>
  public string? ClassName { get; init; }
  /// <summary>Gets the covered room name.</summary>
  public string? CoveredRoom { get; init; }
  /// <summary>Gets the covering room name.</summary>
  public string? CoveringRoom { get; init; }
}

/// <summary>A staff cover arrangement in a date range.</summary>
[DebuggerDisplay("{Date} {PeriodId,nq}: {ClassName,nq} {CoveredStaffId} -> {CoveringStaffId}")]
public sealed class StaffCover
{
  /// <summary>Gets the Bromcom cover identifier.</summary>
  public int Id { get; init; }
  /// <summary>Gets the cover date.</summary>
  public DateOnly Date { get; init; }
  /// <summary>Gets the covered period identifier.</summary>
  public string? PeriodId { get; init; }
  /// <summary>Gets the cover reason.</summary>
  public string? Reason { get; init; }
  /// <summary>Gets the covered class name.</summary>
  public string? ClassName { get; init; }
  /// <summary>Gets the covered staff identifier.</summary>
  public int CoveredStaffId { get; init; }
  /// <summary>Gets the covering staff identifier.</summary>
  public int? CoveringStaffId { get; init; }
  /// <summary>Gets the staff absence type associated with the cover.</summary>
  public string? AbsenceType { get; init; }
  /// <summary>Gets the cover status.</summary>
  public string? CoverStatus { get; init; }
}

/// <summary>A granted parental consent record for a student.</summary>
[DebuggerDisplay("{StudentId}: {ConsentType,nq}")]
public sealed class ParentalConsent
{
  /// <summary>Gets the Bromcom student identifier.</summary>
  public int StudentId { get; init; }
  /// <summary>Gets the parental consent type.</summary>
  public string? ConsentType { get; init; }
}

/// <summary>A behaviour event type configured in Bromcom.</summary>
[DebuggerDisplay("{Code,nq}: {Name,nq}")]
public sealed class BehaviourType
{
  /// <summary>Gets the Bromcom behaviour event type identifier.</summary>
  public int Id { get; init; }
  /// <summary>Gets the behaviour event type code.</summary>
  public string? Code { get; init; }
  /// <summary>Gets the behaviour event type name.</summary>
  public string? Name { get; init; }
}

/// <summary>A behaviour event record.</summary>
[DebuggerDisplay("{Id}")]
public sealed class BehaviourEvent
{
  /// <summary>Gets the Bromcom behaviour event record identifier.</summary>
  public int Id { get; init; }
  /// <summary>Gets the Bromcom student identifier.</summary>
  public int StudentId { get; init; }
  /// <summary>Gets the behaviour event type identifier.</summary>
  public int EventTypeId { get; init; }
  /// <summary>Gets the staff identifier for the event owner.</summary>
  public int StaffId { get; init; }
  /// <summary>Gets the class identifier associated with the event.</summary>
  public int? ClassId { get; init; }
  /// <summary>Gets the location identifier associated with the event.</summary>
  public int? LocationId { get; init; }
  /// <summary>Gets the event date and time.</summary>
  public DateTime Date { get; init; }
  /// <summary>Gets the behaviour points adjustment.</summary>
  public int Points { get; init; }
  /// <summary>Gets the external event comment.</summary>
  public string? Comment { get; init; }
  /// <summary>Gets the internal event comment.</summary>
  public string? InternalComment { get; init; }
}

/// <summary>A department with subjects and staff membership.</summary>
[DebuggerDisplay("{Name,nq}")]
public sealed class Department
{
  /// <summary>Gets the Bromcom department identifier.</summary>
  public int Id { get; init; }
  /// <summary>Gets the department name.</summary>
  public string? Name { get; init; }
  /// <summary>Gets the staff identifier for the head of department.</summary>
  public int? HeadOfDepartmentId { get; init; }
  /// <summary>Gets staff identifiers for department leaders.</summary>
  public IReadOnlyList<int> LeaderIds { get; init; } = [];
  /// <summary>Gets staff identifiers for department teachers.</summary>
  public IReadOnlyList<int> TeacherIds { get; init; } = [];
  /// <summary>Gets subjects associated with the department.</summary>
  public IReadOnlyList<Subject> Subjects { get; init; } = [];
}

/// <summary>A subject associated with a department.</summary>
[DebuggerDisplay("{Name,nq}")]
public sealed class Subject
{
  /// <summary>Gets the Bromcom subject identifier.</summary>
  public int Id { get; init; }
  /// <summary>Gets the subject name.</summary>
  public string? Name { get; init; }
  /// <summary>Gets the subject code.</summary>
  public string? Code { get; init; }
}

/// <summary>An assessment result for a student.</summary>
[DebuggerDisplay("{StudentId}: {Type,nq} {Subject,nq} = {Result,nq}")]
public sealed class AssessmentResult
{
  /// <summary>Gets the Bromcom student identifier.</summary>
  public int StudentId { get; init; }
  /// <summary>Gets the assessment type.</summary>
  public required string Type { get; init; }
  /// <summary>Gets the year group associated with the result.</summary>
  public int? YearGroup { get; init; }
  /// <summary>Gets the term associated with the result.</summary>
  public string? Term { get; init; }
  /// <summary>Gets the subject associated with the result.</summary>
  public string? Subject { get; init; }
  /// <summary>Gets the assessment result value.</summary>
  public required string Result { get; init; }
}
