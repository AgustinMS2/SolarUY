#!/bin/bash

# =========================================================================
# CONFIGURACIÓN DEL PROYECTO
# =========================================================================

# 1. Pega aquí el Token que generaste en SonarQube
SONAR_TOKEN="pega_tu_token_aqui"

# 2. PROJECT_KEY (opcional): Si lo dejas vacío o como "auto", se derivará automáticamente
#    del nombre de la carpeta del proyecto. También puedes pasarlo como 2º parámetro.
PROJECT_KEY="auto"

# =========================================================================
# NO MODIFICAR ABAJO DE ESTA LÍNEA
# =========================================================================

# =========================================================================
# RUTAS Y VALIDACIONES
# =========================================================================

# 3. Leer la ruta del proyecto desde el primer parámetro (o usar la carpeta actual por defecto)
# 4. Leer PROJECT_KEY opcional desde el segundo parámetro
PROJECT_PATH="${1:-$(pwd)}"
PROJECT_KEY_PARAM="${2:-}"

# Convertir a ruta absoluta (requerido por Docker para volúmenes)
ABS_PATH=$(realpath "$PROJECT_PATH")

if [ "$SONAR_TOKEN" = "pega_tu_token_aqui" ]; then
    echo "ERROR: Debes modificar el archivo analizar.sh y poner tu SONAR_TOKEN."
    exit 1
fi

if [ ! -d "$ABS_PATH" ]; then
    echo "ERROR: El directorio $ABS_PATH no existe."
    exit 1
fi

# =========================================================================
# DETERMINAR PROJECT_KEY
# =========================================================================
# Prioridad: 1) Parámetro explícito, 2) Variable del script (si no es "auto"), 3) Nombre de carpeta
if [ -n "$PROJECT_KEY_PARAM" ]; then
    PROJECT_KEY="$PROJECT_KEY_PARAM"
elif [ "$PROJECT_KEY" = "auto" ] || [ -z "$PROJECT_KEY" ]; then
    FOLDER_NAME=$(basename "$ABS_PATH")
    # Sanitizar: solo alfanuméricos, guiones y guiones bajos
    PROJECT_KEY=$(echo "$FOLDER_NAME" | sed 's/[^a-zA-Z0-9_-]/-/g')
    echo "PROJECT_KEY auto-detectado: $PROJECT_KEY"
fi

# =========================================================================
# CONFIGURAR QUALITY GATE ESTRICTO
# =========================================================================
echo "Configurando Quality Gate Estricto en SonarQube..."
QG_NAME="EstrictoTaller"
# Intentar crear el Quality Gate y agregarle condiciones de 0 tolerancia a bloqueos (falla silenciosamente si ya existe)
curl -s -u "${SONAR_TOKEN}:" -X POST "http://localhost:9000/api/qualitygates/create?name=${QG_NAME}" > /dev/null
curl -s -u "${SONAR_TOKEN}:" -X POST "http://localhost:9000/api/qualitygates/create_condition?gateName=${QG_NAME}&metric=blocker_violations&op=GT&error=0" > /dev/null
curl -s -u "${SONAR_TOKEN}:" -X POST "http://localhost:9000/api/qualitygates/create_condition?gateName=${QG_NAME}&metric=security_rating&op=GT&error=1" > /dev/null
curl -s -u "${SONAR_TOKEN}:" -X POST "http://localhost:9000/api/qualitygates/create_condition?gateName=${QG_NAME}&metric=reliability_rating&op=GT&error=1" > /dev/null
curl -s -u "${SONAR_TOKEN}:" -X POST "http://localhost:9000/api/qualitygates/set_as_default?name=${QG_NAME}" > /dev/null

IMAGE_NAME="mi-taller-scanner"
if [[ "$(docker images -q $IMAGE_NAME 2> /dev/null)" == "" ]]; then
    echo "La imagen de Docker '$IMAGE_NAME' no existe en tu sistema."
    echo "Construyéndola automáticamente por primera vez (esto puede tardar unos minutos)..."
    SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" &> /dev/null && pwd)"
    docker build -t $IMAGE_NAME "$SCRIPT_DIR/scanner"
fi

echo "Iniciando contenedor analizador para $PROJECT_KEY..."
echo "Directorio a analizar: $ABS_PATH"

docker run --rm --network host \
  -v "$ABS_PATH":/app \
  -e SONAR_HOST_URL="http://localhost:9000" \
  -e SONAR_TOKEN="$SONAR_TOKEN" \
  -e PROJECT_KEY="$PROJECT_KEY" \
  mi-taller-scanner
