using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace JohnIsDev.Core.Extensions;

/// <summary>
/// Provides extension methods for working with enumerations.
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    /// Retrieves the display name of the specified enum value. If a <see cref="DisplayAttribute"/>
    /// is applied to the enum value, its name is returned; otherwise, the enum value's name is returned as a string.
    /// </summary>
    /// <param name="value">The enum value for which to retrieve the display name.</param>
    /// <returns>The display name of the enum value or its string representation if no display name is defined.</returns>
    public static string GetDisplayName(this Enum value)
    {
        MemberInfo? member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
        return member?.GetCustomAttribute<DisplayAttribute>()?.GetName()
               ?? value.ToString();
    }
}