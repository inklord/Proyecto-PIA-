# 📘 Documentación Técnica Detallada: Proyecto AntMaster

Este documento desglosa la arquitectura interna, las decisiones de diseño y los algoritmos específicos utilizados en la solución.

---

## 1. Arquitectura de la Solución (N-Capas Distribuida)

La solución sigue un diseño modular para separar responsabilidades y permitir la reutilización de código.

### 🧩 `src/Shared/Models`
**¿Qué es?** Una biblioteca de clases (.dll) que comparten todos los proyectos.
**¿Por qué?**
- Evita duplicar código. Si cambiamos la clase `AntSpecies` (añadimos un campo), el Backend, el WPF y el MAUI se enteran automáticamente.
- Contiene los **DTOs** (Data Transfer Objects) como `LoginRequest` o `JsonRpcRequest` que aseguran que cliente y servidor hablen el mismo "idioma".

### 🧠 `src/Backend/Api` (ASP.NET Core 8)
El cerebro del sistema. No devuelve HTML, sino datos (JSON).
- **Controladores (`Controllers/`)**: Manejan peticiones HTTP clásicas (GET/POST) para Login y CRUD de especies.
- **WebSocket Handler (`McpWebSocketHandler`)**: Maneja la conexión persistente con la IA.
- **Inyección de Dependencias (`Program.cs`)**: Aquí se configuran los servicios. `IRepository` se registra como *Singleton* para que los datos en memoria persistan entre peticiones.

### 💾 `src/Backend/Persistence`
Patrón de Repositorio para abstraer el acceso a datos.
- **`IRepository<T>`**: Una interfaz (contrato) que dice: "Cualquier base de datos debe tener `GetAll`, `Add`, `Delete`".
- **`MemoryRepository`**: Implementación volátil (Listas C#). Rápida para desarrollo.
- **`SqlRepository`**: Implementación real (MySQL).
- **`RepositoryFactory`**: Un patrón de fábrica que decide cuál usar según el `appsettings.json`.

---

## 2. El Corazón de la IA: Protocolo MCP sobre WebSockets

Esta es la parte más compleja y potente del proyecto. No usamos HTTP simple para la IA, usamos **WebSockets**.

### 📡 ¿Por qué WebSockets?
HTTP es "petición-respuesta" y cierra. WebSockets mantiene un "tubo" abierto. Esto permite:
1. **Estado**: El servidor sabe quién eres durante toda la sesión.
2. **Velocidad**: No hay handshake en cada mensaje.

### 📜 Protocolo JSON-RPC 2.0
Implementamos el estándar JSON-RPC para encapsular mensajes.
- **Request**: `{ "jsonrpc": "2.0", "id": "uuid", "method": "tools/call", "params": {...} }`
- **Response**: `{ "jsonrpc": "2.0", "id": "uuid", "result": {...} }`
- **Error**: `{ "jsonrpc": "2.0", "id": "uuid", "error": {...} }`

### ⚙️ `McpWebSocketHandler.cs` (Desglose)

#### A. Detección de Typos (Algoritmo Levenshtein)
Antes de molestar a la IA (OpenAI), intentamos resolver la duda localmente.
1. **Limpieza**: Si escribes "¡Busca la myrmecia!", extraemos "myrmecia".
2. **Cálculo**: Comparamos esa palabra con todas las especies de la BD usando **Distancia de Levenshtein** (cuántas letras hay que cambiar para que sean iguales).
3. **Umbrales**:
   - **> 85%**: Asumimos que es un error ortográfico ("nigrocinta" -> "nigrocincta") y corregimos automáticamente.
   - **> 70%**: Sugerimos opciones ("¿Quizás quisiste decir...?").
   - **< 70%**: No encontramos nada local, pasamos a OpenAI.

#### B. Memoria Conversacional (Contexto)
La IA no tiene memoria por defecto. Nosotros se la damos:
- Tenemos una lista `_conversationHistory`.
- Guardamos: "Usuario: ¿Cuál es más grande?" y "Asistente: Myrmecia...".
- En la siguiente pregunta, enviamos **todo** ese historial a OpenAI. Así sabe a qué te refieres con "esa especie".

#### C. RAG (Retrieval-Augmented Generation)
Para evitar que la IA invente ("alucine"):
1. Leemos las especies de la BD.
2. Inyectamos esos datos en el **System Prompt** ("Eres un experto... Tienes estos datos en BD: ...").
3. Le damos reglas estrictas: "Si no sabes el tamaño exacto, usa la lógica de género, no inventes".

---

## 3. Clientes Inteligentes (WPF y MAUI)

Los clientes no son "tontos", tienen lógica avanzada de comunicación.

### 🔄 Correlación de Mensajes Asíncronos
En WebSockets, envías un mensaje y la respuesta puede llegar 1 segundo después, mezclada con otras. ¿Cómo sabe el cliente qué respuesta es de qué pregunta?
- **El Truco**: Usamos un `ConcurrentDictionary<string, TaskCompletionSource<McpResult>>`.
1. Generamos un ID único (`Guid`).
2. Guardamos una "promesa" (`TaskCompletionSource`) en el diccionario con ese ID.
3. Enviamos el mensaje.
4. Cuando llega un mensaje del servidor, miramos su ID.
5. Buscamos la promesa en el diccionario y la completamos (`SetResult`).

### 🧩 Fragmentación de WebSockets
Un problema grave que solucionamos:
Los mensajes grandes (respuestas largas de IA) no caben en un solo paquete de red.
- **Solución**: Implementamos un bucle `do { ... } while (!result.EndOfMessage)` que acumula los bytes en un `StringBuilder` hasta que el servidor dice "Fin del mensaje". Solo entonces intentamos convertirlo a JSON.

### 📱 MAUI (Multi-platform App UI)
- **Desafío Android**: El emulador de Android no entiende `localhost`. Tuvimos que crear lógica para cambiar la URL a `10.0.2.2` cuando detectamos que corre en Android.
- **Threads**: Las respuestas de WebSocket llegan en hilos de fondo. Usamos `MainThread.BeginInvokeOnMainThread` para poder pintar la UI sin que la app crashee.

---

## 4. Seguridad y Flujo

### 🔐 Autenticación JWT
- El servidor emite un **Token** firmado al hacer login.
- Los clientes guardan ese token y lo envían en la cabecera `Authorization: Bearer <token>` en cada petición HTTP.

### 🛡️ Protección de UI (WPF)
- En `MainWindow.xaml.cs`, controlamos la visibilidad.
- Los botones de navegación están `Collapsed` (invisibles) por defecto.
- Solo tras un `LoginAsync` exitoso (que valida contra la API), cambiamos la visibilidad a `Visible`. Esto impide que usuarios no autenticados accedan a las vistas.

---

## Resumen del Flujo de una Consulta IA

1. **Usuario (WPF)**: Escribe "Myrmecia nigrocinta".
2. **Cliente**: Genera ID, envía JSON por WebSocket.
3. **Servidor**:
   - Recibe mensaje.
   - Calcula Levenshtein: detecta similitud 95% con "Myrmecia nigrocincta".
   - **Decisión**: Autocorregir.
   - Añade corrección al Historial.
   - Crea Prompt: "El usuario dijo X, quiso decir Y. Contexto BD: [Datos reales de Myrmecia]".
   - Llama a OpenAI API.
4. **OpenAI**: Devuelve respuesta basada en los datos inyectados.
5. **Servidor**: Recibe respuesta, la añade al Historial, envía JSON al cliente.
6. **Cliente**: Recibe JSON, busca el ID, desbloquea la UI y muestra el texto + la foto de la especie encontrada en BD.

