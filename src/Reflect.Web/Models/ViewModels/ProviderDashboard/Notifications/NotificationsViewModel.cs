using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.Interfaces;
using Reflect.Domain.Interfaces.Repositories;
using Reflect.Domain.ProviderDashboard.Notifications;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Shared;
using Reflect.Web.Services.Notifications;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.Notifications;

/// <summary>Bell notifications for the signed-in provider: urgent clinical flags, RTM threshold reminders and pending attestations.</summary>
public class NotificationsViewModel
{
    public List<NotificationItemViewModel> Items { get; set; } = new();
    public int Unread => Items.Count(i => !i.IsRead);
    public int UnreadUrgent => Items.Count(i => !i.IsRead && i.Kind == NotificationKind.Urgent);

    public async Task PrepareData(ICurrentUserService currentUser, NotificationReadState readState, IProviderProfileRepository providers,
        IProviderClientRepository clients, IAppSessionRepository sessions, IMoodCheckInRepository checkIns, IAssessmentResultRepository assessments,
        IRtmTimeEntryRepository timeEntries, CancellationToken ct)
    {
        var snap = await CaseloadSnapshot.LoadAsync(currentUser, providers, clients, sessions, checkIns, assessments, timeEntries, null, ct);
        var all = new List<ProviderNotification>();
        foreach (var c in snap.Clients.Where(c => c.Status == ClientRtmStatus.Active))
        {
            var rtm = snap.EvaluateRtm(c);
            all.AddRange(ProviderNotificationRules.For(c.Id, c.FullName, snap.FlagsFor(c), rtm, snap.Today, snap.Today.AddDays(rtm.DaysRemainingInPeriod)));
        }
        var userId = currentUser.UserId ?? Guid.Empty;
        Items = ProviderNotificationRules.Order(all).Take(30).Select(n => new NotificationItemViewModel
        {
            Key = n.Key, Kind = n.Kind, ClientId = n.ClientId, Title = n.Title, Detail = n.Detail, Icon = n.Icon,
            IsRead = readState.IsRead(userId, n.Key)
        }).OrderBy(i => i.IsRead).ThenBy(i => i.Kind).ToList();
    }
}
