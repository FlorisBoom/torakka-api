using System.ComponentModel.DataAnnotations;

namespace MangaAlert.Dtos
{
  public record AuthDto
  {
    public string Email { get; init; }

    public string Password { get; init; }
  }
}
