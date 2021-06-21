namespace MangaAlert
{
  public class Definitions
  {
    public enum TrackerTypes
    {
      All,
      Manga,
      Anime
    }

    public enum StatusTypes
    {
      ReadingAndWatching,
      PlanToReadAndWatch,
      OnHold,
      Completed,
      Dropped
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
      LatestReleaseUpdatedAt,
    }
  }
}
