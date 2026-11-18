# Spec 038 — Provisión de Credenciales y Sincronización de Acceso de Técnicos

Estado: implementada
Depende de: Spec 015 (Iniciar Sesión), Spec 017 (Gestionar Roles de Usuario), Spec 018 (Alta de Técnico), Spec 020 (Baja de Técnico), Spec 025 (Ver Mis Mantenimientos)

## Contexto y objetivo
Solucionar la desconexión entre el alta operativa de técnicos y las cuentas de usuario del servicio de autenticación. Actualmente, al registrar un técnico solo se almacena su ficha interna sin crearle credenciales de acceso, impidiéndole iniciar sesión para consultar sus tareas asignadas. Con esta especificación, cuando el Administrador da de alta a un técnico (con nombre, documento, correo, teléfono, especialidad opcional y contraseña), el sistema crea simultáneamente su cuenta de acceso con rol "Técnico" y su perfil operativo, permitiéndole ingresar de inmediato al sistema y ver sus mantenimientos asignados. Asimismo, asegura la consistencia al dar de baja un técnico y en los datos iniciales del sistema.

## Usuarios / Actores
- **Administrador**: registra al técnico asignándole su contraseña de acceso y gestiona su baja.
- **Personal Especializado (Técnico)**: inicia sesión con su correo electrónico y contraseña para consultar y confirmar sus tareas de mantenimiento.

## Historias de usuario
- **HU-1**: Como Administrador, quiero registrar un técnico ingresando su nombre, documento, correo, teléfono, especialidad opcional y una contraseña de acceso para que pueda entrar directamente al sistema sin gestiones manuales externas.
- **HU-2**: Como Técnico, quiero iniciar sesión con mi correo electrónico y la contraseña asignada por el Administrador para ver y gestionar mis tareas de mantenimiento asignadas.
- **HU-3**: Como Administrador, quiero que al dar de baja a un técnico se deshabilite su cuenta de acceso para impedir que un técnico inactivo inicie sesión en el sistema.

## Definiciones
- **Cuenta de acceso del técnico**: identidad de usuario en el servicio de autenticación vinculada al técnico mediante su correo electrónico, con el rol "Técnico" asignado y contraseña permanente (no requiere cambio obligatorio en el primer inicio de sesión).

## Datos

| Campo | Obligatorio | Regla (tipo, rango, largo, unicidad) |
| :--- | :--- | :--- |
| Nombre completo | Sí | Texto. Trim. 1 a 150 caracteres. |
| Documento de identidad | Sí | Texto. Trim. 1 a 20 caracteres. Normalizado sin puntos ni guiones. Único en el sistema. |
| Correo electrónico | Sí | Texto. Trim. Formato de email válido (`usuario@dominio.com`). Único entre técnicos y cuentas de usuario. |
| Teléfono de contacto | Sí | Texto. Trim. Entre 6 y 20 dígitos numéricos (permite espacios y prefijos `+`, `-`). |
| Especialidad | No | Texto. Trim. Máx. 100 caracteres. |
| Contraseña | Sí | Texto. Entre 6 y 100 caracteres. |

## Requisitos funcionales (Criterios de aceptación en EARS)

- **RF-1**: CUANDO el Administrador envíe el formulario de alta de técnico con nombre completo, documento, correo electrónico, teléfono, especialidad (opcional) y contraseña válidos, EL SISTEMA creará la cuenta de usuario habilitada en el servicio de autenticación con el correo como identificador de acceso, la contraseña asignada como permanente y el rol "Técnico", registrará al técnico en estado "Activo" con fecha de creación en UTC, limpiará el formulario y mostrará `"Técnico registrado correctamente."`
- **RF-2**: SI algún campo del formulario de alta no cumple su regla de validación, ENTONCES EL SISTEMA no creará la cuenta de usuario ni registrará al técnico y mostrará:
  - Nombre vacío → `"El nombre completo es obligatorio."`
  - Nombre mayor a 150 caracteres → `"El nombre completo no puede superar los 150 caracteres."`
  - Documento vacío → `"El documento de identidad es obligatorio."`
  - Documento fuera de rango (1 a 20 caracteres) → `"El documento de identidad debe tener entre 1 y 20 caracteres."`
  - Documento duplicado → `"Ya existe un técnico registrado con el documento \"<documento>\"."`
  - Correo vacío → `"El correo electrónico es obligatorio."`
  - Correo inválido → `"El formato del correo electrónico no es válido."`
  - Correo duplicado (en técnicos o en el servicio de autenticación) → `"El correo electrónico \"<email>\" ya se encuentra registrado."`
  - Teléfono vacío → `"El teléfono de contacto es obligatorio."`
  - Teléfono con formato inválido → `"El teléfono de contacto es obligatorio y debe tener un formato válido."`
  - Especialidad mayor a 100 caracteres → `"La especialidad no puede superar los 100 caracteres."`
  - Contraseña vacía o menor a 6 caracteres → `"La contraseña es obligatoria y debe tener al menos 6 caracteres."`
  - Contraseña mayor a 100 caracteres → `"La contraseña no puede superar los 100 caracteres."`
- **RF-3**: SI el servicio de autenticación no está disponible o falla al crear la cuenta de acceso, ENTONCES EL SISTEMA no registrará al técnico en la base de datos y mostrará `"No se pudo comunicar con el servicio de autenticación. Intentá nuevamente."`
- **RF-4**: CUANDO un técnico registrado inicie sesión ingresando su correo electrónico y la contraseña asignada, EL SISTEMA autenticará su sesión con el rol "Técnico" y, al acceder a "Mis Mantenimientos", mostrará las órdenes de mantenimiento asociadas a su perfil de técnico.
- **RF-5**: CUANDO el Administrador confirme la baja de un técnico activo sin mantenimientos pendientes, EL SISTEMA cambiará el estado del técnico a "Inactivo", deshabilitará su cuenta de usuario en el servicio de autenticación y mostrará `"Técnico dado de baja correctamente."`
- **RF-6**: EL SISTEMA mantendrá sincronizados los técnicos precargados de demostración con sus respectivas cuentas de usuario, documentos normalizados y estados de habilitación en el servicio de autenticación.

## Requisitos no funcionales
- **Atomicidad lógica**: El técnico no debe quedar registrado en la base de datos local si la creación de su cuenta en el servicio de autenticación falla.
- **Seguridad**: La contraseña viaja únicamente hacia el servicio de autenticación y nunca se almacena en texto plano ni en tablas propias de la aplicación.
- **Idioma**: Interfaz y mensajes de validación íntegramente en español.

## Casos límite
- Nombre completo de una sola palabra (ej. `"Juan"`): se asigna como nombre en la cuenta de usuario y el apellido queda vacío.
- Nombre completo con múltiples palabras (ej. `"Juan Carlos Pérez Gómez"`): la primera palabra se toma como nombre y el resto como apellido en la cuenta de usuario.
- Correo electrónico con mayúsculas (ej. `"Tecnico.Nuevo@SolarUY.local"`): se normaliza a minúsculas tanto en el registro del técnico como en la cuenta de autenticación.
- Si al dar de baja a un técnico este no tuviera cuenta previa en el servicio de autenticación, la baja lógica del técnico se completa igualmente sin error.

## Fuera de alcance
- Cambio o restablecimiento posterior de contraseña por parte del usuario o del Administrador.
- Reactivación de técnicos dados de baja.
- Registro de nuevos usuarios con rol Administrador desde la aplicación.

## Criterios de finalización
- Reglas de negocio del servicio cubiertas con tests unitarios en verde (validación de contraseña, creación sincronizada en el servicio de autenticación, manejo de duplicados e indisponibilidad del servicio de autenticación, deshabilitación en la baja).
- Tests de arquitectura en verde.
- Verificación en navegador del flujo completo: alta de técnico con contraseña → cierre de sesión de Administrador → inicio de sesión con el nuevo técnico → visualización de sus tareas en `/mis-mantenimientos`.

## Dudas abiertas
- Ninguna pendiente si se confirman las reglas propuestas en esta especificación.
