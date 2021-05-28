using System;
using System.ComponentModel.DataAnnotations;

namespace MangaAlert.Dtos
{
  public record AlertDto
  {
    [Required]
    public Guid Id { get; init; }

    [Required]
    public string Type { get; init; }

    [Required]
    public string Title { get; init; }

    [Required]
    public string Url { get; init; }

    public int ReleaseProgress { get; init; }

    public int LatestRelease { get; init; }

    public bool HasSeenLatestRelease { get; init; }

    public DateTimeOffset? LatestReleaseUpdatedAt { get; init; }

    public bool HasCompleted { get; init; }

    public DateTimeOffset? CompletedAt { get; init; }

    public DateTimeOffset? CreatedAt { get; init; }

    public string Status { get; init; }

    [Required]
    public Guid UserId { get; init; }
   }
}
