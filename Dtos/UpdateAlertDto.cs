using System;
using System.ComponentModel.DataAnnotations;

namespace MangaAlert.Dtos
{
  public record UpdateAlertDto
  {
    [Required]
    public string Title { get; init; }

    [Required]
    public string Type { get; init; }

    [Required]
    public string Url { get; init; }

    [Required]
    public int UserReleaseProgress { get; init; }

    [Required]
    public int LatestRelease { get; init; }

    [Required]
    public string Status { get; init; }
  }
}
