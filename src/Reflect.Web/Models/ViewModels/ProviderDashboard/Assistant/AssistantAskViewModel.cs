using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.Interfaces;
using Reflect.Domain.Interfaces.Repositories;
using Reflect.Domain.ProviderDashboard;
using Reflect.Domain.ProviderDashboard.Assistant;
using Reflect.Domain.ProviderDashboard.Rtm;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Shared;
using Reflect.Web.Services.Assistant;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.Assistant;

/// <summary>
/// One question to the clinical assistant, about one client (ClientId set) or the whole caseload.
/// Builds the de-identified prompt; the controller sends it and restores names in the answer.
/// </summary>
public class AssistantAskViewModel
{
    public Guid? ClientId { get; set; }
    public string Question { get; set; } = string.Empty;

    public AssistantPrompt Prompt { get; private set; }
    public Pseudonymizer Pseudonymizer { get; } = new();
    public string Error { get; private set; } = string.Empty;

    private static readonly TimeZoneInfo Eastern = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
    private static int LocalHour(DateTimeOffset t) => TimeZoneInfo.ConvertTime(t, Eastern).Hour;

    /// <summary>Returns false (and sets <see cref="Error"/>) when the request is invalid.</summary>
    public async Task<bool> PrepareData(ICurrentUserService currentUser, IProviderProfileRepository providers, IProviderClientRepository clients,
        IAppSessionRepository sessions, IMoodCheckInRepository checkIns, IAssessmentResultRepository assessments,
        IObservationRepository observations, IRtmTimeEntryRepository timeEntries, IReadOnlyList<AssistantChatMessage> priorMessages, CancellationToken ct)
    {
        var question = (Question ?? string.Empty).Trim();
        if (question.Length == 0) { Error = "Type a question."; return false; }
        if (question.Length > AssistantPrompts.MaxQuestionLength) { Error = $"Keep questions under {AssistantPrompts.MaxQuestionLength} characters."; return false; }

        string context;
        if (ClientId.HasValue)
        {
            var scope = await ClientScope.LoadAsync(ClientId.Value, currentUser, providers, clients, ct);
            var c = scope.Client;
            var from = scope.DaysAgoUtc(CaseloadSnapshot.LookbackDays);
            var month = RtmPeriods.CalendarMonthOf(scope.Today);
            var s = await sessions.GetForClientsAsync(scope.UserIds, from, scope.NowUtc, ct);
            var entries = await timeEntries.GetForClientAsync(c.Id, EasternTime.StartOfDayUtc(month.Start), scope.NowUtc, ct);
            var token = Pseudonymizer.Register("[CLIENT]", c.FirstName, c.LastName);
            context = ClinicalContextBuilder.ForClient(new ClientContextData
            {
                Client = c, Token = token, Sessions = s,
                CheckIns = await checkIns.GetForClientsAsync(scope.UserIds, from, scope.NowUtc, ct),
                Assessments = await assessments.GetForClientsAsync(scope.UserIds, from, scope.NowUtc, ct),
                Observations = await observations.GetForClientAsync(c.ClientUserId, scope.DaysAgoUtc(30), scope.NowUtc, ct),
                Rtm = RtmClientEvaluator.Evaluate(c, s, entries, scope.Today, EasternTime.ToDate)
            }, scope.NowUtc, EasternTime.ToDate, LocalHour);
        }
        else
        {
            var snap = await CaseloadSnapshot.LoadAsync(currentUser, providers, clients, sessions, checkIns, assessments, timeEntries, null, ct);
            var list = new List<ClientContextData>();
            var n = 0;
            foreach (var c in snap.Clients.Where(c => c.Status != ClientRtmStatus.Discharged))
            {
                var token = Pseudonymizer.Register($"[C{++n:00}]", c.FirstName, c.LastName);
                list.Add(new ClientContextData
                {
                    Client = c, Token = token,
                    Sessions = snap.SessionsFor(c).ToList(), CheckIns = snap.CheckInsFor(c).ToList(),
                    Assessments = snap.AssessmentsFor(c).ToList(), Rtm = snap.EvaluateRtm(c)
                });
            }
            context = ClinicalContextBuilder.ForCaseload(list, snap.NowUtc, EasternTime.ToDate, LocalHour);
        }

        Prompt = new AssistantPrompt
        {
            SystemPrompt = AssistantPrompts.System,
            Context = context,
            History = HistoryFrom(priorMessages).Select(t => new AssistantTurn(t.Role, Pseudonymizer.Apply(t.Content))).ToList(),
            Question = Pseudonymizer.Apply(question)
        };
        return true;
    }

    /// <summary>Last few complete exchanges from the stored thread: a question plus a real AI answer (offline summaries and errors are skipped).</summary>
    private static List<AssistantTurn> HistoryFrom(IReadOnlyList<AssistantChatMessage> messages)
    {
        var turns = new List<AssistantTurn>();
        var list = messages ?? Array.Empty<AssistantChatMessage>();
        for (var k = 0; k + 1 < list.Count; k++)
        {
            var q = list[k];
            var a = list[k + 1];
            if (q.Role != "user" || a.Role != "assistant" || a.Offline || a.IsError || q.Text.Length == 0 || a.Text.Length == 0) continue;
            turns.Add(new AssistantTurn("user", Trim(q.Text)));
            turns.Add(new AssistantTurn("assistant", Trim(a.Text)));
            k++;
        }
        return turns.TakeLast(AssistantPrompts.MaxHistoryTurns).ToList();
    }

    private static string Trim(string t) => t.Length > AssistantPrompts.MaxTurnLength ? t[..AssistantPrompts.MaxTurnLength] : t;
}
