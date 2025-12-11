## Proyecto PIA - Sistema Distribuido .NET con IA

Solución completa en .NET 8 que integra:
- **API REST** para gestión de especies de hormigas y autenticación JWT.
- **Persistencia intercambiable** (Memoria / MySQL).
- **Clientes front-end**: consola, WPF de escritorio y MAUI multiplataforma.
- **Integración con IA (OpenAI/MCP)** para consultas avanzadas sobre los datos.

Este repositorio corresponde al proyecto PIA de la asignatura, preparado para ser clonado y ejecutado en Visual Studio 2022.

## Estructura de la solución
- `src/Backend/Api`: API REST (ASP.NET Core 8).
- `src/Backend/Persistence`: lógica de acceso a datos, factoría de repositorios y repositorios SQL/Memoria.
- `src/Shared/Models`: modelos compartidos (`AntSpecies`, `User`, DTOs de autenticación, etc.).
- `src/Frontend/ConsoleApp`: cliente de consola para login y carga de datos desde CSV.
- `src/Frontend/WpfApp`: cliente WPF de escritorio con vistas maestro-detalle y chat IA.
- `src/Frontend/MauiApp`: app MAUI (Android/Windows) con login y chat IA.
- `init_users.sql`: script para crear usuarios iniciales en MySQL (si se usa persistencia SQL).

## Instrucciones de Ejecución

### Prerrequisitos
- Visual Studio 2022 (con cargas de trabajo: ASP.NET, .NET Desktop, .NET MAUI).
- MySQL Server (opcional, por defecto usa Memoria).

### Paso 1: Iniciar el Backend
1. Abre la carpeta `src/Backend/Api` o el proyecto `Api.csproj` en Visual Studio.
2. Ejecuta el proyecto. Se abrirá una consola o navegador en `http://localhost:5000` (o puerto aleatorio, verifica la consola).
3. **Importante:** Anota la URL base si es diferente a `http://localhost:5000`.

### Paso 2: Cargar Datos (Consola)
1. Abre `src/Frontend/ConsoleApp`.
2. Verifica el archivo `App.config`: asegúrate de que `CsvPath` apunta a tu fichero `sp500_2025_h1_wide_clean.csv`.
3. Ejecuta la aplicación.
4. Login con: `admin` / `admin`.
5. Selecciona la opción **1** para cargar los datos del CSV a la API.

### Paso 3: Cliente WPF
1. Abre `src/Frontend/WpfApp`.
2. Ejecuta la aplicación.
3. Login: `admin` / `admin`.
4. Navega por el menú para ver los datos ("Master") o chatear con el sistema ("Chat MCP").

### Paso 4: Cliente MAUI
1. Abre `src/Frontend/MauiApp`.
2. Selecciona un emulador (Android) o "Windows Machine".
3. Ejecuta la App.
4. Login: `admin` / `admin`.
5. Prueba el "Chat IA" o la lista con Lazy Loading.

## Configuración
- Para cambiar entre MySQL y Memoria, edita `src/Backend/Api/appsettings.json` y cambia `Persistence:Type` a "MySQL" (y ajusta la `ConnectionString`).

