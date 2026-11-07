using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.Interfaces;
using Reflect.Domain.Interfaces.Repositories;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Shared;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.Notes;

/// <summary>Client page > Notes tab, and the "Your notes" section of session prep.</summary>
public class ClientNotesViewModel
{
    public Guid ClientId { get; set; }
    public string ClientFirstName { get; set; } = string.Empty;
    public List<NoteRowViewModel> Notes { get; set; } = new();
    /// <summary>Topics that have notes, with counts, for the filter chips.</summary>
    public List<(NoteTopic Topic, string Label, int Count)> TopicCounts { get; set; } = new();

    public async Task PrepareData(Guid clientId, ICurrentUserService currentUser, IProviderProfileRepository providers,
        IProviderClientRepository clients, IClientNoteRepository notes, CancellationToken ct)
    {
        var scope = await ClientScope.LoadAsync(clientId, currentUser, providers, clients, ct); // ownership check
        ClientId = scope.Client.Id;
        ClientFirstName = scope.Client.FirstName;
        var me = currentUser.UserId ?? Guid.Empty;
        var list = await notes.GetForClientAsync(scope.Client.Id, ct);
        Notes = list.Select(n => NoteRowViewModel.From(n, me)).ToList();
        TopicCounts = list.GroupBy(n => n.Topic).OrderBy(g => g.Key).Select(g => (g.Key, NoteTopics.Label(g.Key), g.Count())).ToList();
    }

    /// <summary>Note counts per attached item (session, assessment, artwork), for the note buttons on other tabs.</summary>
    public static async Task<Dictionary<Guid, int>> CountsBySubjectAsync(Guid providerClientId, IClientNoteRepository notes, CancellationToken ct) =>
        (await notes.GetForClientAsync(providerClientId, ct)).Where(n => n.SubjectId.HasValue)
            .GroupBy(n => n.SubjectId!.Value).ToDictionary(g => g.Key, g => g.Count());
}
