# Plan Técnico: Spec 010 — Listar Nodos

## Proyectos y Archivos Afectados
- `SolarUY.ApiService`:
  - `Repositories/ISolarNodeRepository.cs`: método `IReadOnlyList<SolarNode> GetAll(Guid? parkId = null)`.
  - `Services/ISolarNodeService.cs` y `SolarNodeService.cs`: método `IReadOnlyList<SolarNodeDto> GetAll(Guid? parkId = null)`.
  - `Dtos/SolarNodeDto.cs`: record `(Guid Id, string Name, Guid SolarParkId, string SolarParkName, int PanelsCount, string? InverterSerie, string Status)`.
  - `Controllers/SolarNodesController.cs`: endpoint `GET /api/solar-nodes?parkId=...`.
- `SolarUY.Web`:
  - `Services/SolarNodeApiClient.cs`: método `GetAllAsync(Guid? parkId = null)`.
  - `Components/Pages/SolarNodeList.razor` (ruta `/nodos`): grilla de nodos con filtro de parque y badges de estado.
- `SolarUY.Tests`:
  - `Services/SolarNodeServiceGetAllTests.cs`: tests unitarios de ordenamiento y filtrado del servicio.

## Modelo de Datos y Persistencia
- Consulta con `.Include(n => n.SolarPark)` para mapear el nombre del parque.
- Conteo de paneles relacionados mediante subconsulta eficiente.

## Servicios y Lógica de Dominio Pura
- `GetAll(Guid? parkId = null)`:
  - Recupera los nodos del repositorio.
  - Ordena alfabéticamente: primero por nombre de parque, luego por nombre de nodo.
  - Mapea estado a "Activo" o "Inactivo".

## API y Comunicación
- `GET /api/solar-nodes?parkId={optional}`
  - Respuestas: `200 OK` con `IReadOnlyList<SolarNodeDto>`.

## Interfaz de Usuario (Blazor Server)
- `/nodos`:
  - Selector superior para filtrar por parque solar (opción "Todos los parques").
  - Tabla con columnas: Nombre, Parque solar, Inversor, Paneles asociados, Estado.
  - Botón "Nuevo nodo" que lleva a `/nodos/nuevo`.
  - Mensaje "No hay nodos registrados." si la lista está vacía.

## Estrategia de Tests
- **Tests unitarios**:
  - Lista vacía cuando no hay registros.
  - Filtrado correcto por `parkId`.
  - Ordenamiento estricto por parque y luego nodo.
- **Tests de arquitectura**: se mantienen en verde.
