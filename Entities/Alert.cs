using System;
using System.ComponentModel.DataAnnotations;

namespace MangaAlert.Entities
{
  public record Alert
  {
    public Guid Id { get; init; }

    public string Title { get; init; }

    public string Url { get; init; }

    public int LatestChapter { get; init; }

    public bool HasReadLatestChapter { get; init; }

    public DateTimeOffset? LatestChapterUpdatedAt { get; init; }

    public bool HasCompleted { get; init; }

    public DateTimeOffset? CompletedAt { get; init; }

    public DateTimeOffset? CreatedAt { get; init; }

    public string Status { get; init; }

    public Guid UserId { get; init; }
  }
}
