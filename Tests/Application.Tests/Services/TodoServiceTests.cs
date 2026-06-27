using Application.DTOs.Todo;
using Application.RepositoryInterfaces;
using Application.Services;
using Domain.Entities;
using Moq;

namespace Application.Tests.Services;

public class TodoServiceTests
{
    private readonly Mock<ITodoRepository> _todoRepo = new();
    private readonly Mock<IProjectRepository> _projectRepo = new();
    private readonly TodoService _sut;

    public TodoServiceTests()
    {
        _sut = new TodoService(_todoRepo.Object, _projectRepo.Object);
    }

    private static TodoTask MakeTask(int id = 1, int userId = 1, int projectId = 1,
        string status = "Pending", string priority = "Medium") => new()
    {
        Id = id,
        UserId = userId,
        ProjectId = projectId,
        Title = $"Task {id}",
        Status = status,
        Priority = priority,
        IsCompleted = status == "Completed",
        Project = new Project { Id = projectId, Name = "Project A" }
    };

    // --- GetAllAsync ---

    [Fact]
    public async Task GetAllAsync_ReturnsMappedDtos()
    {
        var tasks = new List<TodoTask> { MakeTask(1), MakeTask(2) };
        _todoRepo.Setup(r => r.GetAllAsync(1, null, null, null, null)).ReturnsAsync(tasks);

        var result = await _sut.GetAllAsync(1, null, null, null, null);

        Assert.True(result.Success);
        Assert.Equal(2, result.Data!.Count);
    }

    // --- GetByIdAsync ---

    [Fact]
    public async Task GetByIdAsync_TaskNotFound_ReturnsFailure()
    {
        _todoRepo.Setup(r => r.GetByIdAsync(99, 1)).ReturnsAsync((TodoTask?)null);

        var result = await _sut.GetByIdAsync(99, 1);

        Assert.False(result.Success);
        Assert.Equal("Task not found.", result.Message);
    }

    [Fact]
    public async Task GetByIdAsync_TaskFound_ReturnsMappedDto()
    {
        var task = MakeTask(5);
        _todoRepo.Setup(r => r.GetByIdAsync(5, 1)).ReturnsAsync(task);

        var result = await _sut.GetByIdAsync(5, 1);

        Assert.True(result.Success);
        Assert.Equal(5, result.Data!.Id);
        Assert.Equal("Task 5", result.Data.Title);
        Assert.Equal("Project A", result.Data.ProjectName);
    }

    // --- CreateAsync ---

    [Fact]
    public async Task CreateAsync_ProjectNotFound_ReturnsFailure()
    {
        _projectRepo.Setup(r => r.ExistsAsync(1, 1)).ReturnsAsync(false);

        var result = await _sut.CreateAsync(new TodoCreateDto
        {
            ProjectId = 1, Title = "T", Priority = "Low", Status = "Pending"
        }, userId: 1);

        Assert.False(result.Success);
        Assert.Equal("Project not found or access denied.", result.Message);
        _todoRepo.Verify(r => r.AddAsync(It.IsAny<TodoTask>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ValidData_CreatesAndReturnsTodoDto()
    {
        _projectRepo.Setup(r => r.ExistsAsync(1, 1)).ReturnsAsync(true);

        var saved = MakeTask(10, status: "Pending");
        _todoRepo.Setup(r => r.AddAsync(It.IsAny<TodoTask>())).ReturnsAsync(saved);
        _todoRepo.Setup(r => r.GetByIdAsync(10, 1)).ReturnsAsync(saved);

        var result = await _sut.CreateAsync(new TodoCreateDto
        {
            ProjectId = 1, Title = "New Task", Priority = "High", Status = "Pending"
        }, userId: 1);

        Assert.True(result.Success);
        Assert.Equal("Task created.", result.Message);
        Assert.Equal(10, result.Data!.Id);
    }

    [Fact]
    public async Task CreateAsync_StatusCompleted_SetsIsCompletedTrue()
    {
        _projectRepo.Setup(r => r.ExistsAsync(1, 1)).ReturnsAsync(true);

        TodoTask? captured = null;
        var saved = MakeTask(11, status: "Completed");
        _todoRepo.Setup(r => r.AddAsync(It.IsAny<TodoTask>()))
                 .Callback<TodoTask>(t => captured = t)
                 .ReturnsAsync(saved);
        _todoRepo.Setup(r => r.GetByIdAsync(11, 1)).ReturnsAsync(saved);

        await _sut.CreateAsync(new TodoCreateDto
        {
            ProjectId = 1, Title = "Done", Priority = "Low", Status = "Completed"
        }, userId: 1);

        Assert.NotNull(captured);
        Assert.True(captured!.IsCompleted);
    }

    // --- UpdateAsync ---

    [Fact]
    public async Task UpdateAsync_TaskNotFound_ReturnsFailure()
    {
        _todoRepo.Setup(r => r.GetByIdAsync(99, 1)).ReturnsAsync((TodoTask?)null);

        var result = await _sut.UpdateAsync(99, new TodoUpdateDto
        {
            ProjectId = 1, Title = "X", Priority = "Low", Status = "Pending"
        }, 1);

        Assert.False(result.Success);
        Assert.Equal("Task not found.", result.Message);
    }

    [Fact]
    public async Task UpdateAsync_ProjectNotFound_ReturnsFailure()
    {
        _todoRepo.Setup(r => r.GetByIdAsync(1, 1)).ReturnsAsync(MakeTask(1));
        _projectRepo.Setup(r => r.ExistsAsync(99, 1)).ReturnsAsync(false);

        var result = await _sut.UpdateAsync(1, new TodoUpdateDto
        {
            ProjectId = 99, Title = "X", Priority = "Low", Status = "Pending"
        }, 1);

        Assert.False(result.Success);
        Assert.Equal("Project not found or access denied.", result.Message);
    }

    [Fact]
    public async Task UpdateAsync_ValidData_UpdatesAndReturnsDto()
    {
        var existing = MakeTask(2);
        var withProject = MakeTask(2, status: "InProgress");

        _todoRepo.SetupSequence(r => r.GetByIdAsync(2, 1))
                 .ReturnsAsync(existing)
                 .ReturnsAsync(withProject);
        _projectRepo.Setup(r => r.ExistsAsync(1, 1)).ReturnsAsync(true);
        _todoRepo.Setup(r => r.UpdateAsync(It.IsAny<TodoTask>())).ReturnsAsync(existing);

        var result = await _sut.UpdateAsync(2, new TodoUpdateDto
        {
            ProjectId = 1, Title = "Updated", Priority = "High", Status = "InProgress"
        }, 1);

        Assert.True(result.Success);
        Assert.Equal("Task updated.", result.Message);
    }

    // --- UpdateStatusAsync ---

    [Fact]
    public async Task UpdateStatusAsync_TaskNotFound_ReturnsFailure()
    {
        _todoRepo.Setup(r => r.GetByIdAsync(99, 1)).ReturnsAsync((TodoTask?)null);

        var result = await _sut.UpdateStatusAsync(99, "Completed", 1);

        Assert.False(result.Success);
        Assert.Equal("Task not found.", result.Message);
    }

    [Fact]
    public async Task UpdateStatusAsync_SetsStatusAndIsCompleted()
    {
        var task = MakeTask(3, status: "Pending");
        _todoRepo.Setup(r => r.GetByIdAsync(3, 1)).ReturnsAsync(task);
        _todoRepo.Setup(r => r.UpdateAsync(task)).ReturnsAsync(task);

        var withProject = MakeTask(3, status: "Completed");
        withProject.IsCompleted = true;
        _todoRepo.SetupSequence(r => r.GetByIdAsync(3, 1))
                 .ReturnsAsync(task)
                 .ReturnsAsync(withProject);

        var result = await _sut.UpdateStatusAsync(3, "Completed", 1);

        Assert.True(result.Success);
        Assert.Equal("Status updated.", result.Message);
        Assert.True(task.IsCompleted);
        Assert.Equal("Completed", task.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_NonCompleted_SetsIsCompletedFalse()
    {
        var task = MakeTask(4, status: "Completed");
        task.IsCompleted = true;
        _todoRepo.Setup(r => r.GetByIdAsync(4, 1)).ReturnsAsync(task);
        _todoRepo.Setup(r => r.UpdateAsync(task)).ReturnsAsync(task);

        var afterUpdate = MakeTask(4, status: "Pending");
        _todoRepo.SetupSequence(r => r.GetByIdAsync(4, 1))
                 .ReturnsAsync(task)
                 .ReturnsAsync(afterUpdate);

        await _sut.UpdateStatusAsync(4, "Pending", 1);

        Assert.False(task.IsCompleted);
        Assert.Equal("Pending", task.Status);
    }

    // --- DeleteAsync ---

    [Fact]
    public async Task DeleteAsync_TaskNotFound_ReturnsFailure()
    {
        _todoRepo.Setup(r => r.GetByIdAsync(99, 1)).ReturnsAsync((TodoTask?)null);

        var result = await _sut.DeleteAsync(99, 1);

        Assert.False(result.Success);
        Assert.Equal("Task not found.", result.Message);
        _todoRepo.Verify(r => r.SoftDeleteAsync(It.IsAny<TodoTask>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_TaskFound_CallsSoftDeleteAndReturnsSuccess()
    {
        var task = MakeTask(6);
        _todoRepo.Setup(r => r.GetByIdAsync(6, 1)).ReturnsAsync(task);
        _todoRepo.Setup(r => r.SoftDeleteAsync(task)).Returns(Task.CompletedTask);

        var result = await _sut.DeleteAsync(6, 1);

        Assert.True(result.Success);
        Assert.Equal("Task deleted.", result.Message);
        _todoRepo.Verify(r => r.SoftDeleteAsync(task), Times.Once);
    }
}
