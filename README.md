# Proyecto Final EV1 - Sistema Distribuido .NET

Este proyecto integra una API REST, persistencia dual (MySQL/Memoria), integración con IA (Protocolo MCP) y tres clientes front-end (Consola, WPF, MAUI).

## Estructura
- **src/Backend/Api**: Servidor REST (ASP.NET Core 8).
- **src/Backend/Persistence**: Lógica de acceso a datos y Repositorios.
- **src/Shared/Models**: Entidades compartidas.
- **src/Frontend/ConsoleApp**: Cliente de carga de datos y pruebas.
- **src/Frontend/WpfApp**: Interfaz de escritorio Windows (MDI).
- **src/Frontend/MauiApp**: Interfaz multiplataforma (Móvil).

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

