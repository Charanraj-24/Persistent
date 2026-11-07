using System.Globalization;
using Reflect.Domain.ProviderDashboard.Rtm;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.Shared;

/// <summary>A status chip. Color is always paired with text and an icon (accessibility rule).</summary>
public sealed record BadgeViewModel(string Color, string Text, string Icon);
