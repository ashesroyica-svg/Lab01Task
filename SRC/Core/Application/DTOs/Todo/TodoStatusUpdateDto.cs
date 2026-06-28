using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Todo;

public class TodoStatusUpdateDto
{
    [Required, AllowedValues("Pending", "InProgress", "Completed")]
    public string Status { get; set; } = string.Empty;
}
