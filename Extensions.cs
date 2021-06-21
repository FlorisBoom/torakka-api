using System;
using System.Linq;
using MangaAlert.Dtos;
using MangaAlert.Entities;

namespace MangaAlert
{
  public static class Extensions
  {
    public static TrackerDto AsDto(this Tracker tracker)
    {
      return new TrackerDto
      {
        Id = tracker.Id,
        Type = tracker.Type,
        Title = tracker.Title,
        Url = tracker.Url,
        ReleaseProgress = tracker.UserReleaseProgress,
        LatestRelease = tracker.LatestRelease,
        HasSeenLatestRelease = tracker.HasSeenLatestRelease,
        LatestReleaseUpdatedAt = tracker.LatestReleaseUpdatedAt,
        HasCompleted = tracker.HasCompleted,
        CompletedAt = tracker.CompletedAt,
        CreatedAt = tracker.CreatedAt,
        Status = tracker.Status,
        ReleasesOn = tracker.ReleasesOn,
        UserId = tracker.UserId,
        ImageUrl = tracker.ImageUrl
      };
    }

    public static string FirstCharToUpper(this string input) =>
      input switch
      {
        null => throw new ArgumentNullException(nameof(input)),
        "" => throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input)),
        _ => input.First().ToString().ToUpper() + input.Substring(1)
      };
  }
}
