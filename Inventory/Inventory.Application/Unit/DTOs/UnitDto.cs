namespace Inventory.Application.Units.DTOs
{
    public record UnitDto(Guid Id, string Name, string Description, bool IsActive);
    public record CreateUnitDto(string Name, string Description);
}
