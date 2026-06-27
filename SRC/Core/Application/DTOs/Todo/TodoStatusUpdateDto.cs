using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Todo;

public class TodoStatusUpdateDto
{
    [Required]
    public string Status { get; set; } = string.Empty;
}
