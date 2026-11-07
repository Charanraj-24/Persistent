using System.ComponentModel.DataAnnotations;

namespace Reflect.Web.Models.Validation;

/// <summary>
/// Requires a checkbox to be checked. Server-side only: jQuery unobtrusive's Range rule compares the
/// checkbox value ("true") to "True" and blocks valid submissions, so it must not be used for booleans.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class MustBeTrueAttribute : ValidationAttribute
{
    public override bool IsValid(object value) => value is true;
}
