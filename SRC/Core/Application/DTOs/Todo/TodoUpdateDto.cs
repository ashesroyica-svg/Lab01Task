using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Todo;

public class TodoUpdateDto
{
    [Required]
    public int ProjectId { get; set; }

    [Required, MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required]
    public string Priority { get; set; } = "Medium";

    [Required]
    public string Status { get; set; } = "Pending";

    public DateTime? DueDate { get; set; }
}
