using BromcomEssentials;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((_, builder) => builder.AddUserSecrets<Program>())
    .Build();

var cfg = host.Services.GetRequiredService<IConfiguration>();
var applicationId = cfg["applicationId"];
var applicationSecret = cfg["applicationSecret"];
var schoolId = int.Parse(cfg["schoolId"]);
var today = DateOnly.FromDateTime(DateTime.Today);

using var client = new BromcomClient(applicationId, applicationSecret);
var students = await client.GetStudentsAsync(schoolId, includeClasses: true, includeTimetable: true);
var staff = await client.GetStaffAsync(schoolId, includeClassesAndTimetable: true);
var staffAbsences = await client.GetStaffAbsencesAsync(schoolId, today);
var roomCovers = await client.GetRoomCoversAsync(schoolId, today);
var staffCovers = await client.GetStaffCoversAsync(schoolId, today);
var parentalConsents = await client.GetParentalConsentAsync(schoolId);
var behaviourTypes = await client.GetBehaviourTypesAsync(schoolId);
var behaviourEvents = await client.GetBehaviourEventsAsync(schoolId, today);
var departments = await client.GetDepartmentsAsync(schoolId);
var results = await client.GetResultsAsync(schoolId, 2025, term: "Spring", yearGroup: 7, gradesOnly: true);
var attendancesByWeek = await client.GetAttendancesByWeekAsync(schoolId, today);
var periodAttendances = await client.GetAttendancesAsync(schoolId, today);

Console.WriteLine($"Students: {students.Count}");
Console.WriteLine($"Staff: {staff.Count}");
Console.WriteLine($"Staff absences: {staffAbsences.Count}");
Console.WriteLine($"Room covers: {roomCovers.Count}");
Console.WriteLine($"Staff covers: {staffCovers.Count}");
Console.WriteLine($"Parental consents: {parentalConsents.Count}");
Console.WriteLine($"Behaviour types: {behaviourTypes.Count}");
Console.WriteLine($"Behaviour events: {behaviourEvents.Count}");
Console.WriteLine($"Departments: {departments.Count}");
Console.WriteLine($"Results: {results.Count}");
Console.WriteLine($"Attendances by week: {attendancesByWeek.Count}");
Console.WriteLine($"Period attendances: {periodAttendances.Count}");
