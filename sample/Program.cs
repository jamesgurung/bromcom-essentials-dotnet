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

using var client = new BromcomClient(applicationId, applicationSecret);
var students = await client.GetStudentsAsync(schoolId, includeClasses: true, includeTimetable: true);
var staff = await client.GetStaffAsync(schoolId, includeClassesAndTimetable: true);
var departments = await client.GetDepartmentsAsync(schoolId);
var results = await client.GetResultsAsync(schoolId, 2025, term: "Spring", yearGroup: 7, gradesOnly: true);

Console.WriteLine($"Students: {students.Count}");
Console.WriteLine($"Staff: {staff.Count}");
Console.WriteLine($"Departments: {departments.Count}");
Console.WriteLine($"Results: {results.Count}");
