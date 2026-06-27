namespace Application.DTOs.Dashboard;

public class DashboardStatsDto
{
    public int TotalProjects { get; set; }
    public int PendingTasks { get; set; }
    public int HighPriorityTasks { get; set; }
    public int CompletedTasks { get; set; }
    public List<ProjectTaskChartDto> ProjectTaskChart { get; set; } = new();
    public List<TaskStatusChartDto> TaskStatusChart { get; set; } = new();
}

public class ProjectTaskChartDto
{
    public string ProjectName { get; set; } = string.Empty;
    public int TaskCount { get; set; }
}

public class TaskStatusChartDto
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}
