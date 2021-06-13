using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MangaAlert.Entities;

namespace MangaAlert.Repositories
{
  public interface IAlertRepository
  {
    Task<Alert> GetAlertForUser (Guid alertId, Guid userId);

    #nullable enable
    Task<IEnumerable<Alert>> GetAlertsForUser (
      Guid userId,
      string type,
      string? status,
      string? sort,
      int? limit,
      int? offset,
      bool hasCompleted
      );

    Task CreateAlert(Alert alert);

    Task UpdateAlert(Alert alert);

    Task DeleteAlert(Guid alertId);

    Task<List<string>> GetAllUniqueAlertsByUrl();

    Task<List<string>> GetNextReleaseForUrl(string url);

    Task BulkUpdateAlert(string url, int latestRelease);

    Task ToggleReleaseSeen(Guid alertId, bool seen);

    Task<long> GetAlertsCountForUser(Guid userId, string type, string? status);

    Task ToggleComplete(Guid alertId, bool completed);
  }
}
