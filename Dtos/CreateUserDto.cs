using System.ComponentModel.DataAnnotations;

namespace MangaAlert.Dtos
{
  public class CreateUserDto
  {
    [Required]
    public string UserName { get; init; }

    [Required]
    public string Email { get; init; }

    [Required]
    [StringLength(8)]
    public string Password { get; init; }
  }
}
