using System;
using System.Linq;
using System.Threading.Tasks;
using MangaAlert.Dtos;
using MangaAlert.Entities;
using MangaAlert.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MangaAlert.Controllers
{
  [ApiController]
  [Authorize]
  [Produces("application/json")]
  [Route("[controller]")]
  public class AlertsController: ControllerBase
  {
    private enum StatusTypes
    {
      Reading,
      OnHold,
      PlanToRead,
      Completed,
      Dropped
    }

    private enum SortTypes
    {
      HasReadLatestChapter,
      Title,
      LatestChapterUpdatedAt
    }

    private enum SortOptions
    {
      Desc = -1,
      Asc = 1
    }

    private string[] _supportedWebsites = {
      "mangakakalot.com",
      "pahe.win",
      "mangahub.io",
      "reader.deathtollscans.net",
      "toomics.com"
    };

    private readonly IAlertRepository _alertRepository;
    private readonly IUserRepository _userRepository;

    public AlertsController(IAlertRepository alertRepository, IUserRepository userRepository)
    {
      this._alertRepository = alertRepository;
      this._userRepository = userRepository;
    }

    // GET /alerts/{userId}/{alertId}
    [HttpGet("{userId}/{alertId}")]
    public async Task<ActionResult<AlertDto>> GetAlertForUser(Guid userId, Guid alertId)
    {
      if (User.Identity.Name != userId.ToString()) {
        return StatusCode(403, new {
          message = "User does not have the permission to access this alert."
        });
      }

      Alert alert = await _alertRepository.GetAlertForUser(alertId, userId);

      if (alert is null) return NotFound();

      return Ok(new {
        data = alert.AsDto()
      });
    }

    // GET /alerts/{userId}
    [HttpGet("{userId}")]
    public async Task<ActionResult<AlertDto>> GetAlertsForUser(Guid userId, QueryOptionsDto queryOptionsDto)
    {
      if (User.Identity.Name != userId.ToString()) {
        return StatusCode(403, new {
          message = "User does not have the access to this users alerts."
        });
      }

      if (!string.IsNullOrWhiteSpace(queryOptionsDto.Status)) {
        if (!Enum.IsDefined(typeof(StatusTypes), queryOptionsDto.Status.FirstCharToUpper())) {
          return StatusCode(422, new {
            message = "Passed status not allowed."
          });
        }
      }

      if (!string.IsNullOrWhiteSpace(queryOptionsDto.SortBy)) {
        if (!Enum.IsDefined(typeof(SortTypes), queryOptionsDto.SortBy.FirstCharToUpper())) {
          return StatusCode(422, new {
            message = "Passed sort by not allowed."
          });
        }
      }

      if (!string.IsNullOrWhiteSpace(queryOptionsDto.SortOption)) {
        if (!Enum.IsDefined(typeof(SortOptions), queryOptionsDto.SortOption.FirstCharToUpper())) {
          return StatusCode(422, new {
            message = "Passed sort option not allowed."
          });
        }
      }

      int? sortOption = null;
      if (queryOptionsDto.SortOption != null) {
        sortOption = (int)Enum.Parse(typeof(SortOptions), queryOptionsDto.SortOption.FirstCharToUpper());
      }

      var alerts = (await _alertRepository.GetAlertsForUser(
          userId,
          queryOptionsDto.Status,
          queryOptionsDto.Limit,
          queryOptionsDto.SortBy,
          sortOption,
          queryOptionsDto.Page
          ))
        .Select(alert => alert.AsDto());

      return Ok(new {
        data = alerts
      });
    }

    // POST /alerts/{userId}
    [HttpPost("{userId}")]
    public async Task<ActionResult<AlertDto>> CreateAlert(Guid userId, CreateAlertDto alertDto)
    {
      var user = await _userRepository.GetUser(userId);

      if (user is null) {
        return NotFound(new {
          message = "user not found"
        });
      }

      if (User.Identity.Name != userId.ToString()) {
        return StatusCode(403, new {
          message = "User does not have the permission to create alert."
        });
      }

      if (!Enum.IsDefined(typeof(StatusTypes), alertDto.Status.FirstCharToUpper())) {
        return StatusCode(422, new {
          message = "Status not allowed."
        });
      }

      Alert alert = new() {
        Id = Guid.NewGuid(),
        Title = alertDto.Title,
        Url = alertDto.Url,
        LatestChapter = alertDto.LatestChapter,
        Status = alertDto.Status.FirstCharToUpper(),
        UserId = userId,
        CreatedAt = DateTimeOffset.Now
      };

      await _alertRepository.CreateAlert(alert);

      var returnObject = new {
        data = alert.AsDto()
      };

      return CreatedAtAction(nameof(GetAlertForUser), new {
        userId = alert.UserId, alertId = alert.Id
      }, returnObject);
    }

    // PUT /alerts/{userId}/{alertId}
    [HttpPut("{userId}/{alertId}")]
    public async Task<ActionResult<AlertDto>> UpdateAlert(Guid userId, Guid alertId, UpdateAlertDto alertDto)
    {
      if (User.Identity.Name != userId.ToString()) {
        return StatusCode(403, new {
          message = "User does not have the permission to update this alert."
        });
      }

      if (!Enum.IsDefined(typeof(StatusTypes), alertDto.Status.FirstCharToUpper())) {
        return StatusCode(422, new {
          message = "Status not allowed."
        });
      }

      Alert existingAlert = await _alertRepository.GetAlertForUser(alertId, userId);

      if (existingAlert is null) return NotFound();

      Alert updatedAlert = (existingAlert with {
        Title = alertDto.Title,
        Url = alertDto.Url,
        LatestChapter = alertDto.LatestChapter,
        Status = alertDto.Status,
      });

      await _alertRepository.UpdateAlert(updatedAlert);

      return Ok(new {
        data = updatedAlert.AsDto()
      });
    }

    // DELETE /alerts/{userId}/{alertId}
    [HttpDelete("{userId}/{alertId}")]
    public async Task<ActionResult> DeleteAlert(Guid userId, Guid alertId)
    {
      if (User.Identity.Name != userId.ToString()) {
        return StatusCode(403, new {
          message = "User does not have the permission to delete this alert."
        });
      }

      Alert existingAlert = await _alertRepository.GetAlertForUser(alertId, userId);

      if (existingAlert is null) return NotFound();

      await _alertRepository.DeleteAlert(alertId);

      return StatusCode(200,  new {
        data = "success"
      });
    }
  }
}
