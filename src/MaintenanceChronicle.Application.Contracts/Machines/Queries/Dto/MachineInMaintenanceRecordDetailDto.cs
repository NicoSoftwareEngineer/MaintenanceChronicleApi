using MaintenanceChronicle.Data.Entities.Business;
using NodaTime.Text;

namespace MaintenanceChronicle.Application.Contracts.Machines.Queries.Dto;

public class MachineInMaintenanceRecordDetailDto
{
    public Guid Id { get; set; }
    public required string Model { get; set; }
    public required string Manufacture { get; set; }
    public required string SerialNumber { get; set; }
    public required string LocationName { get; set; }
    public required string Color { get; set; }
    public required string InUseSince { get; set; }
}
/// <summary>
/// Extension methods for <see cref="Machine"/> entity.
/// </summary>
public static class MachineInMaintenanceRecordDetailExtension
{
    /// <summary>
    /// Converts <see cref="Machine"/> to <see cref="MachineInMaintenanceRecordDetailDto"/>.
    /// </summary>
    /// <param name="entity"><see cref="Machine"/> to convert</param>
    /// <returns>Converted entity to  <see cref="MachineInMaintenanceRecordDetailDto"/></returns>
    public static MachineInMaintenanceRecordDetailDto ToMachineDto(this Machine entity) =>
        new MachineInMaintenanceRecordDetailDto
        {
            Id = entity.Id,
            Model = entity.Model,
            Manufacture = entity.Manufacture,
            Color = entity.Color,
            InUseSince =  InstantPattern.CreateWithInvariantCulture("dd.MM.yyyy").Format(entity.InUseSince),
            SerialNumber = entity.SerialNumber,
            LocationName = entity.Location.Name,
        };
}
