namespace SolarUY.ApiService.Dtos;

public record AssignPanelsRequest(
    IReadOnlyList<Guid>? PanelIds,
    // Confirmación opcional: si el cliente no la envía vale false (no confirmó el cambio de nodo).
#pragma warning disable S6964 // Value type property used as input should be required
    bool Confirm
#pragma warning restore S6964
);
