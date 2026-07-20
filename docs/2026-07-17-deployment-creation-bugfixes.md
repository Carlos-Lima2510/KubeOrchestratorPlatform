# Bugs de creación de Deployments/Services y mensajes de error crudos de Kubernetes — 2026-07-17

Tres problemas encontrados al usar el formulario real de creación de deployments/services, y su fix.

## 1. `An item with the same key has already been added. Key: app`

**Causa:** `DeploymentService.BuildDeploymentObject` construía las labels del pod con el inicializador de colección de C#:

```csharp
var metadataLabels = new Dictionary<string, string>(dto.Labels)
{
    { "app", dto.Name },
    { "managed-by", "kube-orchestrator" }
};
```

El inicializador de colección llama a `.Add(key, value)` internamente, y `.Add()` **lanza excepción si la clave ya existe**. Como el diccionario arranca poblado con `dto.Labels` (las labels que el usuario escribe en el formulario), si el usuario agregaba su propia label `app` (muy común en Kubernetes), el `.Add("app", dto.Name)` posterior chocaba contra esa clave ya existente.

**Fix:** `Servidor/KubeOrchestratorPlatform/Services/Deployment/DeploymentService.cs` — se reemplazó por asignación por índice (`dict["app"] = ...`), que sobreescribe en vez de tirar excepción. Es además el comportamiento correcto: la label `app` debe ser siempre el nombre del deployment (para que coincida con el selector de pods), sin importar lo que el usuario haya escrito.

**Verificado:** reproducido el error exacto y confirmado el fix con un script C# aislado (lógica pura de `Dictionary`, no requiere cluster real).

## 2. `The string '' cannot be converted to fraction.`

**Causa:** `DeploymentService.ConvertToResourceQuantity` construía un `k8s.Models.ResourceQuantity` para cada entrada de `resources.requests`/`resources.limits`, sin filtrar valores vacíos. El formulario del frontend (`deployments/create/page.tsx`) tiene los campos "CPU Limit"/"Memory Limit" sin validación `required`, así que si el usuario los dejaba en blanco, se enviaba `{ cpu: "", memory: "" }` — y `new ResourceQuantity("")` no puede parsear un string vacío.

**Fix:** `ConvertToResourceQuantity` ahora omite (`continue`) cualquier entrada con valor vacío o solo espacios, en vez de intentar parsearla. Los recursos son opcionales por diseño (un usuario puede querer especificar solo CPU y no Memory, o ninguno), así que ignorarlos es el comportamiento correcto.

**Verificado:** mismo enfoque, script C# aislado referenciando el paquete `KubernetesClient` para instanciar `ResourceQuantity` directamente.

## 3. Mensajes de error crudos y técnicos de Kubernetes llegando al usuario

**Contexto:** al crear un Service con un `nodePort` ya usado, el usuario veía:
```
{"message": "Operation returned an invalid status code 'UnprocessableEntity', response body {\"kind\":\"Status\",...,\"message\":\"Service \\\"hgh\\\" is invalid: ... provided port is already allocated\",...}"}
```

**Causa:** el cliente de Kubernetes (`KubernetesClient`) lanza `k8s.Autorest.HttpOperationException` cuyo `.Message` es un texto técnico genérico seguido del **JSON crudo completo** de la respuesta del API server. La mayoría de los controladores atrapaban la excepción genéricamente y devolvían `ex.Message` tal cual, exponiendo ese JSON sin parsear.

**Fix:** nuevo helper `Servidor/KubeOrchestratorPlatform/Services/Errors/KubernetesErrorHelper.cs` — `GetFriendlyMessage(Exception ex)` detecta `HttpOperationException`, parsea `ex.Response.Content` como el `Status` estándar de la API de Kubernetes (`{ message, reason, details, code }`), y devuelve solo el campo `message` (el texto legible que Kubernetes ya arma). Si el body no es ese JSON esperado, cae de vuelta al `.Message` original sin romper nada.

Aplicado en los catches genéricos de: `NetworkSvcController`, `DeploymentController`, `IngressController`, `SecretsController`, `NamespacesController`, `PodController` (solo el catch genérico de `GetByDeployment`; el de logs ya tenía manejo especializado que no se tocó). También se completó `ConfigMapController`, que no tenía **ningún** manejo de errores en `Create`/`Delete` (cualquier excepción se propagaba como stack trace crudo) — ahora sigue el mismo patrón que `SecretsController`/`NetworkSvcController` (Conflict en 409, status code real + mensaje limpio en otros `HttpOperationException`, 500 genérico como último recurso).

De paso, en `NetworkSvcController`, `DeploymentController` y `NamespacesController` se agregó un catch específico de `HttpOperationException` que propaga el **status code real** de Kubernetes (422, 400, etc.) en vez de devolver siempre 500 para cualquier error de validación.

**Verificado:** parseo confirmado con el JSON exacto del error reportado (extrae correctamente `Service "hgh" is invalid: spec.ports[0].nodePort: Invalid value: 32200: provided port is already allocated`), build limpio, y sanity-check contra el backend real confirmando que no se rompió nada más.

## Nota

No se tocó el frontend para este fix — como ya se había unificado la clave de error a `message` en la sesión anterior, el frontend ya sabe leer `data.message` correctamente; ahora ese campo simplemente va a contener texto legible en vez del JSON crudo.
