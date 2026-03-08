# Bromcom Essentials .NET SDK

Retrieve basic staff, student, and department data from the [Bromcom Partner API](https://partner.bromcomcloud.com) in a .NET application.

> This repository is not affiliated with Bromcom.

## Usage

```csharp
using BromcomEssentials;

using var client = new BromcomClient(applicationId, applicationSecret);
var students = await client.GetStudentsAsync(schoolId, includeClasses: true, includeTimetable: true);
var staff = await client.GetStaffAsync(schoolId, includeClassesAndTimetable: true);
var departments = await client.GetDepartmentsAsync(schoolId);
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
| `YearGroup` | `int?` |
| `TutorGroup` | `string?` |
| `Parents` | `IReadOnlyList<ParentContact>` |
| `Classes` | `IReadOnlyList<string>` |
| `Timetable` | `IReadOnlyList<StudentTimetableEntry>` |

#### `ParentContact`

| Property | Type |
| --- | --- |
| `Name` | `string?` |
| `Telephone` | `string?` |
| `Email` | `string?` |
| `Relationship` | `string?` |

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

## Upstream API endpoints

* `/v2/Departments`
* `/v2/DepartmentTeachers`
* `/v2/Staff`
* `/v2/StudentFlatView`
* `/v2/StudentTimetables`
* `/v2/Subjects`
* `/v2/Timetable`
* `/v2/YearGroupSubjectStudents`
