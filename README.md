# Proyecto PIA - Sistema Distribuido .NET con IA (MCP)

Solución completa en **.NET 8** que implementa una arquitectura distribuida moderna, integrando servicios de IA mediante el protocolo **MCP (Model Context Protocol)** sobre WebSockets.

## 🚀 Características Principales

### Backend (ASP.NET Core 8)
- **API REST**: Gestión CRUD de especies (`AntSpecies`) y usuarios con autenticación **JWT**.
- **Servidor MCP (WebSockets)**: Implementación personalizada del protocolo JSON-RPC 2.0 para comunicación en tiempo real con la IA.
- **Inteligencia Artificial Avanzada**:
  - **RAG (Retrieval-Augmented Generation)**: La IA consulta la base de datos antes de responder.
  - **Memoria Conversacional**: El asistente "recuerda" el contexto de la charla.
  - **Anti-Alucinaciones**: Prompt de sistema estricto que prioriza datos reales.
- **Persistencia Híbrida**: 
  - **Modo Demo (Default)**: Todo funciona en memoria RAM. Ideal para descargar y probar al instante.
  - **Modo SQL**: Soporte completo para MySQL mediante configuración.

### Frontend
- **WPF (Desktop)**: Interfaz moderna con Chat IA y gestión CRUD completa de especies.
- **MAUI (Multiplataforma)**: App móvil con cliente WebSocket y gestión de datos.
- **Consola**: Herramienta administrativa para gestión rápida de usuarios y especies.

## 🛠️ Configuración y Ejecución

### Prerrequisitos
- **.NET 8 SDK**
- **Visual Studio 2022** (cargas: ASP.NET, Desktop, MAUI).
- **API Key de OpenAI** (opcional, para el chat inteligente).

### 1. Backend (API)
Por defecto, el proyecto está configurado en **MODO MEMORIA**.
1. Abre `src/Backend/Api` y ejecuta el proyecto.
2. La API arrancará en `http://localhost:5000`.

> **¿Quieres usar MySQL real?**
> 1. Ejecuta el script `database_setup.sql` en tu servidor MySQL.
> 2. Edita `src/Backend/Api/appsettings.json`.
> 3. Cambia `"Type": "Memory"` por `"Type": "MySQL"`.
> 4. Ajusta la `ConnectionString` con tu contraseña.

### 2. Gestión de Datos (Consola)
1. Ejecuta `src/Frontend/ConsoleApp`.
2. Login: `admin` / `admin`.
3. Usa el menú para crear usuarios o añadir especies manualmente.

### 3. Clientes Gráficos (WPF / MAUI)
*   **WPF**: Ejecuta `WpfApp`. Login con `admin` / `admin`.
*   **MAUI (Android)**:
    *   El proyecto viene preconfigurado para el Emulador de Android.
    *   Redirige automáticamente las peticiones a `10.0.2.2` (tu PC).
    *   Simplemente selecciona un Emulador y dale a Play.

## 🤖 IA y Configuración
Para que el asistente ("Comunidad IA") funcione:
1. Necesitas una clave de OpenAI.
2. Configúrala como variable de entorno en tu PC:
   ```powershell
   setx OPENAI_API_KEY "sk-tu-clave-aqui..."
   ```
   O usa "User Secrets" en Visual Studio.

## 📝 Arquitectura
- **Core**: Protocolo MCP sobre WebSockets (`/mcp`).
- **Datos**: Repositorio genérico (`IRepository`) que permite cambiar entre SQL y Memoria sin tocar el código de negocio.
