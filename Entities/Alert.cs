using System;
using System.ComponentModel.DataAnnotations;

namespace MangaAlert.Entities
{
  public record Alert
  {
    public Guid Id { get; init; }

    public string Title { get; init; }

    public string Url { get; init; }

    public string Thumbnail { get; init; }

    public int LatestChapter { get; init; }

    public bool HasReadLatestChapter { get; init; }

    public bool HasCompleted { get; init; }

    public DateTimeOffset? CompletedAt { get; init; }

    public Guid UserId { get; init; }
  }
}
