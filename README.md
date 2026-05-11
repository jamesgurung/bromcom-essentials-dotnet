# Bromcom Essentials .NET SDK

Retrieve basic staff, student, department, attendance, and assessment result data from the [Bromcom Partner API](https://partner.bromcomcloud.com) in a .NET application.

> This repository is not affiliated with Bromcom.

## Usage

```csharp
using BromcomEssentials;

using var client = new BromcomClient(applicationId, applicationSecret);
var students = await client.GetStudentsAsync(schoolId, includeClasses: true, includeTimetable: true);
var staff = await client.GetStaffAsync(schoolId, includeClassesAndTimetable: true);
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

### `StudentWeeklyAttendance`

Returns weekly AM/PM attendance marks for the week containing the requested date.

| Property | Type |
| --- | --- |
| `StudentId` | `int` |
| `Attendances` | `IReadOnlyList<SessionAttendance>` |
| `Percentage` | `decimal` |

### `PeriodAttendance`

Returns period attendance marks for the requested date. Set `periodName` to add the corresponding upstream entity filter.

| Property | Type |
| --- | --- |
| `StudentId` | `int` |
| `Date` | `DateOnly` |
| `PeriodName` | `string` |
| `Code` | `string?` |
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
| `Classes` | `IReadOnlyList<string>` |
| `Timetable` | `IReadOnlyList<StaffTimetableEntry>` |

#### `StaffTimetableEntry`

| Property | Type |
| --- | --- |
| `Period` | `string?` |
| `Class` | `string?` |
| `Room` | `string?` |

### `Department`

| Property | Type |
| --- | --- |
| `Id` | `int` |
| `Name` | `string?` |
| `HeadOfDepartmentId` | `int?` |
| `LeaderIds` | `IReadOnlyList<int>` |
| `TeacherIds` | `IReadOnlyList<int>` |
| `SubjectCodes` | `IReadOnlyList<string>` |

### `AssessmentResult`

Returns assessment results entered during the academic year that starts on 1 September of `academicYearStart` and ends on 31 August of the following year. Set `term`, `yearGroup`, and `gradesOnly` to add the corresponding upstream entity filters.

| Property | Type |
| --- | --- |
| `StudentId` | `int` |
| `Type` | `string` |
| `YearGroup` | `int?` |
| `Term` | `string?` |
| `Subject` | `string?` |
| `Result` | `string` |

## Upstream API endpoints

* `/v2/AssociationAssessmentResultsRaw`
* `/v2/BasicAttendance`
* `/v2/Departments`
* `/v2/DepartmentTeachers`
* `/v2/Staff`
* `/v2/StudentAttendanceByWeek`
* `/v2/StudentFlatView`
* `/v2/StudentTimetables`
* `/v2/Subjects`
* `/v2/Timetable`
* `/v2/YearGroupSubjectStudents`
