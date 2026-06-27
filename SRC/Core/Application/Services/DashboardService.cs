using Application.DTOs.Dashboard;
using Application.RepositoryInterfaces;
using Application.ServiceInterfaces;
using Application.WrapperClass;

namespace Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IProjectRepository _projectRepository;
    private readonly ITodoRepository _todoRepository;

    public DashboardService(IProjectRepository projectRepository, ITodoRepository todoRepository)
    {
        _projectRepository = projectRepository;
        _todoRepository = todoRepository;
    }

    public async Task<ApiResponse<DashboardStatsDto>> GetStatsAsync(int userId)
    {
        var projects = await _projectRepository.GetAllAsync(userId, null, null, null);
        var pendingCount = await _todoRepository.CountByStatusAsync(userId, "Pending");
        var completedCount = await _todoRepository.CountByStatusAsync(userId, "Completed");
        var highPriorityCount = await _todoRepository.CountByPriorityAsync(userId, "High");
        var chartData = await _todoRepository.GetTaskCountPerProjectAsync(userId);
        var statusData = await _todoRepository.GetTaskStatusCountsAsync(userId);

        var stats = new DashboardStatsDto
        {
            TotalProjects = projects.Count,
            PendingTasks = pendingCount,
            HighPriorityTasks = highPriorityCount,
            CompletedTasks = completedCount,
            ProjectTaskChart = chartData.Select(x => new ProjectTaskChartDto
            {
                ProjectName = x.ProjectName,
                TaskCount = x.Count
            }).ToList(),
            TaskStatusChart = statusData.Select(x => new TaskStatusChartDto
            {
                Status = x.Status,
                Count = x.Count
            }).ToList()
        };

        return ApiResponse<DashboardStatsDto>.Ok(stats, "Stats fetched.");
    }
}
