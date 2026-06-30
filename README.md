# Bromcom Essentials .NET SDK

Retrieve basic staff, student, department, attendance, cover, consent, behaviour, and assessment result data from the [Bromcom Partner API](https://partner.bromcomcloud.com) in a .NET application.

> This repository is not affiliated with Bromcom.

## Usage

```csharp
using BromcomEssentials;

using var client = new BromcomClient(applicationId, applicationSecret);
var students = await client.GetStudentsAsync(schoolId, includeClasses: true, includeTimetable: true);
var staff = await client.GetStaffAsync(schoolId, includeClassesAndTimetable: true);
var staffAbsences = await client.GetStaffAbsencesAsync(schoolId, DateOnly.FromDateTime(DateTime.Today));
var roomCovers = await client.GetRoomCoversAsync(schoolId, DateOnly.FromDateTime(DateTime.Today));
var staffCovers = await client.GetStaffCoversAsync(schoolId, DateOnly.FromDateTime(DateTime.Today));
var parentalConsents = await client.GetParentalConsentAsync(schoolId, consentType: "U");
var behaviourTypes = await client.GetBehaviourTypesAsync(schoolId);
var behaviourEvents = await client.GetBehaviourEventsAsync(schoolId, DateOnly.FromDateTime(DateTime.Today));
var departments = await client.GetDepartmentsAsync(schoolId);
var results = await client.GetResultsAsync(schoolId, 2025, term: "Spring", yearGroup: 7, gradesOnly: true);
var attendances = await client.GetAttendancesByWeekAsync(schoolId, DateOnly.FromDateTime(DateTime.Today));
var periodAttendances = await client.GetAttendancesAsync(schoolId, DateOnly.FromDateTime(DateTime.Today));
```

## Data model

### `Student`

| Property | Type |
| --- | --- |
| `Id` | `int` |
| `Forename` | `string?` |
| `Surname` | `string?` |
| `Gender` | `string?` |
| `DateOfBirth` | `DateOnly?` |
| `Email` | `string?` |
| `Upn` | `string?` |
| `ExamNumber` | `int?` |
| `AdmissionNumber` | `int?` |
| `EthnicCode` | `string?` |
| `SendStatusCode` | `string?` |
| `IsGiftedAndTalented` | `bool` |
| `IsFsmEver6` | `bool` |
| `IsEal` | `bool` |
| `IsLookedAfter` | `bool` |
| `IsPupilPremium` | `bool` |
| `EnrolmentStatus` | `string?` |
| `Attendance` | `decimal` |
| `YearGroup` | `int?` |
| `TutorGroup` | `string?` |
| `Parents` | `IReadOnlyList<ParentContact>` |
| `Classes` | `IReadOnlyList<StudentClass>` |
| `Timetable` | `IReadOnlyList<StudentTimetableEntry>` |

#### `ParentContact`

| Property | Type |
| --- | --- |
| `Name` | `string?` |
| `Telephone` | `string?` |
| `Email` | `string?` |
| `Relationship` | `string?` |

#### `StudentClass`

| Property | Type |
| --- | --- |
| `Name` | `string` |
| `Subject` | `string?` |

#### `StudentTimetableEntry`

| Property | Type |
| --- | --- |
| `Period` | `string?` |
| `Class` | `string?` |
| `Room` | `string?` |
| `TeacherCode` | `string?` |

### `Staff`

| Property | Type |
| --- | --- |
| `Id` | `int` |
| `Title` | `string?` |
| `Forename` | `string?` |
| `Surname` | `string?` |
| `Email` | `string?` |
| `StaffCode` | `string?` |
| `JobTitle` | `string?` |
| `LineManagerId` | `int?` |
| `Classes` | `IReadOnlyList<string>` |
| `Timetable` | `IReadOnlyList<StaffTimetableEntry>` |

#### `StaffTimetableEntry`

| Property | Type |
| --- | --- |
| `Period` | `string?` |
| `Class` | `string?` |
| `Room` | `string?` |

### `StaffAbsence`

| Property | Type |
| --- | --- |
| `Id` | `int` |
| `EmployeeId` | `int` |
| `Type` | `string?` |
| `Notes` | `string?` |
| `Duration` | `decimal` |
| `Start` | `DateTime` |
| `End` | `DateTime?` |

### `RoomCover`

| Property | Type |
| --- | --- |
| `Id` | `int` |
| `Date` | `DateOnly` |
| `PeriodId` | `string?` |
| `Reason` | `string?` |
| `ClassName` | `string?` |
| `CoveredRoom` | `string?` |
| `CoveringRoom` | `string?` |

### `StaffCover`

| Property | Type |
| --- | --- |
| `Id` | `int` |
| `Date` | `DateOnly` |
| `PeriodId` | `string?` |
| `Reason` | `string?` |
| `ClassName` | `string?` |
| `CoveredStaffId` | `int` |
| `CoveringStaffId` | `int?` |
| `AbsenceType` | `string?` |
| `CoverStatus` | `string?` |

### `ParentalConsent`

| Property | Type |
| --- | --- |
| `StudentId` | `int` |
| `ConsentType` | `string?` |

### `BehaviourType`

| Property | Type |
| --- | --- |
| `Id` | `int` |
| `Code` | `string?` |
| `Name` | `string?` |

### `BehaviourEvent`

| Property | Type |
| --- | --- |
| `Id` | `int` |
| `StudentId` | `int` |
| `EventTypeId` | `int` |
| `StaffId` | `int` |
| `ClassId` | `int?` |
| `LocationId` | `int?` |
| `Date` | `DateTime` |
| `Points` | `int` |
| `Comment` | `string?` |
| `InternalComment` | `string?` |

### `Department`

| Property | Type |
| --- | --- |
| `Id` | `int` |
| `Name` | `string?` |
| `HeadOfDepartmentId` | `int?` |
| `LeaderIds` | `IReadOnlyList<int>` |
| `TeacherIds` | `IReadOnlyList<int>` |
| `Subjects` | `IReadOnlyList<Subject>` |

#### `Subject`

| Property | Type |
| --- | --- |
| `Id` | `int` |
| `Name` | `string?` |
| `Code` | `string?` |

### `AssessmentResult`

| Property | Type |
| --- | --- |
| `StudentId` | `int` |
| `Type` | `string` |
| `YearGroup` | `int?` |
| `Term` | `string?` |
| `Subject` | `string?` |
| `Result` | `string` |

### `StudentWeeklyAttendance`

| Property | Type |
| --- | --- |
| `StudentId` | `int` |
| `Attendances` | `IReadOnlyList<SessionAttendance>` |
| `Percentage` | `decimal` |

### `PeriodAttendance`

| Property | Type |
| --- | --- |
| `StudentId` | `int` |
| `Date` | `DateOnly` |
| `PeriodName` | `string` |
| `Code` | `string?` |
| `Comment` | `string?` |
| `Category` | `AttendanceCategory` |

#### `SessionAttendance`

| Property | Type |
| --- | --- |
| `DayOfWeek` | `DayOfWeek` |
| `Session` | `SessionType` |
| `Code` | `string?` |
| `Category` | `AttendanceCategory` |

#### `SessionType`

| Value |
| --- |
| `AM` |
| `PM` |

#### `AttendanceCategory`

| Value |
| --- |
| `NotEntered` |
| `Present` |
| `ApprovedEducationalActivity` |
| `AuthorisedAbsence` |
| `UnauthorisedAbsence` |
| `NotPossibleAttendance` |
| `Invalid` |

## Upstream API endpoints

* `/v2/AssociationAssessmentResultsRaw`
* `/v2/BasicAttendance`
* `/v2/BehaviourEventRecords`
* `/v2/BehaviourEvents`
* `/v2/Departments`
* `/v2/DepartmentTeachers`
* `/v2/RoomCovers`
* `/v2/Staff`
* `/v2/StaffAbsences`
* `/v2/StaffCovers`
* `/v2/StaffLineManagers`
* `/v2/StudentAttendanceByWeek`
* `/v2/StudentFlatView`
* `/v2/StudentParentalConsent`
* `/v2/StudentTimetables`
* `/v2/Subjects`
* `/v2/Timetable`
* `/v2/YearGroupSubjectStudents`
