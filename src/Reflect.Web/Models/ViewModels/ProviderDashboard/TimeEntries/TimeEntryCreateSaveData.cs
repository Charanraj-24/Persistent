using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.Exceptions;
using Reflect.Domain.Interfaces;
using Reflect.Domain.Interfaces.Repositories;
using Reflect.Domain.ProviderDashboard;
using Reflect.Domain.ProviderDashboard.Rtm;
using Reflect.Web.Messages;
using Reflect.Web.Models.Validation;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Shared;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.TimeEntries;

public class TimeEntryCreateSaveData
{
    public RtmTimeEntry Entry { get; set; }
}
