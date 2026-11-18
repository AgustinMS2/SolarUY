using System.Text;
using SolarUY.ApiService.Dtos;

namespace SolarUY.ApiService.Services;

public class SolarUyKnowledgeAssistant
{
    private readonly ISolarUyDomainContextRetriever _retriever;

    public SolarUyKnowledgeAssistant(ISolarUyDomainContextRetriever retriever)
    {
        _retriever = retriever;
    }

    public ChatQueryResponse ProcessQuery(string message, IReadOnlyList<ChatMessageDto>? history = null)
    {
        var normalized = message.Trim().ToLowerInvariant();

        // 1. RF-8: Bloqueo de credenciales y seguridad
        if (ContainsAny(normalized, "password", "contraseña", "token", "keycloak", "secreto", "secret", "hash", "credencial"))
        {
            return new ChatQueryResponse(
                "Por motivos de seguridad, no estoy autorizado a divulgar contraseñas, credenciales, secretos ni tokens del sistema.",
                "SolarUY Security Policy");
        }

        // 2. Saludos de cortesía
        if (normalized is "hola" or "buenos dias" or "buenas tardes" or "buenas noches" or "buenas" || normalized.StartsWith("hola "))
        {
            return new ChatQueryResponse(
                "¡Hola! Soy el Asistente Inteligente de SolarUY. Puedo ayudarte con consultas sobre generación energética, estado de parques solares, alertas operativas y órdenes de mantenimiento agendadas.",
                "SolarUY Assistant");
        }

        // 3. RF-4: Validación de ámbito / fuera de dominio
        var domainTerms = new[]
        {
            "parque", "solar", "generaci", "energ", "kwh", "mwh", "potencia", "kw", "panel", "nodo",
            "inversor", "mantenimiento", "falla", "alerta", "salud", "rendimiento", "orden", "tecnico", "estado"
        };

        if (!ContainsAny(normalized, domainTerms))
        {
            return new ChatQueryResponse(
                "Solo puedo responder consultas relacionadas con la operación, generación, equipos y mantenimientos de los parques solares de SolarUY.",
                "SolarUY Guardrails");
        }

        var snapshot = _retriever.GetCurrentSnapshot();

        // Caso límite Spec 035 / Spec 039 RF-20: si menciona un parque específico que no existe en el inventario.
        var missingParkResponse = CheckNonExistentPark(message, snapshot.Parks);
        if (missingParkResponse != null)
        {
            return missingParkResponse;
        }

        // 4. Consultas de Alertas / Fallas (Spec 031)
        if (ContainsAny(normalized, "alerta", "falla", "anomalia", "problema", "severidad"))
        {
            if (snapshot.Alerts.PendingCount == 0 && snapshot.Alerts.AcknowledgedCount == 0)
            {
                return new ChatQueryResponse(
                    "Actualmente no se registran alertas activas de falla en los parques solares de SolarUY. Todo el equipamiento se encuentra operando dentro de los parámetros esperados.",
                    "SolarUY Telemetry");
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Actualmente se registran **{snapshot.Alerts.PendingCount} alertas pendientes** y {snapshot.Alerts.AcknowledgedCount} reconocidas:");
            sb.AppendLine();
            if (snapshot.Alerts.RecentAlertMessages.Count > 0)
            {
                foreach (var msg in snapshot.Alerts.RecentAlertMessages)
                {
                    sb.AppendLine($"- {msg}");
                }
            }
            else
            {
                sb.AppendLine("No hay alertas críticas recientes sin atender.");
            }

            return new ChatQueryResponse(sb.ToString().TrimEnd(), "SolarUY Alerts");
        }

        // 5. Consultas de Mantenimientos (Spec 024)
        if (ContainsAny(normalized, "mantenimiento", "visita", "reparacion", "orden"))
        {
            if (snapshot.Maintenances.PendingCount == 0)
            {
                return new ChatQueryResponse(
                    "No se encuentran órdenes de mantenimiento pendientes agendadas en este momento.",
                    "SolarUY Maintenance");
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Se encuentran **{snapshot.Maintenances.PendingCount} órdenes de mantenimiento pendientes**:");
            sb.AppendLine();
            foreach (var ord in snapshot.Maintenances.UpcomingOrders)
            {
                sb.AppendLine($"- {ord}");
            }

            return new ChatQueryResponse(sb.ToString().TrimEnd(), "SolarUY Maintenance");
        }

        // 6. Consultas de Generación y Energía (Spec 034)
        if (ContainsAny(normalized, "generaci", "energ", "kwh", "mwh", "producc", "hoy", "mes"))
        {
            if (snapshot.Generations.Count == 0)
            {
                return new ChatQueryResponse(
                    "No hay datos de generación registrados en el sistema para los parques solares.",
                    "SolarUY History");
            }

            var sb = new StringBuilder();
            sb.AppendLine("A continuación se detalla la generación acumulada registrada:");
            sb.AppendLine();
            sb.AppendLine("| Parque Solar | Generación Hoy (kWh) | Generación Mes (kWh) |");
            sb.AppendLine("| :--- | :--- | :--- |");
            foreach (var gen in snapshot.Generations)
            {
                sb.AppendLine($"| {gen.ParkName} | {gen.TodayKwh:N1} | {gen.MonthKwh:N1} |");
            }

            return new ChatQueryResponse(sb.ToString().TrimEnd(), "SolarUY History");
        }

        // 7. Consultas de Parques Solares y Equipos (Specs 001-014)
        if (ContainsAny(normalized, "parque", "nodo", "panel", "inversor", "equipo", "capacidad", "infraestructura"))
        {
            if (snapshot.Parks.Count == 0)
            {
                return new ChatQueryResponse(
                    "No hay parques solares registrados en la plataforma SolarUY.",
                    "SolarUY Infrastructure");
            }

            var sb = new StringBuilder();
            sb.AppendLine("SolarUY cuenta con los siguientes parques solares registrados:");
            sb.AppendLine();
            sb.AppendLine("| Parque Solar | Capacidad Nominal (kW) | Estado |");
            sb.AppendLine("| :--- | :--- | :--- |");
            foreach (var park in snapshot.Parks)
            {
                sb.AppendLine($"| {park.Name} | {park.CapacityKw:N1} | {park.Status} |");
            }

            return new ChatQueryResponse(sb.ToString().TrimEnd(), "SolarUY Infrastructure");
        }

        // 8. RF-3: Pedido de aclaración por ambigüedad
        return new ChatQueryResponse(
            "Por favor indicame a qué parque solar, generación, alerta o mantenimiento te referís para poder darte el dato exacto.",
            "SolarUY Assistant");
    }

    private static bool ContainsAny(string text, params string[] terms)
    {
        return terms.Any(t => text.Contains(t, StringComparison.OrdinalIgnoreCase));
    }

    private static ChatQueryResponse? CheckNonExistentPark(string message, IReadOnlyList<DomainParkInfo> parks)
    {
        if (parks.Count == 0)
        {
            return null;
        }

        // Si el mensaje ya menciona el nombre de algún parque existente, es válido.
        foreach (var p in parks)
        {
            var shortName = p.Name.Replace("Parque Solar ", "", StringComparison.OrdinalIgnoreCase).Trim();
            if (!string.IsNullOrEmpty(shortName) && message.Contains(shortName, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
        }

        var idx = message.IndexOf("parque ", StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
        {
            return null;
        }

        var after = message[(idx + "parque ".Length)..].Trim();
        if (after.StartsWith("solar ", StringComparison.OrdinalIgnoreCase))
        {
            after = after["solar ".Length..].Trim();
        }

        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "solar", "solares", "de", "del", "en", "con", "sin", "tiene", "tienen", "más", "mas",
            "mayor", "menor", "activo", "activos", "inactivo", "inactivos", "registrado", "registrados",
            "hoy", "ayer", "este", "mes", "año", "anio", "que", "cual", "cuál", "como", "cómo"
        };

        var firstToken = after.Split([' ', '?', '¿', ',', '.', ';', ':'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (string.IsNullOrWhiteSpace(firstToken) || stopWords.Contains(firstToken))
        {
            return null;
        }

        var availableNames = string.Join(", ", parks.Select(p => $"**{p.Name}**"));
        return new ChatQueryResponse(
            $"El parque mencionado (\"{firstToken}\") no existe en el inventario de SolarUY. Los parques registrados disponibles son: {availableNames}.",
            "SolarUY Guardrails");
    }
}

