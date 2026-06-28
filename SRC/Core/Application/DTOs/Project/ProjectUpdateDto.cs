using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Project;

public class ProjectUpdateDto
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required, AllowedValues("Active", "OnHold", "Completed")]
    public string Status { get; set; } = "Active";

    [Required, AllowedValues("Low", "Medium", "High")]
    public string Priority { get; set; } = "Medium";

    public DateTime? DueDate { get; set; }
}
