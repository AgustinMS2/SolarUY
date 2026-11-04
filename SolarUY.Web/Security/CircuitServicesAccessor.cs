using Microsoft.AspNetCore.Components.Server.Circuits;

namespace SolarUY.Web.Security;

/// <summary>
/// Exposes the current Blazor circuit's services to HttpClient handlers, which are resolved
/// in a different DI scope. Pattern documented by Microsoft to reach the user from a DelegatingHandler.
/// </summary>
public class CircuitServicesAccessor
{
    private static readonly AsyncLocal<IServiceProvider?> BlazorServices = new();

    public IServiceProvider? Services
    {
        get => BlazorServices.Value;
        set => BlazorServices.Value = value;
    }
}

public class ServicesAccessorCircuitHandler(
    IServiceProvider services,
    CircuitServicesAccessor servicesAccessor) : CircuitHandler
{
    public override Func<CircuitInboundActivityContext, Task> CreateInboundActivityHandler(
        Func<CircuitInboundActivityContext, Task> next)
    {
        return async context =>
        {
            servicesAccessor.Services = services;
            await next(context);
            servicesAccessor.Services = null;
        };
    }
}
