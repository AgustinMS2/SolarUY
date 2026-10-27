# Spec 018 — Alta de Técnico

Estado: borrador
Depende de: Spec 015 (Iniciar Sesión), Spec 017 (Roles)

## Contexto y objetivo
Registrar los datos del personal técnico especializado en SolarUY para posibilitar su posterior asignación a órdenes de mantenimiento preventivo y correctivo en los parques solares.

## Usuarios / Actores
- **Administrador**: da de alta al personal técnico.

## Historias de usuario
- **HU-1**: Como Administrador, quiero registrar un técnico con su nombre, documento, correo y teléfono para poder asignarle órdenes de trabajo en los paneles y nodos.

## Datos del técnico

| Campo | Obligatorio | Regla (tipo, rango, largo, unicidad) |
| :--- | :--- | :--- |
| Nombre completo | Sí | Texto. Trim. 1 a 150 caracteres. |
| Documento de identidad | Sí | Texto. Formato válido de cédula o identificación (1 a 20 caracteres). Único en el sistema. |
| Correo electrónico | Sí | Texto. Formato de email válido (ej. usuario@dominio.com). Único en el sistema. |
| Teléfono de contacto | Sí | Texto. 6 a 20 caracteres numéricos y prefijos (+, -). |
| Especialidad / Nivel | No | Texto (ej. "Electricista de alta tensión", "Inversores"). Máx. 100 caracteres. |

## Requisitos funcionales (EARS)

- **RF-1**: CUANDO el Administrador envíe el formulario de alta con todos los campos válidos, EL SISTEMA registrará el técnico en estado "Activo", guardará la fecha de creación en UTC y mostrará "Técnico registrado correctamente."
- **RF-2**: SI algún campo no cumple su regla, ENTONCES EL SISTEMA no registrará el técnico y mostrará:
  - Nombre vacío → "El nombre completo es obligatorio."
  - Documento vacío → "El documento de identidad es obligatorio."
  - Documento duplicado → "Ya existe un técnico registrado con el documento \"<documento>\"."
  - Email inválido → "El formato del correo electrónico no es válido."
  - Email duplicado → "El correo electrónico \"<email>\" ya se encuentra registrado."
  - Teléfono vacío o inválido → "El teléfono de contacto es obligatorio y debe tener un formato válido."
- **RF-3**: CUANDO el alta sea exitosa, EL SISTEMA limpiará el formulario para permitir registrar otro técnico.
- **RF-4**: SI el técnico dado de alta posee una cuenta de usuario en Keycloak, EL SISTEMA vinculará automáticamente el registro de técnico con su identidad de login mediante su correo electrónico.

## Casos límite
- "  Juan Pérez  " se almacena como "Juan Pérez".
- Documentos con puntos o guiones se normalizan antes de validar unicidad.

## Fuera de alcance
- Modificación y baja de técnico (specs 019 y 020).
- Gestión de horarios de guardia.

## Criterios de finalización
- Reglas del servicio cubiertas con tests unitarios en verde.
- Tests de arquitectura en verde.
- Verificación manual en navegador.

## Dudas abiertas
- Ninguna.
