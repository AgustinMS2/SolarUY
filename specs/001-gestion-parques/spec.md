# Spec 001 — Gestión de Parques Solares

Estado: borrador

## Contexto y objetivo
Permitir a los Administradores de SolarUY dar de alta, modificar, desactivar (baja lógica) y listar los parques solares fotovoltaicos gestionados por la plataforma. Cada parque solar agrupa nodos y paneles, y posee configuraciones operativas independientes necesarias para el cálculo de telemetría y monitoreo energético.

## Usuarios / Actores
- **Administrador**: Puede crear, modificar y dar de baja parques solares, así como gestionar sus configuraciones operativas.
- **Técnico (Personal Especializado)**: Puede consultar la lista y el detalle de parques solares para planificar mantenimientos.

## Historias de usuario
- **HU-1**: Como Administrador, quiero dar de alta un nuevo parque solar con su nombre, ubicación geográfica y configuraciones operativas para incorporarlo al sistema de monitoreo.
- **HU-2**: Como Administrador, quiero modificar los datos y parámetros operativos de un parque existente para mantener la información actualizada.
- **HU-3**: Como Administrador, quiero dar de baja lógica un parque solar para suspender su monitoreo sin perder su historial de generación energética.
- **HU-4**: Como Usuario (Administrador o Técnico), quiero visualizar el listado de todos los parques solares con sus métricas clave y estado operativo.

## Definiciones
- **Parque Solar**: Unidad macro de generación fotovoltaica que contiene uno o más Nodos y dispone de una conexión central a la red eléctrica.
- **Configuración Independiente**: Conjunto de parámetros específicos del parque (capacidad instalada máxima en kW, umbrales de alerta de potencia, geolocalización latitud/longitud y huso horario).
- **Baja Lógica**: Desactivación del parque que impide la ingesta de nuevas órdenes operativas o mantenimientos futuros, pero preserva intactos los registros históricos de telemetría.

## Requisitos funcionales (Criterios de aceptación en EARS)
- **RF-1 (Alta exitosa)**: CUANDO el Administrador envíe el formulario de alta con nombre único, ubicación válida y capacidad nominal mayor a cero, EL SISTEMA creará el parque solar con estado 'Activo' y devolverá su identificador único.
- **RF-2 (Validación de duplicados)**: SI el Administrador intenta registrar un parque solar con un nombre que ya existe (comparación insensible a mayúsculas/minúsculas), ENTONCES EL SISTEMA rechazará la creación e informará que el nombre ya se encuentra en uso.
- **RF-3 (Listado ordenado)**: CUANDO cualquier usuario autenticado solicite la lista de parques, EL SISTEMA retornará todos los parques ordenados alfabéticamente por nombre, indicando su estado (Activo/Inactivo), cantidad de nodos asociados y potencia total instalada.
- **RF-4 (Modificación de configuración)**: CUANDO el Administrador guarde cambios en el nombre o configuraciones independientes de un parque existente, EL SISTEMA actualizará los datos y registrará la marca temporal de última modificación en UTC.
- **RF-5 (Baja lógica y restricción con nodos activos)**: SI el Administrador solicita la baja de un parque que posee nodos con inversores en inyección activa a la red, ENTONCES EL SISTEMA denegará la baja hasta que se desactiven los inversores correspondientes.
- **RF-6 (Baja lógica permitida)**: CUANDO el Administrador confirme la baja de un parque elegible, EL SISTEMA cambiará su estado a 'Inactivo' y conservará todos los registros históricos y paneles vinculados.

## Requisitos no funcionales
- **Seguridad**: Solo usuarios con el rol `Administrador` verificado por token OpenID (Keycloak) pueden invocar altas, modificaciones o bajas.
- **Rendimiento**: El listado de parques debe responder en menos de 200 ms para un volumen de hasta 100 parques.
- **Interfaz**: Visualización en Blazor Server en idioma español, con diseño responsivo y accesible.

## Casos límite
- Nombre de parque con espacios en blanco al inicio o al final (deben ser recortados / trimmed).
- Parque sin nodos registrados: debe mostrar cantidad de nodos = 0 y potencia = 0 kW sin arrojar excepciones.
- Intentos de ingresar coordenadas de latitud/longitud fuera de los rangos válidos (-90 a 90 / -180 a 180).
- Concurrencia: intento simultáneo de modificación de un mismo parque por dos administradores (debe detectarse control de concurrencia optimista).

## Fuera de alcance
- Asignación de nodos y paneles (se gestionará en la spec `002-gestion-nodos` y `003-gestion-paneles`).
- Telemetría en tiempo real por SignalR (se especificará en `006-monitoreo-tiempo-real`).

## Criterios de finalización
- Cobertura completa de RF-1 a RF-6 con pruebas automatizadas en verde (`dotnet test`).
- Pantalla de listado y formulario de parque en Blazor Server navegable y validada en escritorio y vista móvil.

## Dudas abiertas
- [NECESITA ACLARACIÓN] ¿Se permitirá a un usuario con rol Técnico ver parques dados de baja (inactivos) o solo los parques activos?
- [NECESITA ACLARACIÓN] ¿Cuál es el formato exacto requerido para almacenar las 'configuraciones independientes' (columnas dedicadas en base de datos o documento JSON tipado)?
