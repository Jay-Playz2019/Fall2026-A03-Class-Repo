using Microsoft.AspNetCore.Authorization;

namespace Day16Demo.Auth;

// The Handler is the ANSWER: it reads the user's claims and decides. The pairing is by the
// generic type — AuthorizationHandler<DepartmentRequirement> runs whenever that requirement
// is evaluated, and the framework hands the matched requirement in as a parameter.
public class DepartmentHandler : AuthorizationHandler<DepartmentRequirement>
{
    // The framework calls this each time a policy needs the requirement
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DepartmentRequirement requirement)
    {
        // Read the department claim the provider sent
        var departmentClaim = context.User.FindFirst("department");

        // Call Succeed ONLY when the rule passes — and never call context.Fail(). Fail is a
        // hard veto that overrides every other handler, even ones that would approve.
        // Staying silent (not succeeding) simply declines: the framework denies access
        // unless some handler succeeds, so "do nothing" is already the safe default.
        if (departmentClaim?.Value == requirement.Department)
        {
            // Mark the requirement as met
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
