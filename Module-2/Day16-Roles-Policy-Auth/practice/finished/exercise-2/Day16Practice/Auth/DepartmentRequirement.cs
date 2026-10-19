using Microsoft.AspNetCore.Authorization;

namespace Day16Practice.Auth;

// Requirement = data only (what to check). No logic — the marker interface just lets the
// policy system recognize it and route it to the matching handler.
public class DepartmentRequirement : IAuthorizationRequirement
{
    // The department this requirement asks for
    public string Department { get; }

    // Each policy passes in its own department
    public DepartmentRequirement(string department)
    {
        Department = department;
    }
}
