namespace Domain.Entities;

public class Project
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "Active";
    public string Priority { get; set; } = "Medium";
    public DateTime? DueDate { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; }

    public User User { get; set; } = null!;
    public ICollection<TodoTask> Tasks { get; set; } = new List<TodoTask>();
}
