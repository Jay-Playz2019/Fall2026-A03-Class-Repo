using Microsoft.AspNetCore.Authorization;

namespace Day16Practice.Auth;

/// <summary>
/// Stretch exercise: Requirement for report access.
/// Succeeds if the user has the Admin role OR belongs to the Analytics department.
/// </summary>
public class ReportAccessRequirement : IAuthorizationRequirement
{
}
