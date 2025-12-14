# ☁️ Guía: Cómo alojar tu Base de Datos MySQL Gratis en la Nube

Para que tu proyecto AntMaster sea accesible desde cualquier lugar (Universidad, Móvil, Casa de un amigo), necesitas que la base de datos esté en internet.

Aquí tienes dos opciones excelentes y gratuitas compatibles con MySQL.

---

## Opción A: Aiven (Recomendada, muy estándar)
Aiven ofrece un plan gratuito de MySQL muy estable.

1. **Registro**: Ve a [Aiven.io](https://aiven.io/) y regístrate (puedes usar GitHub).
2. **Crear Servicio**:
   - Pulsa **"Create service"**.
   - Selecciona **MySQL**.
   - Elige el plan **Free** (Cloud: DigitalOcean o aws, Región: la que quieras).
   - Dale a "Create".
3. **Obtener Credenciales**:
   - Una vez creado (tarda un par de minutos en ponerse en verde "Running"), verás la sección **Connection information**.
   - Copia la **Service URI** (es algo como `mysql://avnadmin:password@host:port/defaultdb?ssl-mode=REQUIRED`).
   - O copia los datos sueltos: `Host`, `Port`, `User`, `Password`.

## Opción B: TiDB Cloud (Serverless, muy generoso)
TiDB es totalmente compatible con MySQL y su plan gratuito es enorme.

1. **Registro**: Ve a [TiDB Cloud](https://tidbcloud.com/).
2. **Crear Cluster**: Elige "Serverless Tier".
3. **Credenciales**: Te dará un botón "Connect". Elige "General" o "ADO.NET" y copia los datos.

---

## ⚙️ Paso 2: Conectar tu Proyecto

Una vez tengas los datos de tu nube, edita el archivo `src/Backend/Api/appsettings.json` en tu proyecto.

Cambia la sección `Persistence` así:

```json
"Persistence": {
  "Type": "MySQL", 
  "ConnectionString": "Server=TU_HOST_DE_LA_NUBE;Port=TU_PUERTO;Database=defaultdb;Uid=TU_USUARIO;Pwd=TU_CONTRASEÑA;SslMode=Required;"
}
```

> **Nota**: `SslMode=Required` es importante para bases de datos en la nube (Aiven/TiDB lo exigen).

---

## 🚀 Paso 3: Inicializar las Tablas

Tu base de datos en la nube está vacía. Tienes que ejecutar el script de creación.

1. Descarga e instala **MySQL Workbench** o **DBeaver** (o usa la consola web que a veces traen Aiven/TiDB).
2. Crea una "Nueva Conexión" usando los mismos datos (Host, User, Pass) que pusiste en el json.
3. Abre el archivo `database_setup.sql` de este proyecto.
4. Ejecútalo (Rayo ⚡).

¡Listo! Ahora tu API se conecta a internet. Cualquiera que ejecute tu programa (si le das las credenciales) verá los mismos datos.

