namespace MangaAlert.Dtos
{
  #nullable enable
  public class QueryOptionsDto
  {
    public string? Status { get; init; }

    public string? SortBy { get; init; }

    public string? SortOption { get; init; }

    public int? Limit { get; init; } = 50;

    public int? Page { get; init; } = 1;
  }
}
