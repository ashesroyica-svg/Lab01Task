using Application.DTOs.Project;
using Application.RepositoryInterfaces;
using Application.Services;
using Domain.Entities;
using Moq;

namespace Application.Tests.Services;

public class ProjectServiceTests
{
    private readonly Mock<IProjectRepository> _projectRepo = new();
    private readonly ProjectService _sut;

    public ProjectServiceTests()
    {
        _sut = new ProjectService(_projectRepo.Object);
    }

    // --- GetAllAsync ---

    [Fact]
    public async Task GetAllAsync_ReturnsMappedDtos()
    {
        var projects = new List<Project>
        {
            new() { Id = 1, Name = "Alpha", Status = "Active", Priority = "High", UserId = 1 },
            new() { Id = 2, Name = "Beta",  Status = "OnHold", Priority = "Low",  UserId = 1 }
        };
        _projectRepo.Setup(r => r.GetAllAsync(1, null, null, null)).ReturnsAsync(projects);

        var result = await _sut.GetAllAsync(1, null, null, null);

        Assert.True(result.Success);
        Assert.Equal(2, result.Data!.Count);
        Assert.Equal("Alpha", result.Data[0].Name);
        Assert.Equal("Beta", result.Data[1].Name);
    }

    [Fact]
    public async Task GetAllAsync_EmptyList_ReturnsEmptyData()
    {
        _projectRepo.Setup(r => r.GetAllAsync(1, null, null, null)).ReturnsAsync(new List<Project>());

        var result = await _sut.GetAllAsync(1, null, null, null);

        Assert.True(result.Success);
        Assert.Empty(result.Data!);
    }

    // --- GetByIdAsync ---

    [Fact]
    public async Task GetByIdAsync_ProjectNotFound_ReturnsFailure()
    {
        _projectRepo.Setup(r => r.GetByIdAsync(99, 1)).ReturnsAsync((Project?)null);

        var result = await _sut.GetByIdAsync(99, 1);

        Assert.False(result.Success);
        Assert.Equal("Project not found.", result.Message);
    }

    [Fact]
    public async Task GetByIdAsync_ProjectFound_ReturnsMappedDto()
    {
        var project = new Project { Id = 5, Name = "MyProject", Status = "Active", Priority = "Medium", UserId = 1 };
        _projectRepo.Setup(r => r.GetByIdAsync(5, 1)).ReturnsAsync(project);

        var result = await _sut.GetByIdAsync(5, 1);

        Assert.True(result.Success);
        Assert.Equal(5, result.Data!.Id);
        Assert.Equal("MyProject", result.Data.Name);
    }

    // --- CreateAsync ---

    [Fact]
    public async Task CreateAsync_ValidDto_CreatesAndReturnsMappedDto()
    {
        var dto = new ProjectCreateDto
        {
            Name = "New Project",
            Description = "Desc",
            Status = "Active",
            Priority = "High",
            DueDate = DateTime.UtcNow.AddDays(10)
        };

        _projectRepo.Setup(r => r.AddAsync(It.IsAny<Project>()))
                    .ReturnsAsync((Project p) => { p.Id = 10; return p; });

        var result = await _sut.CreateAsync(dto, userId: 1);

        Assert.True(result.Success);
        Assert.Equal("Project created.", result.Message);
        Assert.Equal(10, result.Data!.Id);
        Assert.Equal("New Project", result.Data.Name);
        _projectRepo.Verify(r => r.AddAsync(It.Is<Project>(p =>
            p.UserId == 1 &&
            p.Name == "New Project" &&
            p.Priority == "High")), Times.Once);
    }

    // --- UpdateAsync ---

    [Fact]
    public async Task UpdateAsync_ProjectNotFound_ReturnsFailure()
    {
        _projectRepo.Setup(r => r.GetByIdAsync(99, 1)).ReturnsAsync((Project?)null);

        var result = await _sut.UpdateAsync(99, new ProjectUpdateDto { Name = "X", Status = "Active", Priority = "Low" }, 1);

        Assert.False(result.Success);
        Assert.Equal("Project not found.", result.Message);
        _projectRepo.Verify(r => r.UpdateAsync(It.IsAny<Project>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ProjectFound_UpdatesFieldsAndReturnsDto()
    {
        var existing = new Project { Id = 3, Name = "Old", Status = "Active", Priority = "Low", UserId = 1 };
        _projectRepo.Setup(r => r.GetByIdAsync(3, 1)).ReturnsAsync(existing);
        _projectRepo.Setup(r => r.UpdateAsync(It.IsAny<Project>()))
                    .ReturnsAsync((Project p) => p);

        var dto = new ProjectUpdateDto
        {
            Name = "Updated",
            Description = "New desc",
            Status = "Completed",
            Priority = "High",
            DueDate = DateTime.UtcNow.AddDays(5)
        };

        var result = await _sut.UpdateAsync(3, dto, 1);

        Assert.True(result.Success);
        Assert.Equal("Project updated.", result.Message);
        Assert.Equal("Updated", result.Data!.Name);
        Assert.Equal("Completed", result.Data.Status);
        Assert.Equal("High", result.Data.Priority);
    }

    // --- DeleteAsync ---

    [Fact]
    public async Task DeleteAsync_ProjectNotFound_ReturnsFailure()
    {
        _projectRepo.Setup(r => r.GetByIdAsync(99, 1)).ReturnsAsync((Project?)null);

        var result = await _sut.DeleteAsync(99, 1);

        Assert.False(result.Success);
        Assert.Equal("Project not found.", result.Message);
        _projectRepo.Verify(r => r.SoftDeleteAsync(It.IsAny<Project>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ProjectFound_CallsSoftDeleteAndReturnsSuccess()
    {
        var project = new Project { Id = 4, Name = "ToDelete", UserId = 1 };
        _projectRepo.Setup(r => r.GetByIdAsync(4, 1)).ReturnsAsync(project);
        _projectRepo.Setup(r => r.SoftDeleteAsync(project)).Returns(Task.CompletedTask);

        var result = await _sut.DeleteAsync(4, 1);

        Assert.True(result.Success);
        Assert.Equal("Project deleted.", result.Message);
        _projectRepo.Verify(r => r.SoftDeleteAsync(project), Times.Once);
    }

    // --- TaskCount mapping ---

    [Fact]
    public async Task GetAllAsync_TaskCountExcludesDeletedTasks()
    {
        var project = new Project
        {
            Id = 1,
            Name = "P",
            Status = "Active",
            Priority = "Low",
            UserId = 1,
            Tasks = new List<Domain.Entities.TodoTask>
            {
                new() { Id = 1, IsDeleted = false },
                new() { Id = 2, IsDeleted = true },
                new() { Id = 3, IsDeleted = false }
            }
        };
        _projectRepo.Setup(r => r.GetAllAsync(1, null, null, null)).ReturnsAsync(new List<Project> { project });

        var result = await _sut.GetAllAsync(1, null, null, null);

        Assert.Equal(2, result.Data![0].TaskCount);
    }
}
