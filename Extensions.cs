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
        HasCompleted = alert.HasCompleted,
        CompletedAt = alert.CompletedAt,
        UserId = alert.UserId,
      };
    }
  }
}
