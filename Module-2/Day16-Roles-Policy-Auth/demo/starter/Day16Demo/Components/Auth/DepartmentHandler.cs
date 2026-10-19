using Microsoft.AspNetCore.Authorization;

namespace Day16Demo.Components.Auth
{
    public class DepartmentHandler : AuthorizationHandler<DepartmentRequirement>
    {
        // The framework calls this each time a policy needs the requirement
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            DepartmentRequirement requirement)
        {
            var deptClaim = context.User.FindFirst("department");
            if (deptClaim?.Value == requirement.Department)
            {
                // Mark the requirement as met
                context.Succeed(requirement);
            }
            return Task.CompletedTask;
        }
    }
}
