namespace MatinPower.Server.Models.Body;

public class SetUserRoleRequest
{
    public int UserId { get; set; }
    public int? RoleId { get; set; }
}

public class CreateAdminRequest
{
    public string? FullName { get; set; }
    public string Mobile { get; set; } = "";
    public string Password { get; set; } = "";
    public int? RoleId { get; set; }
}

public class UpdateUserRequest
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Mobile { get; set; } = "";
    public string? Password { get; set; }
}

public class RoleFormRequest
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
}

public class SetPermissionsRequest
{
    public int RoleId { get; set; }
    public List<int> SiteMapIds { get; set; } = new();
}
