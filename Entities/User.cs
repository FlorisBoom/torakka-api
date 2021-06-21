using System;

namespace MangaAlert.Entities
{
  public record User
  {
    public Guid Id { get; init; }

    public string Email { get; init; }

    public string Password { get; init; }
  }
}
