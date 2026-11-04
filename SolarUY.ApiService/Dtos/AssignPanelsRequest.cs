namespace SolarUY.ApiService.Dtos;

public record AssignPanelsRequest(
    IReadOnlyList<Guid>? PanelIds,
    bool Confirm
);
