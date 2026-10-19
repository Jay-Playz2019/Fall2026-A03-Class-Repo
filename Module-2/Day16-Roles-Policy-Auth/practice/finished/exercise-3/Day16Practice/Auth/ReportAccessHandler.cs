using Microsoft.AspNetCore.Authorization;

namespace Day16Practice.Auth;

/// <summary>
/// Stretch exercise: Handler that grants report access if the user
/// has the Admin role OR belongs to the Analytics department.
/// Demonstrates OR logic in a single handler (since multiple requirements
/// within a policy are AND-ed by default).
/// </summary>
public class ReportAccessHandler : AuthorizationHandler<ReportAccessRequirement>
{
    // The framework calls this each time a policy needs the requirement
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ReportAccessRequirement requirement)
    {
        // Check 1: Is the user an Admin?
        if (context.User.IsInRole("Admin"))
        {
            // Mark the requirement as met
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Check 2: Is the user in the Analytics department?
        var departmentClaim = context.User.FindFirst("department");
        if (departmentClaim?.Value == "Analytics")
        {
            // Mark the requirement as met
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
