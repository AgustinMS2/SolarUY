<# =========================================================================
CONFIGURACIÓN DEL PROYECTO
========================================================================= #>

<# 1. Pega aquí el Token que generaste en SonarQube #>
<# 2. PROJECT_KEY (opcional): Si lo dejas vacío o como "auto", se derivará automáticamente
   del nombre de la carpeta del proyecto. También puedes pasarlo como 2º parámetro. #>

# =========================================================================
# PARÁMETROS (DEBE SER LO PRIMERO EJECUTABLE EN EL SCRIPT)
# =========================================================================
param(
    [string]$ProjectPath,
    [string]$ProjectKeyParam
)

# =========================================================================
# CONFIGURACIÓN (DESPUÉS DE param)
# =========================================================================
$SONAR_TOKEN = "squ_9dc864593ac0b42c2d579841114c7a5fe9d9678b"
$PROJECT_KEY = "auto"

# =========================================================================
# NO MODIFICAR ABAJO DE ESTA LÍNEA
# =========================================================================

# =========================================================================
# RUTAS Y VALIDACIONES
# =========================================================================

# Si no se pasa ruta, usar directorio actual
if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = "."
}

# Convertir a ruta absoluta y resolverla
try {
    $ABS_PATH = (Resolve-Path $ProjectPath).Path
} catch {
    Write-Host "ERROR: El directorio $ProjectPath no existe." -ForegroundColor Red
    exit 1
}

if ($SONAR_TOKEN -eq "pega_tu_token_aqui") {
    Write-Host "ERROR: Debes modificar el archivo analizar.ps1 y poner tu SONAR_TOKEN." -ForegroundColor Red
    exit 1
}

# =========================================================================
# DETERMINAR PROJECT_KEY
# =========================================================================
# Prioridad: 1) Parámetro explícito, 2) Variable del script (si no es "auto"), 3) Nombre de carpeta
if (-not [string]::IsNullOrWhiteSpace($ProjectKeyParam)) {
    $PROJECT_KEY = $ProjectKeyParam
} elseif ($PROJECT_KEY -eq "auto" -or [string]::IsNullOrWhiteSpace($PROJECT_KEY)) {
    $folderName = Split-Path $ABS_PATH -Leaf
    # Sanitizar: solo alfanuméricos, guiones y guiones bajos
    $PROJECT_KEY = $folderName -replace '[^a-zA-Z0-9_-]', '-'
    Write-Host "PROJECT_KEY auto-detectado: $PROJECT_KEY" -ForegroundColor Cyan
}

# =========================================================================
# CONFIGURAR QUALITY GATE ESTRICTO
# =========================================================================
Write-Host "Configurando Quality Gate Estricto en SonarQube..." -ForegroundColor Cyan
$EncodedToken = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("${SONAR_TOKEN}:"))
$Headers = @{ Authorization = "Basic $EncodedToken" }
$BaseUrl = "http://localhost:9000/api/qualitygates"
$QGName = "EstrictoTaller"

try { Invoke-RestMethod -Uri "$BaseUrl/create?name=$QGName" -Method Post -Headers $Headers -ErrorAction SilentlyContinue | Out-Null } catch {}
try { Invoke-RestMethod -Uri "$BaseUrl/create_condition?gateName=$QGName&metric=blocker_violations&op=GT&error=0" -Method Post -Headers $Headers -ErrorAction SilentlyContinue | Out-Null } catch {}
try { Invoke-RestMethod -Uri "$BaseUrl/create_condition?gateName=$QGName&metric=security_rating&op=GT&error=1" -Method Post -Headers $Headers -ErrorAction SilentlyContinue | Out-Null } catch {}
try { Invoke-RestMethod -Uri "$BaseUrl/create_condition?gateName=$QGName&metric=reliability_rating&op=GT&error=1" -Method Post -Headers $Headers -ErrorAction SilentlyContinue | Out-Null } catch {}
try { Invoke-RestMethod -Uri "$BaseUrl/set_as_default?name=$QGName" -Method Post -Headers $Headers -ErrorAction SilentlyContinue | Out-Null } catch {}

$IMAGE_NAME = "mi-taller-scanner"
$imageExists = docker images -q $IMAGE_NAME
if ([string]::IsNullOrWhiteSpace($imageExists)) {
    Write-Host "La imagen de Docker '$IMAGE_NAME' no existe en tu sistema." -ForegroundColor Yellow
    Write-Host "Construyéndola automáticamente por primera vez (esto puede tardar unos minutos)..." -ForegroundColor Yellow
    $ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
    docker build -t $IMAGE_NAME "$ScriptDir/scanner"
}

Write-Host "Iniciando contenedor analizador para $PROJECT_KEY..." -ForegroundColor Cyan
Write-Host "Directorio a analizar: $ABS_PATH" -ForegroundColor Cyan

docker run --rm --network host `
  -v "${ABS_PATH}:/app" `
  -e SONAR_HOST_URL="http://localhost:9000" `
  -e SONAR_TOKEN=$SONAR_TOKEN `
  -e PROJECT_KEY=$PROJECT_KEY `
  mi-taller-scanner