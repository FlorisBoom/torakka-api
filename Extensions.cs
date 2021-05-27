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
        Title = alert.Title,
        Url = alert.Url,
        LatestChapter = alert.LatestChapter,
        HasReadLatestChapter = alert.HasReadLatestChapter,
        LatestChapterUpdatedAt = alert.LatestChapterUpdatedAt,
        HasCompleted = alert.HasCompleted,
        CompletedAt = alert.CompletedAt,
        Status = alert.Status,
        CreatedAt = alert.CreatedAt,
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
