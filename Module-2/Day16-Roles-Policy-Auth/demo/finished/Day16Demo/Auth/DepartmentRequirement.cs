using Microsoft.AspNetCore.Authorization;

namespace Day16Demo.Auth;

// A Requirement is the QUESTION, not the answer: a data-only class carrying what to check
// (which department). Implementing the IAuthorizationRequirement marker is what lets the
// policy system recognize it and match it to a handler. No logic lives here — that's the
// handler's job. One requirement type can back many policies with different data
// ("Inventory", "Sales", …), each constructed with its own value.
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
