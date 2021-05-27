using System;
using System.ComponentModel.DataAnnotations;

namespace MangaAlert.Dtos
{
  public record UpdateAlertDto
  {
    [Required]
    public string Title { get; init; }

    [Required]
    public string Url { get; init; }

    [Required]
    public int LatestChapter { get; init; }

    [Required]
    public string Status { get; init; }
  }
}
