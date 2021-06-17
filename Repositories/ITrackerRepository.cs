using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using MangaAlert.Dtos;
using MangaAlert.Entities;

namespace MangaAlert.Repositories
{
  public interface ITrackerRepository
  {
    Task<Tracker> GetTrackerForUser (Guid trackerId, Guid userId);

    #nullable enable
    Task<IEnumerable<Tracker>> GetTrackersForUser(Guid userId,
      string? type,
      string? status,
      string? sort,
      int? limit,
      int? offset,
      bool hasCompleted,
      string? search);

    Task CreateTracker(Tracker tracker);

    Task UpdateTracker(Tracker tracker);

    Task DeleteTracker(Guid trackerId);

    Task<List<string>> GetAllUniqueTrackersByUrl();

    Task<List<string>> GetNextReleaseForUrl(string url);

    Task BulkUpdateTracker(string url, int latestRelease);

    Task ToggleReleaseSeen(Guid trackerId, bool seen);

    Task<long> GetTrackersCountForUser(
      Guid userId,
      string? type,
      string? status,
      string? search
      );

    Task ToggleComplete(Guid trackerId, bool completed);
  }
}
