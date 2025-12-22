# KubeOrchestrator Platform

![Status](https://img.shields.io/badge/Status-MVP-blue)
![License](https://img.shields.io/badge/License-MIT-green)

**KubeOrchestrator** es una plataforma centralizada para la gestión y monitoreo en tiempo real de múltiples clústeres de Kubernetes. Diseñada con una arquitectura moderna, permite a los desarrolladores visualizar métricas, gestionar cargas de trabajo (Workloads) y administrar redes sin exponer credenciales sensibles.

---

## 🚀 Características Principales

* **📡 Monitoreo en Tiempo Real:** Visualización de consumo de CPU y Memoria vía **SignalR (WebSockets)** con latencia mínima.
* **🔐 Gestión Segura de Credenciales:** Encriptación **AES-256** (Encryption at Rest) para tokens y certificados `kubeconfig`.
* **⚡ Rendimiento Optimizado:** Caché inteligente con **Redis** para acelerar la conexión a los clústeres.
* **🕹️ Orquestación de Workloads:**
    * Creación y gestión de **Deployments** y **Namespaces**.
    * Visualización de **Pods** y **Logs** en vivo.
    * Exposición de aplicaciones mediante **Services**.
* **🛡️ Autenticación Robusta:** Sistema de identidad con JWT y roles (Admin/User).

---

## 🛠️ Stack Tecnológico

### Backend (API & Core)
* **Framework:** .NET 8 (ASP.NET Core Web API)
* **Base de Datos:** MySQL 8.0 (Entity Framework Core)
* **Caché:** Redis (StackExchange.Redis)
* **Kubernetes:** KubernetesClient (SDK oficial)
* **Seguridad:** BCrypt & AES-256 Encryption Service

### Frontend (Client)
* **Framework:** Next.js 16 (App Router)
* **UI Library:** React 19 & Tailwind CSS v4
* **State Management:** Zustand & TanStack Query
* **Real-time:** @microsoft/signalr
* **Charts:** Recharts

---

## 📋 Prerrequisitos

Asegúrate de tener instalado lo siguiente antes de comenzar:

* [Docker Desktop](https://www.docker.com/products/docker-desktop/) (Para BD y Redis)
* [.NET SDK 8.0](https://dotnet.microsoft.com/download/dotnet/8.0)
* [Node.js 18+](https://nodejs.org/) (Recomendado v20 LTS)

---

## ⚙️ Instalación y Despliegue Local

Sigue estos pasos para levantar el entorno de desarrollo completo.

### 1. Clonar el repositorio
```bash
git clone https://github.com/tu-usuario/KubeOrchestratorPlatform.git
cd KubeOrchestratorPlatform
```

### 2. Levantar Infraestructura (Base de Datos y Caché)
Utilizamos Docker Compose para levantar MySQL y Redis sin ensuciar tu sistema local.

```bash
# Desde la raíz del proyecto (donde está el docker-compose.yml)
docker-compose up -d mysql_db redis_cache
```

### 3. Configurar y Ejecutar el Backend (.NET)

El backend es el núcleo de la plataforma. Necesita las claves de encriptación y la conexión a la base de datos para arrancar.

1.  Navega a la carpeta del servidor:
    ```bash
    cd Servidor
    ```
2.  **Importante:** Crea un archivo `.env` en esta carpeta (puedes copiar `.env.example`) y configura tus claves.
3.  Ejecuta la aplicación usando el perfil HTTP (puerto 5024):

    ```bash
    dotnet run --launch-profile http
    ```

> **Verificación:** Abre tu navegador en `http://localhost:5024/swagger`. Si ves la interfaz de Swagger UI, el backend está funcionando correctamente.
> *Nota: Asegúrate de que la variable `ASPNETCORE_ENVIRONMENT` sea `Development`.*

### 4. Configurar y Ejecutar el Frontend (Next.js)

El frontend es la interfaz visual construida con Next.js 16.

1.  Abre una nueva terminal y clona el repositorio:
    ```git
    git clone https://github.com/Carlos-Lima2510/KubeOrchestratorWeb.git
    ```
2. Navega a la carpeta web:
    ```bash
    cd kube-orchestrator-web
    ```
3.  Instala las dependencias del proyecto:
    ```bash
    npm install
    ```
4.  Inicia el servidor de desarrollo:
    ```bash
    npm run dev
    ```

> **Acceso:** La aplicación estará disponible en **http://localhost:3000**.

---

## 🔑 Variables de Entorno (.env)

El sistema requiere las siguientes variables. Para desarrollo local, crea un archivo `.env` dentro de la carpeta `Servidor/`.

```env
# --- Configuración General ---
ASPNETCORE_ENVIRONMENT=Development

# --- Base de Datos (MySQL) ---
MYSQL_DB_HOST=localhost
MYSQL_DB_PORT=3307
MYSQL_DB_NAME=KubeOrchestratorDb
MYSQL_DB_USER=root
MYSQL_DB_PASSWORD=password123

# --- Caché (Redis) ---
REDIS_HOST=localhost
REDIS_PORT=6380

# --- Seguridad (Encriptación AES-256) ---
# ¡IMPORTANTE! Genera claves nuevas en Base64 para producción.
# Key debe ser de 32 bytes (256 bits)
# IV debe ser de 16 bytes (128 bits)
Encryption__CurrentVersion=v1
Encryption__Keys__v1__Key=TU_KEY_GENERADA_EN_BASE64==
Encryption__Keys__v1__IV=TU_IV_GENERADO_EN_BASE64==
```
