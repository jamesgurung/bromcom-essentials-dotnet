using System.Text.Json;
using System.Text.Json.Serialization;

namespace BromcomEssentials;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(ApiResponse<StudentFlatViewContract>))]
[JsonSerializable(typeof(ApiResponse<YearGroupSubjectStudentContract>))]
[JsonSerializable(typeof(ApiResponse<StaffContract>))]
[JsonSerializable(typeof(ApiResponse<TimetableContract>))]
[JsonSerializable(typeof(ApiResponse<DepartmentContract>))]
[JsonSerializable(typeof(ApiResponse<DepartmentTeacherContract>))]
[JsonSerializable(typeof(ApiResponse<SubjectContract>))]
internal sealed partial class BromcomJsonContext : JsonSerializerContext
{
}
