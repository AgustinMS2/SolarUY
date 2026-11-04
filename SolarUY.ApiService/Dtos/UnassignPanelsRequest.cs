namespace SolarUY.ApiService.Dtos;

public record UnassignPanelsRequest(
    IReadOnlyList<Guid>? PanelIds
);
