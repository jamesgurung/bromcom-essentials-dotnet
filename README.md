# Bromcom Essentials .NET SDK

Retrieve basic staff, student, club, detention, department, attendance, cover, consent, behaviour, and assessment data from the [Bromcom Partner API](https://partner.bromcomcloud.com) in a .NET application.

> This repository is not affiliated with Bromcom.

## Usage

Register `SchoolBromcomClient` as a typed HTTP client, then retrieve it from a scope:

```csharp
using BromcomEssentials;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
var applicationId = builder.Configuration["BromcomApplicationId"]!;
var applicationSecret = builder.Configuration["BromcomApplicationSecret"]!;
var schoolId = int.Parse(builder.Configuration["BromcomSchoolId"]!);
builder.Services.AddHttpClient<SchoolBromcomClient, SchoolBromcomClient>(httpClient =>
  new(applicationId, applicationSecret, schoolId, httpClient));

using var host = builder.Build();
using var scope = host.Services.CreateScope();
var client = scope.ServiceProvider.GetRequiredService<SchoolBromcomClient>();
var today = DateOnly.FromDateTime(DateTime.Today);

var students = await client.GetStudentsAsync(includeClasses: true, includeTimetable: true);
var staff = await client.GetStaffAsync(includeClassesAndTimetable: true);
var clubs = await client.GetClubsAsync();
var clubAttendances = await client.GetClubAttendancesAsync(today);
var staffAbsences = await client.GetStaffAbsencesAsync(today);
var detentions = await client.GetDetentionsAsync(today);
var roomCovers = await client.GetRoomCoversAsync(today);
var staffCovers = await client.GetStaffCoversAsync(today);
var parentalConsents = await client.GetParentalConsentAsync(consentType: "U");
var behaviourTypes = await client.GetBehaviourTypesAsync();
var behaviourEvents = await client.GetBehaviourEventsAsync(today);
var departments = await client.GetDepartmentsAsync();
var columns = await client.GetColumnsAsync(term: "Spring", yearGroup: 7);
var results = await client.GetResultsAsync(2025, term: "Spring", yearGroup: 7, gradesOnly: true);
var attendances = await client.GetAttendancesByWeekAsync(today);
var periodAttendances = await client.GetAttendancesAsync(today);
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
| `Attendance` | `decimal?` |
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
| `TelephoneExtension` | `int?` |
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

### `Club`

| Property | Type |
| --- | --- |
| `Id` | `int` |
| `Name` | `string?` |
| `Description` | `string?` |
| `StartDate` | `DateOnly` |
| `EndDate` | `DateOnly?` |
| `Time` | `string?` |
| `DayOfWeek` | `string?` |
| `Length` | `string?` |
| `ReservedSpaces` | `int?` |
| `MembershipLimit` | `int?` |
| `McasLiveFrom` | `DateTime?` |
| `McasLiveUntil` | `DateTime?` |
| `StaffName` | `string?` |
| `Room` | `string?` |
| `AssociatedGroupName` | `string?` |
| `IsWaitingListEnabled` | `bool` |
| `IsTrip` | `bool` |

### `ClubStudentAttendance`

| Property | Type |
| --- | --- |
| `StudentId` | `int` |
| `ClubId` | `int` |
| `MembershipStartDate` | `DateOnly` |
| `MembershipEndDate` | `DateOnly?` |
| `Mark` | `string?` |
| `AttendanceDate` | `DateOnly?` |

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

### `Detention`

| Property | Type |
| --- | --- |
| `Id` | `int` |
| `StudentId` | `int` |
| `Type` | `string?` |
| `Description` | `string?` |
| `Start` | `DateTime` |
| `End` | `DateTime?` |
| `EmployeeId` | `int?` |
| `LocationId` | `int?` |
| `Mark` | `string?` |
| `IsScheduled` | `bool` |
| `IsAuthorised` | `bool` |
| `IsEscalated` | `bool` |
| `PeriodName` | `string?` |
| `EventRecordId` | `int?` |
| `Source` | `string?` |

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

### `AssessmentColumn`

| Property | Type |
| --- | --- |
| `Id` | `int?` |
| `Type` | `string` |
| `Term` | `string?` |
| `YearGroup` | `int?` |
| `Subject` | `string?` |

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

* `/v2/AssociationAssessmentColumns`
* `/v2/AssociationAssessmentResultsRaw`
* `/v2/BasicAttendance`
* `/v2/BehaviourEventRecords`
* `/v2/BehaviourEvents`
* `/v2/ClubDetails`
* `/v2/ClubStudentsAndAttendance`
* `/v2/Departments`
* `/v2/DepartmentTeachers`
* `/v2/RoomCovers`
* `/v2/Staff`
* `/v2/StaffAbsences`
* `/v2/StaffCovers`
* `/v2/StaffLineManagers`
* `/v2/StudentAttendanceByWeek`
* `/v2/StudentDetentions`
* `/v2/StudentFlatView`
* `/v2/StudentParentalConsent`
* `/v2/StudentTimetables`
* `/v2/Subjects`
* `/v2/Timetable`
* `/v2/YearGroupSubjectStudents`
