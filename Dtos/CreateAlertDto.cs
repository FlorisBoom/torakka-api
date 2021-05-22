using System;
using System.ComponentModel.DataAnnotations;

namespace MangaAlert.Dtos
{
  public record CreateAlertDto
  {
    [Required]
    public string Title { get; init; }

    [Required]
    public string Url { get; init; }

    public int LatestChapter { get; init; }
  }
}
