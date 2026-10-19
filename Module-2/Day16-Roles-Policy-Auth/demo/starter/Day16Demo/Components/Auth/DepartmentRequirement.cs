using Microsoft.AspNetCore.Authorization;

namespace Day16Demo.Components.Auth
{
    public class DepartmentRequirement : IAuthorizationRequirement
    {
        public string Department { get; }
        public DepartmentRequirement(string department) => Department = department;
    }
}
