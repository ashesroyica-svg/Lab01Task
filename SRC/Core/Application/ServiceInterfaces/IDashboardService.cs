using Application.DTOs.Dashboard;
using Application.WrapperClass;

namespace Application.ServiceInterfaces;

public interface IDashboardService
{
    Task<ApiResponse<DashboardStatsDto>> GetStatsAsync(int userId);
}
