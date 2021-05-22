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
    public string Password { get; init; }
  }
}
