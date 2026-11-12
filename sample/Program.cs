using System.Globalization;
using BromcomEssentials;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
var cfg = builder.Configuration;
cfg.AddUserSecrets<Program>();
var connectionString = builder.Configuration.GetConnectionString("AppConfiguration");
if (!string.IsNullOrWhiteSpace(connectionString))
  cfg.AddAzureAppConfiguration(options => options.Connect(connectionString).Select("Bromcom:*").TrimKeyPrefix("Bromcom:"));

var applicationId = cfg["BromcomApplicationId"] ?? throw new InvalidOperationException("Configuration value 'BromcomApplicationId' is required.");
var applicationSecret = cfg["BromcomApplicationSecret"] ?? throw new InvalidOperationException("Configuration value 'BromcomApplicationSecret' is required.");
var schoolIdValue = cfg["BromcomSchoolId"] ?? throw new InvalidOperationException("Configuration value 'BromcomSchoolId' is required.");

if (!int.TryParse(schoolIdValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var schoolId))
  throw new InvalidOperationException("Configuration value 'BromcomSchoolId' must be a valid integer.");

builder.Services.AddHttpClient<SchoolBromcomClient, SchoolBromcomClient>(httpClient => new(applicationId, applicationSecret, schoolId, httpClient));

using var host = builder.Build();
using var scope = host.Services.CreateScope();

var school = scope.ServiceProvider.GetRequiredService<SchoolBromcomClient>();
var today = DateOnly.FromDateTime(DateTime.Today);

var students = await school.GetStudentsAsync();
var staff = await school.GetStaffAsync();
var classes = await school.GetClassesAsync();
var photoIds = await school.GetPhotoIdsAsync();
var photo = photoIds.Count > 0 ? await school.GetPhotoAsync(photoIds[0].PhotoId) : null;
var documents = await school.GetStudentDocumentsAsync(startDate: today);
var documentContent = documents.Count > 0 ? await school.GetStudentDocumentContentAsync(documents[0].Id) : null;
var medicalConditions = await school.GetMedicalConditionsAsync();
var dietaryNeeds = await school.GetDietaryNeedsAsync();
var disabilities = await school.GetDisabilitiesAsync();
var clubs = await school.GetClubsAsync();
var clubAttendances = await school.GetClubAttendancesAsync(today);
var staffAbsences = await school.GetStaffAbsencesAsync(today);
var detentions = await school.GetDetentionsAsync(today);
var roomCovers = await school.GetRoomCoversAsync(today);
var staffCovers = await school.GetStaffCoversAsync(today);
var parentalConsents = await school.GetParentalConsentAsync();
var behaviourTypes = await school.GetBehaviourTypesAsync();
var behaviourOutcomes = await school.GetBehaviourOutcomesAsync();
var behaviourEvents = await school.GetBehaviourEventsAsync(today);
var departments = await school.GetDepartmentsAsync();
var columns = await school.GetColumnsAsync();
var results = await school.GetResultsAsync(2026, term: "Autumn", yearGroup: 7, gradesOnly: true);
var attendancePeriods = await school.GetAttendancePeriodsAsync(today, endDate: today);
var attendancesByWeek = await school.GetAttendancesByWeekAsync(today);
var attendances = await school.GetAttendancesAsync(today);
var udfData = await school.GetUdfDataAsync();

Console.WriteLine($"Students: {students.Count}");
Console.WriteLine($"Staff: {staff.Count}");
Console.WriteLine($"Classes: {classes.Count}");
Console.WriteLine($"Photo IDs: {photoIds.Count}");
Console.WriteLine($"First photo bytes: {photo?.Length ?? 0}");
Console.WriteLine($"Student documents: {documents.Count}");
Console.WriteLine($"First document bytes: {documentContent?.Length ?? 0}");
Console.WriteLine($"Medical conditions: {medicalConditions.Count}");
Console.WriteLine($"Dietary needs: {dietaryNeeds.Count}");
Console.WriteLine($"Disabilities: {disabilities.Count}");
Console.WriteLine($"Clubs: {clubs.Count}");
Console.WriteLine($"Club attendances: {clubAttendances.Count}");
Console.WriteLine($"Staff absences: {staffAbsences.Count}");
Console.WriteLine($"Detentions: {detentions.Count}");
Console.WriteLine($"Room covers: {roomCovers.Count}");
Console.WriteLine($"Staff covers: {staffCovers.Count}");
Console.WriteLine($"Parental consents: {parentalConsents.Count}");
Console.WriteLine($"Behaviour types: {behaviourTypes.Count}");
Console.WriteLine($"Behaviour outcomes: {behaviourOutcomes.Count}");
Console.WriteLine($"Behaviour events: {behaviourEvents.Count}");
Console.WriteLine($"Departments: {departments.Count}");
Console.WriteLine($"Columns: {columns.Count}");
Console.WriteLine($"Results: {results.Count}");
Console.WriteLine($"Attendance periods: {attendancePeriods.Count}");
Console.WriteLine($"Attendances by week: {attendancesByWeek.Count}");
Console.WriteLine($"Attendances: {attendances.Count}");
Console.WriteLine($"User defined field data: {udfData.Count}");
