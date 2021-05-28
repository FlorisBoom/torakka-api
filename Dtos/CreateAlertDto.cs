using System;
using System.ComponentModel.DataAnnotations;

namespace MangaAlert.Dtos
{
  public record CreateAlertDto
  {
    [Required]
    public string Title { get; init; }

    [Required]
    public string Type { get; init; }

    [Required]
    public string Url { get; init; }

    public int UserReleaseProgress { get; init; }

    public int LatestRelease { get; init; }

    [Required]
    public string Status { get; init; }
  }
}
