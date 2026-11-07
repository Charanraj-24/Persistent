using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.Exceptions;
using Reflect.Domain.Interfaces;
using Reflect.Domain.Interfaces.Repositories;
using Reflect.Domain.ProviderDashboard;
using Reflect.Domain.ProviderDashboard.Rtm;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Shared;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.TimeEntries;

public class ReviewHeartbeatSaveData
{
    public bool IsNew { get; set; }
    public int AcceptedSeconds { get; set; }
    public RtmTimeEntry Entry { get; set; }
}
