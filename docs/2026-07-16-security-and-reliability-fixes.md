# Cambios de seguridad y confiabilidad — 2026-07-16

Este documento resume, a detalle, todos los cambios realizados en la sesión de trabajo del 16 de julio de 2026. Partió de una revisión de los últimos commits de encriptación/Redis (`e72b8ca`, `3d99c70`) y se extendió a una revisión general del backend, resolviendo los problemas encontrados uno por uno.

## Índice

1. [Clave JWT hardcodeada](#1-clave-jwt-hardcodeada)
2. [Autorización faltante en controladores REST](#2-autorización-faltante-en-controladores-rest)
3. [SSRF: bloqueo de loopback en NetworkSafetyValidator](#3-ssrf-bloqueo-de-loopback-en-networksafetyvalidator)
4. [Redis sin autenticación](#4-redis-sin-autenticación)
5. [Migraciones de EF Core fallando en silencio](#5-migraciones-de-ef-core-fallando-en-silencio)
6. [Proceso no abortaba ante un fallo de arranque](#6-proceso-no-abortaba-ante-un-fallo-de-arranque)
7. [Configuración muerta y bug de `Encryption:CurrentVersion`](#7-configuración-muerta-y-bug-de-encryptioncurrentversion)
8. [Dockerfile de producción con el SDK completo](#8-dockerfile-de-producción-con-el-sdk-completo)
9. [Auto-sanación incompleta del caché de Redis](#9-auto-sanación-incompleta-del-caché-de-redis)
10. [Rate limiting en endpoints de autenticación](#10-rate-limiting-en-endpoints-de-autenticación)
11. [Refresh tokens y revocación de sesiones](#11-refresh-tokens-y-revocación-de-sesiones)
12. [Mapa de topología roto sin metrics-server](#12-mapa-de-topología-roto-sin-metrics-server)
13. [Pendientes / recomendaciones para el futuro](#pendientes--recomendaciones-para-el-futuro)

---

## 1. Clave JWT hardcodeada

**Problema:** `Servidor/appsettings.json` tenía la clave de firma de JWT escrita en texto plano y commiteada al repositorio (`"Jwt:Key": "ClaveSeguraParaKubeOrchestratorPlatform"`). Cualquiera con acceso al código podía firmar tokens válidos para cualquier usuario, en cualquier despliegue que no sobreescribiera ese valor.

**Fix:**
- `Servidor/appsettings.json`: `Jwt:Key` pasó a `""`.
- `Servidor/Program.cs`: se agregó una validación explícita al arrancar — si `Jwt:Key` no está definida, la app lanza `InvalidOperationException` con un mensaje claro en vez de fallar más adelante de forma críptica.
- `.env.example`: se documentaron `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience` como variables de entorno esperadas.
- `.env` (local, ignorado por git): se generó una clave real con `openssl rand -base64 48`.

**Impacto:** todos los tokens JWT emitidos antes de este cambio (firmados con la clave vieja) quedan inválidos — los usuarios deben volver a autenticarse.

---

## 2. Autorización faltante en controladores REST

**Problema:** `ClustersController`, `PodsController`, `ServicesController`, `DeploymentsController` y `NamespacesController` no tenían el atributo `[Authorize]`, a diferencia de `ClusterHub`/`TerminalHub` (SignalR), que sí lo tenían. La app "funcionaba" solo porque `CurrentUserService.UserId` lanza `UnauthorizedAccessException` si no hay claim `id` en el token — un control de acceso incidental y frágil: cualquier acción que no llamara a `UserId` antes quedaba potencialmente abierta, y las excepciones no controladas podían devolver `500` en vez de `401`.

**Fix:** se agregó `[Authorize]` a nivel de clase en los 5 controladores:
- `Servidor/KubeOrchestratorPlatform/Controllers/ClustersController.cs`
- `Servidor/KubeOrchestratorPlatform/Controllers/PodController.cs` (clase `PodsController`)
- `Servidor/KubeOrchestratorPlatform/Controllers/NetworkSvcController.cs` (clase `ServicesController`)
- `Servidor/KubeOrchestratorPlatform/Controllers/DeploymentController.cs` (clase `DeploymentsController`)
- `Servidor/KubeOrchestratorPlatform/Controllers/NamespacesController.cs`

---

## 3. SSRF: bloqueo de loopback en NetworkSafetyValidator

**Problema:** `NetworkSafetyValidator.EnsureSafeClusterEndpointAsync` (llamado desde `ClusterConfigurationService` al registrar la URL del servidor de un cluster) solo bloqueaba IPv6 link-local y el rango de metadata de nube `169.254.0.0/16`. No bloqueaba loopback (`127.0.0.0/8`, `::1`), lo que permitía apuntar la URL de un "cluster" a servicios corriendo en el propio host del backend.

**Decisión de producto:** se preguntó explícitamente si además bloquear los rangos privados RFC1918 (`10.0.0.0/8`, `172.16.0.0/12`, `192.168.0.0/16`). Se decidió **no** bloquearlos, porque muchos clusters reales de esta plataforma corren en redes privadas (on-prem, VPC corporativa) y bloquearlos rompería ese caso de uso legítimo.

**Fix:** `Servidor/KubeOrchestratorPlatform/Services/Security/NetworkSafetyValidator.cs` — se agregó `IPAddress.IsLoopback(ip)` a las condiciones bloqueadas.

---

## 4. Redis sin autenticación

**Problema:** el servicio `redis_cache` en `docker-compose.yml` no tenía contraseña (`redis:alpine` sin `--requirepass`) y su puerto quedaba publicado al host (`6380:6379`). Cualquier proceso en esa red podía leer/escribir el caché directamente.

**Fix:**
- `docker-compose.yml`: `redis_cache` ahora arranca con `command: ["redis-server", "--requirepass", "${REDIS_PASSWORD}"]`.
- `Servidor/Program.cs`: la connection string de Redis ahora agrega `,password={REDIS_PASSWORD}` cuando la variable está definida.
- `.env.example`: se documentó `REDIS_PASSWORD`.
- `.env` (local): se generó una contraseña real con `openssl rand -base64 24`.

**Verificación:** confirmado en runtime — `redis-cli PING` sin password devuelve `NOAUTH Authentication required`; con la password correcta responde `PONG`.

---

## 5. Migraciones de EF Core fallando en silencio

**Problema:** en `Servidor/Program.cs`, si `context.Database.Migrate()` fallaba al arrancar, el error solo se registraba con `Log.Error(...)` y la aplicación seguía sirviendo tráfico contra un esquema de base de datos potencialmente desincronizado.

**Fix:** el `catch` de ese bloque ahora hace `Log.Fatal(...)` y **relanza la excepción** (`throw;`), de modo que el fallo de migración aborta el arranque en vez de continuar en un estado inconsistente.

---

## 6. Proceso no abortaba ante un fallo de arranque

**Problema:** el `try/catch` que envuelve todo `Main()` capturaba cualquier excepción de arranque, la registraba con `Log.Fatal`, y el proceso terminaba con código de salida `0` — como si hubiera cerrado limpiamente. En Docker/Kubernetes esto significa que un contenedor que **falló al arrancar** no se reinicia automáticamente, porque el orquestador ve una salida "exitosa".

**Fix:** se agregó `Environment.ExitCode = 1;` dentro del `catch` de `Main`, antes del `finally` que hace `Log.CloseAndFlush()`.

---

## 7. Configuración muerta y bug de `Encryption:CurrentVersion`

**Problema doble:**
- `Servidor/appsettings.json` conservaba un bloque `"Encryption": { "Key": "", "IV": "" }` que ya no usaba ningún código (el `EncryptionService` actual lee `Encryption:Keys:{version}:Key` y `Encryption:CurrentVersion`).
- **Bug real:** `Encryption:CurrentVersion` no estaba definida en ningún lado (ni `appsettings.json`, ni `.env`, ni `.env.example`). Esto significa que `EncryptionService.Encrypt()` lanzaba excepción **cada vez** que se intentaba cifrar algo (por ejemplo, al cachear credenciales de un cluster en Redis).

**Fix:**
- Se eliminó el bloque muerto de `appsettings.json`.
- Se agregó `Encryption__CurrentVersion=v1` a `.env.example` y a `.env` local.

---

## 8. Dockerfile de producción con el SDK completo

**Problema:** la etapa final del `Dockerfile` usaba `mcr.microsoft.com/dotnet/sdk:8.0` (en vez de la imagen `aspnet:8.0`, solo runtime), copiaba todo el código fuente al contenedor final, y ejecutaba `dotnet restore && dotnet tool restore` durante el build de esa imagen — aparentemente para tener el CLI `dotnet-ef` disponible en runtime.

**Investigación:** se confirmó que `dotnet-ef` **nunca se invoca dentro del contenedor** — las migraciones se aplican en código, programáticamente, vía `context.Database.Migrate()` en `Program.cs`. El SDK completo, el código fuente y las herramientas de compilación en la imagen final no aportaban nada, solo aumentaban el tamaño de la imagen y la superficie de ataque.

**Fix:** se revirtió la etapa final del `Dockerfile` a `aspnet:8.0`, copiando solo el resultado de `dotnet publish`.

**Verificación:** build de imagen exitoso, tamaño final de **241 MB** (antes, con el SDK completo + fuente + herramientas, era sustancialmente mayor).

---

## 9. Auto-sanación incompleta del caché de Redis

**Contexto:** el commit `e72b8ca` migró el cifrado de credenciales en Redis de AES-CBC (IV fijo) a AES-GCM (nonce aleatorio). Cualquier entrada cacheada **antes** de ese cambio queda en el formato viejo y ya no se puede desencriptar con el código nuevo.

**Lo que ya funcionaba bien:** `ClusterService.GetCluster()` ya manejaba este caso correctamente — `UserClusterCache.Get()` atrapa la excepción de desencriptado y devuelve `null`; `GetCluster()` entonces cae a la base de datos real (`repository.GetClusterConfiguration`), descifra con la versión correcta ahí guardada, y vuelve a cachear ya en el formato nuevo (`cache.Set(...)`).

**El bug encontrado:** `ClusterService.GetClusterContext()` (usado por `Ingress`, `Deployment`, `Secret`, `Network`, `ConfigMap` y `Namespace` para logs de auditoría) hacía primero `cache.Exists()` — que solo verifica que la clave exista en Redis, **sin validar que se pueda desencriptar**. Si la entrada existía pero era del formato viejo, entraba al branch de caché, `cache.Get()` fallaba silenciosamente y devolvía `null`, y el código devolvía los placeholders `"Unknown"`/`clusterId` **sin nunca consultar la base de datos**. Además, este método no repuebla el caché, así que el problema persistía hasta que la entrada expirara (TTL de 12h) o algo más disparara `GetCluster()` para ese mismo cluster.

**Fix:** `Servidor/KubeOrchestratorPlatform/Services/Cluster/ClusterService.cs` — `GetClusterContext()` ahora llama directo a `cache.Get()` (sin el `cache.Exists()` previo) y cae a `repository.GetClusterNames(...)` si devuelve `null`, igual que ya hacía `GetCluster()`. De paso, elimina un round-trip redundante a Redis.

---

## 10. Rate limiting en endpoints de autenticación

**Problema:** `AuthController.Login` (y `Register`) no tenían ningún límite de intentos, permitiendo ataques de fuerza bruta sin fricción.

**Fix:** se usó el rate limiting nativo de ASP.NET Core (`Microsoft.AspNetCore.RateLimiting`, sin dependencias nuevas):
- `Servidor/Program.cs`: política `"auth"` — fixed window, **5 requests/minuto por IP**, sin cola (`QueueLimit = 0`), responde `429 Too Many Requests` al excederse. Se agregó `app.UseRateLimiter()` al pipeline.
- `Servidor/KubeOrchestratorPlatform/Controllers/AuthController.cs`: `[EnableRateLimiting("auth")]` a nivel de clase, cubriendo `login`, `register`, y los nuevos `refresh`/`revoke` (ver sección 11).

**Verificación:** probado en runtime contra el contenedor real — a partir del 6to intento en la misma ventana de 1 minuto, responde `429`.

---

## 11. Refresh tokens y revocación de sesiones

**Problema:** el JWT emitido en login/registro duraba **7 días** y no existía forma de revocarlo antes de que expirara (no había noción de sesión en el servidor, solo verificación de firma). Un token filtrado seguía siendo válido hasta una semana.

**Fix — arquitectura nueva:**
- **`Servidor/KubeOrchestratorPlatform/Models/RefreshTokenModel.cs`** (nuevo): entidad `RefreshToken` con `TokenHash` (SHA-256 del valor, nunca se guarda el token crudo — mismo principio que el hash de contraseñas), `UserId`, `ExpiresAt`, `CreatedAt`, `RevokedAt`.
- **`AppDbContext.cs`**: `DbSet<RefreshToken>`, índice único en `TokenHash`, relación 1:N con `User` (cascade delete).
- **`AuthService.cs`**:
  - El access token JWT bajó de 7 días a **15 minutos** (`Jwt:AccessTokenExpirationMinutes`).
  - Se emite junto a un refresh token opaco de **7 días** (`Jwt:RefreshTokenExpirationDays`), generado con `RandomNumberGenerator.GetBytes(64)` y guardado hasheado en la BD.
  - `RefreshAsync(refreshToken)`: valida el token contra la BD (existe, no revocado, no expirado), **rota** el token (revoca el usado, emite uno nuevo) y devuelve un nuevo par de tokens.
  - `RevokeAsync(refreshToken)`: marca el token como revocado (logout). No revela si el token existía o no, para no filtrar información.
- **`AuthController.cs`**: nuevos endpoints `POST /api/auth/refresh` y `POST /api/auth/revoke`, bajo el mismo rate limit `"auth"`.
- **`appsettings.json`**: `Jwt:ExpireDays` reemplazado por `Jwt:AccessTokenExpirationMinutes` (15) y `Jwt:RefreshTokenExpirationDays` (7).
- **Migración `20260716182852_AddRefreshTokens`**: generada con `dotnet-ef` y aplicada automáticamente al arrancar (crea la tabla `RefreshTokens`).

**Verificación end-to-end contra los contenedores reales:**
1. Register/Login → devuelven `token` (15 min) + `refreshToken` (7 días).
2. Refresh con token válido → nuevo `token`/`refreshToken`, revoca el usado.
3. Reusar el refresh token viejo → `401`.
4. Revoke (logout) → el token queda revocado.
5. Usar un refresh token revocado → `401`.

**⚠️ Impacto para el frontend:** el cliente debe llamar a `POST /api/auth/refresh` con el `refreshToken` guardado antes de que el access token expire (cada ~15 min), o las sesiones se van a cortar sin aviso. Esto requiere cambios en el cliente que consume esta API.

---

## 12. Mapa de topología roto sin metrics-server

**Problema reportado por el usuario:** cuando el metrics-server no está activo/disponible en un cluster, todo el mapa de grafos deja de funcionar — independientemente de si los recursos mostrados dependen o no de métricas.

**Causa raíz:** en `GraphService.GetFullTopologyAsync`, la llamada a `metricsService.GetPodMetricsAsync(...)` se hacía **antes** de construir cualquier nodo del grafo y sin manejo de errores. `MetricsService.GetPodMetricsAsync` solo perdona el caso específico de `HttpOperationException` con status `404` (API `metrics.k8s.io` totalmente ausente) — pero no cubre escenarios igual de comunes cuando el metrics-server está registrado pero no responde bien (pod caído, TLS mal configurado con el kubelet, timeout de red), donde el servidor de API devuelve otros códigos (503/500) o incluso otro tipo de excepción. Esa excepción no atrapada se propagaba sin control hasta `GraphController` (que tampoco tiene try/catch) y tumbaba la respuesta completa del mapa.

**Fix:** `Servidor/KubeOrchestratorPlatform/Services/Graph/GraphService.cs` — se agregó `GetPodMetricsSafeAsync`, que envuelve la llamada real y, ante **cualquier** excepción, devuelve un diccionario vacío en vez de propagar el error. El resto del código ya estaba preparado para esto (`AddPodNode` ya usa `cpu ?? "0m"` / `mem ?? "0Mi"` como fallback), así que los pods simplemente quedan sin datos de CPU/Mem en vez de tumbar el mapa completo.

**Alcance de la verificación:** compila correctamente y el razonamiento sobre el flujo de código es sólido, pero no se pudo reproducir end-to-end contra un cluster real con metrics-server deshabilitado — no había `kind`/`minikube`/`kubectl` disponibles en el entorno de trabajo. Se recomienda probarlo contra un cluster real antes de darlo por cerrado del todo.

**Nota:** se dejó sin tocar el endpoint separado `ClustersController.GetMetrics` (`/api/clusters/{id}/metrics`) y `ClusterHubs.cs`, porque ahí el propósito completo del endpoint *es* mostrar métricas — que falle explícitamente con un mensaje claro tiene sentido y ya estaba bien manejado.

---

## Pendientes / recomendaciones para el futuro

Cosas que se identificaron pero **no** se implementaron en esta sesión:

- **Frontend:** adaptar el cliente para usar `POST /api/auth/refresh` antes de que el access token expire cada 15 minutos (ver sección 11).
- **DNS rebinding en `NetworkSafetyValidator`:** el validador resuelve el hostname una sola vez antes de conectar; en teoría un atacante podría cambiar la resolución DNS entre la validación y la conexión real (TOCTOU). No se abordó por ser un ataque más sofisticado y de menor probabilidad práctica en este contexto.
- **Rate limiting por usuario, no solo por IP:** hoy el límite de login es por IP; un atacante que rote de IP no queda cubierto.
- **Verificar en un cluster real** el fix de la sección 12 (metrics-server caído/mal configurado, no solo ausente).
- **Logging de `UserClusterCache`:** usa `Console.WriteLine` en vez de Serilog/`ILogger` para reportar errores de Redis — inconsistente con el resto del logging de la app.

## Archivos nuevos en esta sesión

- `Servidor/KubeOrchestratorPlatform/Models/RefreshTokenModel.cs`
- `Servidor/Migrations/20260716182852_AddRefreshTokens.cs` (+ `.Designer.cs`)
- `docs/2026-07-16-security-and-reliability-fixes.md` (este documento)
