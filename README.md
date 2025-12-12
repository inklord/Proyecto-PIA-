# Proyecto PIA - Sistema Distribuido .NET con IA (MCP)

Solución completa en **.NET 8** que implementa una arquitectura distribuida moderna, integrando servicios de IA mediante el protocolo **MCP (Model Context Protocol)** sobre WebSockets.

Este proyecto permite la gestión y consulta de información mirmecológica (hormigas), combinando una base de datos tradicional con un asistente experto basado en IA capaz de razonar sobre los datos y mantener el contexto de la conversación.

## 🚀 Características Principales

### Backend (ASP.NET Core 8)
- **API REST**: Gestión CRUD de especies (`AntSpecies`) y usuarios con autenticación **JWT**.
- **Servidor MCP (WebSockets)**: Implementación personalizada del protocolo JSON-RPC 2.0 para comunicación en tiempo real con la IA.
- **Inteligencia Artificial Avanzada**:
  - **RAG (Retrieval-Augmented Generation)**: La IA consulta la base de datos antes de responder.
  - **Memoria Conversacional**: El asistente "recuerda" el contexto de la charla (preguntas anteriores y sus propias respuestas).
  - **Sugerencias Inteligentes**: Algoritmo de similitud (Levenshtein + heurísticas) para detectar typos en nombres científicos y sugerir correcciones.
  - **Anti-Alucinaciones**: Prompt de sistema estricto que prioriza datos reales y reglas taxonómicas de tamaño.
- **Persistencia Flexible**: Soporte para InMemory (por defecto) o MySQL.

### Frontend
- **WPF (Desktop)**:
  - Interfaz moderna con navegación controlada (Login obligatorio).
  - Chat interactivo con el experto MCP.
  - Gestión de especies.
- **MAUI (Multiplataforma)**:
  - App móvil (Android) y escritorio (Windows) con la misma lógica de negocio.
  - Cliente WebSocket robusto con manejo de fragmentación de mensajes y reconexión.
- **Consola**:
  - Herramienta administrativa para carga masiva de datos de prueba.

## 📂 Estructura de la Solución

- `src/Backend/Api`: Servidor principal. Contiene los controladores REST y el `McpWebSocketHandler`.
- `src/Backend/Persistence`: Capa de acceso a datos (Repository Pattern).
- `src/Shared/Models`: Biblioteca de clases compartida entre todos los proyectos.
- `src/Frontend/WpfApp`: Cliente de escritorio Windows.
- `src/Frontend/MauiApp`: Cliente móvil/híbrido.
- `src/Frontend/ConsoleApp`: Utilidad de carga de datos.

## 🛠️ Configuración y Ejecución

### Prerrequisitos
- **.NET 8 SDK**
- **Visual Studio 2022** (cargas de trabajo: ASP.NET, Desktop, MAUI).
- Una **API Key de OpenAI** válida.

### 1. Configurar Backend
1. Navega a `src/Backend/Api`.
2. Configura tu clave de OpenAI. Puedes hacerlo de dos formas:
   - **Opción A (Recomendada)**: Variable de entorno.
     ```powershell
     setx OPENAI_API_KEY "sk-..."
     ```
   - **Opción B (Dev)**: User Secrets.
     ```powershell
     dotnet user-secrets set "OpenAI:ApiKey" "sk-..."
     ```
3. Ejecuta el proyecto `Api`. Se iniciará en `http://localhost:5000`.

### 2. Cargar Datos de Ejemplo
1. Ejecuta `src/Frontend/ConsoleApp`.
2. Inicia sesión con `admin` / `admin`.
3. Selecciona la opción **1** para cargar especies de prueba en la memoria del servidor.

### 3. Ejecutar Clientes (WPF / MAUI)
1. **WPF**: Establece `WpfApp` como proyecto de inicio y ejecuta.
   - Login: `admin` / `admin`.
   - Verás que el menú se desbloquea tras el login.
   - Prueba el chat "Comunidad (IA)" preguntando por especies cargadas o generales.
2. **MAUI**: Selecciona el framework de destino (Android Emulator o Windows Machine) y ejecuta.

### 🤖 Ejecución en Android (Emulador)
El proyecto MAUI está preconfigurado para funcionar en el Emulador de Android de Visual Studio / Android Studio.

**Configuración automática incluida:**
- **Dirección IP**: La app detecta si corre en Android y cambia `localhost` por **`10.0.2.2`** (el alias del host en el emulador).
- **Tráfico HTTP**: Se ha habilitado `usesCleartextTraffic="true"` en el manifiesto para permitir conectar con la API local sin HTTPS.

**Pasos:**
1. Asegúrate de que la API (`src/Backend/Api`) esté corriendo en tu PC.
2. En Visual Studio, selecciona el proyecto de inicio `MauiApp`.
3. En el selector de dispositivo, elige un emulador (ej: "Pixel 5 - API 33").
4. Dale a ejecutar. La app conectará automáticamente con tu servidor local.

## 🧠 Capacidades del Asistente MCP

El asistente no solo responde preguntas generales, sino que está conectado a los datos de la aplicación.
- **Pregunta**: *"¿Qué especies de Camponotus tienes?"* -> Buscará en la BD y listará las reales.
- **Typos**: *"Enséñame una myrmecia nigrocinta"* -> Detectará el error, buscará *Myrmecia nigrocincta* (95% similitud), corregirá la búsqueda y mostrará la ficha correcta.
- **Comparaciones**: *"¿Cuál es más grande?"* (tras ver dos especies) -> Usará la memoria de la conversación para saber de qué especies hablas y aplicará lógica científica para responder.

## 📝 Notas Técnicas
- El protocolo MCP se implementa sobre WebSockets en el endpoint `/mcp`.
- El servidor maneja el estado de la conexión y el historial de mensajes en memoria volátil (se reinicia con el servidor).
- La persistencia por defecto es en memoria para facilitar la prueba (`MemoryRepository`), pero puede cambiarse a MySQL en `appsettings.json`.
