using System.ComponentModel.DataAnnotations;

using Identity.Domain.Common;

namespace Identity.Domain.Menus;

public class Menu : AuditableEntity, IMultiTenant
{
    [Key]
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public Guid? ParentId { get; set; }
    public int Order { get; set; }

    public Guid? CompanyId { get; set; }
    public string? BranchId { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public Menu? Parent { get; private set; }
    public ICollection<Menu> Children { get; private set; } = new List<Menu>();

    public Menu() { }

    public Menu(string title, string url, string? icon, Guid? parentId, int order)
    {
        Id = Guid.NewGuid();
        Title = title;
        Url = url;
        Icon = icon;
        ParentId = parentId;
        Order = order;
    }
    public void Update(string title, string url, string? icon, Guid? parentId, int order)
    {
        Title = title;
        Url = url;
        Icon = icon;
        ParentId = parentId;
        Order = order;
    }
}