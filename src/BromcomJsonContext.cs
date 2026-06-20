using System.Text.Json;
using System.Text.Json.Serialization;

namespace BromcomEssentials;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(ApiResponse<StudentFlatViewContract>))]
[JsonSerializable(typeof(ApiResponse<YearGroupSubjectStudentContract>))]
[JsonSerializable(typeof(ApiResponse<StaffContract>))]
[JsonSerializable(typeof(ApiResponse<StaffAbsenceContract>))]
[JsonSerializable(typeof(ApiResponse<StaffLineManagerContract>))]
[JsonSerializable(typeof(ApiResponse<RoomCoverContract>))]
[JsonSerializable(typeof(ApiResponse<StaffCoverContract>))]
[JsonSerializable(typeof(ApiResponse<ParentalConsentContract>))]
[JsonSerializable(typeof(ApiResponse<BehaviourTypeContract>))]
[JsonSerializable(typeof(ApiResponse<BehaviourEventContract>))]
[JsonSerializable(typeof(ApiResponse<TimetableContract>))]
[JsonSerializable(typeof(ApiResponse<DepartmentContract>))]
[JsonSerializable(typeof(ApiResponse<DepartmentTeacherContract>))]
[JsonSerializable(typeof(ApiResponse<SubjectContract>))]
[JsonSerializable(typeof(ApiResponse<AssessmentResultContract>))]
[JsonSerializable(typeof(ApiResponse<StudentAttendanceByWeekContract>))]
[JsonSerializable(typeof(ApiResponse<BasicAttendanceContract>))]
internal sealed partial class BromcomJsonContext : JsonSerializerContext
{
}
