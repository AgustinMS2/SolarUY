#!/bin/bash
set -e

# Verificaciones básicas de variables de entorno
if [ -z "$SONAR_HOST_URL" ]; then
    echo "ERROR: La variable SONAR_HOST_URL no está definida."
    echo "Ejemplo: -e SONAR_HOST_URL=http://localhost:9000"
    exit 1
fi

if [ -z "$SONAR_TOKEN" ]; then
    echo "ERROR: La variable SONAR_TOKEN no está definida."
    exit 1
fi

if [ -z "$PROJECT_KEY" ]; then
    echo "ERROR: La variable PROJECT_KEY no está definida."
    exit 1
fi

echo "========================================="
echo "Iniciando análisis de SonarQube para: $PROJECT_KEY"
echo "Servidor: $SONAR_HOST_URL"
echo "========================================="

# 0. Trabajar sobre una COPIA del código (sin bin/, obj/ ni .git).
#    Así el análisis no choca con binarios en uso en la máquina host (por ejemplo,
#    la aplicación corriendo con Aspire) ni deja artefactos de Linux en bin/ y obj/.
WORK_DIR=/work
rm -rf "$WORK_DIR" && mkdir -p "$WORK_DIR"
tar -C /app \
  --exclude=./bin --exclude=./obj \
  --exclude='*/bin' --exclude='*/obj' \
  --exclude=.git --exclude=.sonarqube --exclude=.vs --exclude=node_modules \
  -cf - . | tar -C "$WORK_DIR" -xf -
cd "$WORK_DIR"

# 1. Iniciar el escáner
dotnet sonarscanner begin \
  /k:"$PROJECT_KEY" \
  /d:sonar.host.url="$SONAR_HOST_URL" \
  /d:sonar.login="$SONAR_TOKEN"

# 2. Compilar el proyecto
echo "Construyendo proyecto..."
#    Sin servidores de compilación residentes: en máquinas con poca memoria para
#    Docker (~3 GB) el servidor de SonarQube terminaba muerto por falta de RAM al subir el reporte.
dotnet build -nodeReuse:false -p:UseSharedCompilation=false
dotnet build-server shutdown >/dev/null 2>&1 || true

# 3. Finalizar el análisis y enviar resultados
echo "Finalizando análisis y enviando resultados..."
dotnet sonarscanner end /d:sonar.login="$SONAR_TOKEN"

echo "========================================="
echo "Análisis completado exitosamente."
echo "========================================="
