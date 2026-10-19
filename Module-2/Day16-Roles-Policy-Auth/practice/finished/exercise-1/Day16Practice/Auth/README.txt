Create DepartmentRequirement.cs and DepartmentHandler.cs here for Practice 2.

Practice 2 Steps:
1. Create DepartmentRequirement.cs - implement IAuthorizationRequirement with a Department property
2. Create DepartmentHandler.cs - implement AuthorizationHandler<DepartmentRequirement>
3. Register the policy and handler in Program.cs
4. Apply [Authorize(Policy = "CanManageInventory")] to Products.razor
