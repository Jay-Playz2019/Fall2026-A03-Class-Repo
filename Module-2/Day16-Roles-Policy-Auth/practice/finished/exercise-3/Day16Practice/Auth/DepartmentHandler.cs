using Microsoft.AspNetCore.Authorization;

namespace Day16Practice.Auth;

// The handler: the logic that checks DepartmentRequirement
public class DepartmentHandler : AuthorizationHandler<DepartmentRequirement>
{
    // The framework calls this each time a policy needs the requirement
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DepartmentRequirement requirement)
    {
        // Read the department claim the provider sent
        var departmentClaim = context.User.FindFirst("department");

        // Handler = the logic. Call Succeed only on a match; never call Fail (Fail is a hard
        // veto that overrides other handlers). Staying silent simply declines.
        if (departmentClaim?.Value == requirement.Department)
        {
            // Mark the requirement as met
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
