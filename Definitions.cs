namespace MangaAlert
{
  public class Definitions
  {
    public enum AlertTypes
    {
      Manga,
      Anime
    }

    public enum MangaStatusTypes
    {
      Reading,
      PlanToRead,
      OnHold,
      Completed,
      Dropped
    }

    public enum AnimeStatusTypes
    {
      Watching,
      PlanToWatch,
      OnHold,
      Completed,
      Dropped
    }

    public enum SortTypes
    {
      Title,
      HasReadLatestChapter,
    }
  }
}
