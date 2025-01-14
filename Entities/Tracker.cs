﻿using System;
using System.IO;

namespace MangaAlert.Entities
{
  public record Tracker
  {
    public Guid Id { get; init; }

    public string Type { get; init; }

    public string Title { get; init; }

    public string Url { get; init; }

    public int UserReleaseProgress { get; init; }

    public int LatestRelease { get; init; }

    public bool HasSeenLatestRelease { get; init; }

    public DateTimeOffset? LatestReleaseUpdatedAt { get; init; }

    public bool HasCompleted { get; init; }

    public DateTimeOffset? CompletedAt { get; init; }

    public DateTimeOffset? CreatedAt { get; init; }

    public string Status { get; init; }

    public string ReleasesOn { get; init; }

    public string ImageUrl { get; init; }

    public Guid UserId { get; init; }
  }
}
