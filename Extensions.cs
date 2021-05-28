using System;
using System.Linq;
using MangaAlert.Dtos;
using MangaAlert.Entities;

namespace MangaAlert
{
  public static class Extensions
  {
    public static AlertDto AsDto(this Alert alert)
    {
      return new AlertDto
      {
        Id = alert.Id,
        Type = alert.Type,
        Title = alert.Title,
        Url = alert.Url,
        ReleaseProgress = alert.UserReleaseProgress,
        LatestRelease = alert.LatestRelease,
        HasSeenLatestRelease = alert.HasSeenLatestRelease,
        LatestReleaseUpdatedAt = alert.LatestReleaseUpdatedAt,
        HasCompleted = alert.HasCompleted,
        CompletedAt = alert.CompletedAt,
        CreatedAt = alert.CreatedAt,
        Status = alert.Status,
        UserId = alert.UserId,
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
