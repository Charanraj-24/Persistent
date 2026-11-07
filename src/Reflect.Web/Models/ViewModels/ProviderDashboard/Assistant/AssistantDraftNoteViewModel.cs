using Reflect.Domain.Interfaces;
using Reflect.Domain.Interfaces.Repositories;
using Reflect.Domain.ProviderDashboard;
using Reflect.Domain.ProviderDashboard.Assistant;
using Reflect.Domain.ProviderDashboard.Rtm;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Shared;
using Reflect.Web.Services.Assistant;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.Assistant;

/// <summary>AJAX: draft an RTM documentation note for one client from their data. Always returns a template draft;
/// the controller asks the AI to polish it when an AI key is configured.</summary>
public class AssistantDraftNoteViewModel
{
    public Guid ClientId { get; set; }

    public string Draft { get; private set; } = string.Empty;
    public AssistantPrompt PolishPrompt { get; private set; }
    public Pseudonymizer Pseudonymizer { get; } = new();

    private static readonly TimeZoneInfo Eastern = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    public async Task PrepareData(ICurrentUserService currentUser, IProviderProfileRepository providers, IProviderClientRepository clients,
        IAppSessionRepository sessions, IMoodCheckInRepository checkIns, IAssessmentResultRepository assessments,
        IObservationRepository observations, IRtmTimeEntryRepository timeEntries, CancellationToken ct)
    {
        var scope = await ClientScope.LoadAsync(ClientId, currentUser, providers, clients, ct);
        var c = scope.Client;
        var from = scope.DaysAgoUtc(CaseloadSnapshot.LookbackDays);
        var month = RtmPeriods.CalendarMonthOf(scope.Today);
        var s = await sessions.GetForClientsAsync(scope.UserIds, from, scope.NowUtc, ct);
        var entries = await timeEntries.GetForClientAsync(c.Id, EasternTime.StartOfDayUtc(month.Start), scope.NowUtc, ct);
        Pseudonymizer.Register("[CLIENT]", c.FirstName, c.LastName);
        var data = new ClientContextData
        {
            Client = c, Token = "[CLIENT]", Sessions = s,
            CheckIns = await checkIns.GetForClientsAsync(scope.UserIds, from, scope.NowUtc, ct),
            Assessments = await assessments.GetForClientsAsync(scope.UserIds, from, scope.NowUtc, ct),
            Observations = await observations.GetForClientAsync(c.ClientUserId, scope.DaysAgoUtc(30), scope.NowUtc, ct),
            Rtm = RtmClientEvaluator.Evaluate(c, s, entries, scope.Today, EasternTime.ToDate)
        };

        Draft = RtmNoteDrafter.Draft(data, scope.NowUtc, EasternTime.ToDate);
        PolishPrompt = new AssistantPrompt
        {
            SystemPrompt = AssistantPrompts.System,
            Context = ClinicalContextBuilder.ForClient(data, scope.NowUtc, EasternTime.ToDate, t => TimeZoneInfo.ConvertTime(t, Eastern).Hour),
            Question = RtmNoteDrafter.AiInstruction + Draft
        };
    }
}
