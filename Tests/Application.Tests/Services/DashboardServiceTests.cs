using Application.RepositoryInterfaces;
using Application.Services;
using Domain.Entities;
using Moq;

namespace Application.Tests.Services;

public class DashboardServiceTests
{
    private readonly Mock<IProjectRepository> _projectRepo = new();
    private readonly Mock<ITodoRepository> _todoRepo = new();
    private readonly DashboardService _sut;

    public DashboardServiceTests()
    {
        _sut = new DashboardService(_projectRepo.Object, _todoRepo.Object);
    }

    [Fact]
    public async Task GetStatsAsync_ReturnsCorrectKpiCounts()
    {
        _projectRepo.Setup(r => r.GetAllAsync(1, null, null, null))
                    .ReturnsAsync(new List<Project> { new(), new(), new() });

        _todoRepo.Setup(r => r.CountByStatusAsync(1, "Pending")).ReturnsAsync(5);
        _todoRepo.Setup(r => r.CountByStatusAsync(1, "Completed")).ReturnsAsync(8);
        _todoRepo.Setup(r => r.CountByPriorityAsync(1, "High")).ReturnsAsync(3);
        _todoRepo.Setup(r => r.GetTaskCountPerProjectAsync(1))
                 .ReturnsAsync(new List<(string, int)>
                 {
                     ("Alpha", 4),
                     ("Beta", 2)
                 });
        _todoRepo.Setup(r => r.GetTaskStatusCountsAsync(1))
                 .ReturnsAsync(new List<(string, int)>
                 {
                     ("Pending", 5),
                     ("Completed", 8)
                 });

        var result = await _sut.GetStatsAsync(1);

        Assert.True(result.Success);
        Assert.Equal("Stats fetched.", result.Message);
        Assert.Equal(3, result.Data!.TotalProjects);
        Assert.Equal(5, result.Data.PendingTasks);
        Assert.Equal(8, result.Data.CompletedTasks);
        Assert.Equal(3, result.Data.HighPriorityTasks);
    }

    [Fact]
    public async Task GetStatsAsync_MapsProjectTaskChartCorrectly()
    {
        _projectRepo.Setup(r => r.GetAllAsync(1, null, null, null)).ReturnsAsync(new List<Project>());
        _todoRepo.Setup(r => r.CountByStatusAsync(1, It.IsAny<string>())).ReturnsAsync(0);
        _todoRepo.Setup(r => r.CountByPriorityAsync(1, It.IsAny<string>())).ReturnsAsync(0);
        _todoRepo.Setup(r => r.GetTaskCountPerProjectAsync(1))
                 .ReturnsAsync(new List<(string, int)>
                 {
                     ("ProjectX", 10),
                     ("ProjectY", 3)
                 });
        _todoRepo.Setup(r => r.GetTaskStatusCountsAsync(1))
                 .ReturnsAsync(new List<(string, int)>());

        var result = await _sut.GetStatsAsync(1);

        Assert.Equal(2, result.Data!.ProjectTaskChart.Count);
        Assert.Equal("ProjectX", result.Data.ProjectTaskChart[0].ProjectName);
        Assert.Equal(10, result.Data.ProjectTaskChart[0].TaskCount);
        Assert.Equal("ProjectY", result.Data.ProjectTaskChart[1].ProjectName);
        Assert.Equal(3, result.Data.ProjectTaskChart[1].TaskCount);
    }

    [Fact]
    public async Task GetStatsAsync_MapsTaskStatusChartCorrectly()
    {
        _projectRepo.Setup(r => r.GetAllAsync(1, null, null, null)).ReturnsAsync(new List<Project>());
        _todoRepo.Setup(r => r.CountByStatusAsync(1, It.IsAny<string>())).ReturnsAsync(0);
        _todoRepo.Setup(r => r.CountByPriorityAsync(1, It.IsAny<string>())).ReturnsAsync(0);
        _todoRepo.Setup(r => r.GetTaskCountPerProjectAsync(1)).ReturnsAsync(new List<(string, int)>());
        _todoRepo.Setup(r => r.GetTaskStatusCountsAsync(1))
                 .ReturnsAsync(new List<(string, int)>
                 {
                     ("Pending", 7),
                     ("InProgress", 2),
                     ("Completed", 11)
                 });

        var result = await _sut.GetStatsAsync(1);

        Assert.Equal(3, result.Data!.TaskStatusChart.Count);
        Assert.Equal("Pending", result.Data.TaskStatusChart[0].Status);
        Assert.Equal(7, result.Data.TaskStatusChart[0].Count);
        Assert.Equal("Completed", result.Data.TaskStatusChart[2].Status);
        Assert.Equal(11, result.Data.TaskStatusChart[2].Count);
    }

    [Fact]
    public async Task GetStatsAsync_NoProjectsOrTasks_ReturnsZeroKpis()
    {
        _projectRepo.Setup(r => r.GetAllAsync(1, null, null, null)).ReturnsAsync(new List<Project>());
        _todoRepo.Setup(r => r.CountByStatusAsync(1, It.IsAny<string>())).ReturnsAsync(0);
        _todoRepo.Setup(r => r.CountByPriorityAsync(1, It.IsAny<string>())).ReturnsAsync(0);
        _todoRepo.Setup(r => r.GetTaskCountPerProjectAsync(1)).ReturnsAsync(new List<(string, int)>());
        _todoRepo.Setup(r => r.GetTaskStatusCountsAsync(1)).ReturnsAsync(new List<(string, int)>());

        var result = await _sut.GetStatsAsync(1);

        Assert.True(result.Success);
        Assert.Equal(0, result.Data!.TotalProjects);
        Assert.Equal(0, result.Data.PendingTasks);
        Assert.Equal(0, result.Data.CompletedTasks);
        Assert.Equal(0, result.Data.HighPriorityTasks);
        Assert.Empty(result.Data.ProjectTaskChart);
        Assert.Empty(result.Data.TaskStatusChart);
    }
}
