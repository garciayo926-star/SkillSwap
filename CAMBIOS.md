# Registro de cambios — SkillSwap

---

## ACTUALIZACIÓN 2026-10-06 (3) — Revisión y completado de modelos

Se revisaron los modelos contra el spec de entidades. Resultado por modelo:

| Modelo (spec) | En el proyecto | Estado | Qué se agregó |
|---|---|---|---|
| **Rol** | `Role` + `UserRoles` | Correcto | Se mantiene la tabla de roles (ADMIN/MODERADOR/ESTUDIANTE). *Nota de diseño abajo.* |
| **Estudiante** | `User` + `Student` | Completado | **`Reputation`** (promedio de reseñas recibidas). El `estado ACTIVO/SUSPENDIDO` ya existía como `User.IsActive`. |
| **Habilidad** | `Skill` | Completado | Ya tenía **`Status`** (estado_aprobacion); se agregó **`SuggestedByStudentId`** (sugerido_por, opcional). |
| **Oferta** | `Offer` | Completado | **`Level`** (Básico/Intermedio/Avanzado) y **`Modality`** (Online/Presencial/Ambas). |
| **Solicitud** | `Request` | Completado | **`DesiredLevel`** (nivel_deseado). `comentarios` ya existía como `Notes`. |
| **Intercambio** | `Exchange` | Completado | **`SessionDate`** (fecha_sesion). Participantes y estados ya existían. |
| **Reseña** | **nuevo** `Review` | Creado | Entidad **1:1 con `Exchange`**: `ReviewerStudentId` (calificador), `RevieweeStudentId` (calificado), `Score` (1-5), `Comment`, `CreatedAt`. |

Migración `OfferLevelRequestLevelReputationReviews` (aplicada). Las filas existentes recibieron
valores por defecto sensatos (`Level`=Básico, `Modality`=Ambas, `DesiredLevel`=Básico, `Reputation`=0).
La migración anterior ya había dejado las habilidades existentes en `Status`=Aprobada.

### Lógica conectada
- Al **completar un intercambio con reseña** (`POST /api/Exchanges/{id}/review`) se crea/actualiza la
  entidad `Review`, se determina calificador/calificado desde el token, y se **recalcula la
  `Reputation`** del calificado como promedio de sus reseñas recibidas. (Probado: reputación = 5.)
- Al **sugerir una habilidad** un estudiante, se guarda `SuggestedByStudentId` y queda `Pendiente`.
- Ofertas/solicitudes devuelven y aceptan `Level`/`Modality`/`DesiredLevel` (probado).

### Nota de diseño sobre Rol (1:N vs N:M)
El spec plantea `rol_id` como FK directa en Estudiante (1:N). El proyecto usa una tabla intermedia
`UserRoles` (N:M) que **hoy funciona como rol único** por usuario y ya sostiene el JWT, el login y la
gestión de roles. Cambiarla a FK directa obligaría a reescribir autenticación y autorización sin
beneficio funcional, por lo que **se mantuvo**. El comportamiento (un rol por usuario) es el mismo.

---

---

## ACTUALIZACIÓN 2026-10-06 (2) — Controladores alineados al spec

Se **conservaron las rutas actuales** (el frontend sigue funcionando) y se **agregaron** las acciones
que faltaban del spec. Mapeo de nombres: Estudiantes=`Students`, Habilidades=`Skills`,
Ofertas=`Offers`, Solicitudes=`Requests`, Intercambios=`Exchanges`.

| # | Controlador (spec) | En el proyecto | Qué se agregó | Verificado |
|---|---|---|---|:-:|
| 1 | AuthController | `AuthController` | **GET `/api/Auth/me`** (usuario autenticado desde el token) | ✔ |
| 2 | EstudiantesController | `StudentsController` | **PATCH `/api/Students/{id}/status`** (activar/suspender) | ✔ |
| 3 | HabilidadesController | `SkillsController` | Campo **`Status`** (Pendiente/Aprobada/Rechazada); POST abierto a Estudiante (queda *Pendiente*); **PATCH `/api/Skills/{id}/review`** (aprobar/rechazar, Moderador/Admin); filtro `?status=` | ✔ |
| 4 | OfertasController | `OffersController` | Ya cubría POST/GET/PUT/DELETE con filtros | ✔ |
| 5 | SolicitudesController | `RequestsController` | Ya cubría POST/GET/PUT/DELETE con filtros | ✔ |
| 6 | MatchingController | **nuevo** `MatchingController` | **GET `/api/Matching/suggestions`** y **GET `/api/Matching/filter`** (por categoría/tipo). El `/api/Exchanges/matches/{id}` anterior se mantiene por compatibilidad | ✔ |
| 7 | IntercambiosController | `ExchangesController` | **PATCH `/api/Exchanges/{id}/status`** (alias REST) y **POST `/api/Exchanges/{id}/review`** (completar + calificar) | ✔ |
| 8 | Admin / ModeracionController | `AdminController` + **nuevo** `ReportsController` | Alias **GET `/api/Admin/dashboard`**; **POST `/api/Reports`**, **GET `/api/Reports`**, **GET `/api/Reports/{id}`**, **PATCH `/api/Reports/{id}/status`** | ✔ |

### Cambios de datos (migración `SkillStatusAndReports`, aplicada)
- `Skill.Status` (texto). Las habilidades **ya existentes quedaron "Aprobada"** para no romper nada.
- Nueva tabla **`Reports`** (reportes de moderación): quién reporta, usuario y/o intercambio reportado,
  motivo, descripción, estado (Abierto/EnRevision/Resuelto/Descartado) y resolución. FKs sin cascada.

### Flujo de aprobación de habilidades
- Un **Estudiante** que crea una habilidad la deja **Pendiente** (el servidor fija el estado según el
  rol del token; no se confía en el cliente). Un **Moderador/Administrador** la aprueba o rechaza con
  `PATCH /review`. Moderador/Admin que crean una habilidad la dejan directamente **Aprobada**.

### Pruebas realizadas (API en vivo, con limpieza posterior de los datos de prueba)
- `GET /api/Auth/me` devuelve el usuario del token.
- Estudiante sugiere habilidad → **Pendiente**; Admin `PATCH /review` → **Aprobada**.
- `GET /api/Matching/suggestions` → 200.
- `POST /api/Reports` (valida que apunte a usuario o intercambio), `GET /api/Reports` (Moderador/Admin),
  `PATCH /api/Reports/{id}/status` → Resuelto. Estudiante en `GET /api/Reports` → **403**.
- `GET /api/Admin/dashboard` y `/system-health` → 200.

> **Frontend:** intacto y compilando; todas las rutas previas se conservaron. Quedan como
> ampliación opcional (no incluida aún) las pantallas para **aprobar habilidades pendientes** y para
> **gestionar reportes**; el backend ya las soporta.

---

---

## ACTUALIZACIÓN 2026-10-06 — Roles, seguridad JWT y mensajes de login

### Roles: modelo final de 3 roles
Tras el análisis del proyecto, el sistema quedó con **exactamente 3 roles**:
**Estudiante** (usuario final), **Moderador** (gestión de comunidad y contenido) y
**Administrador** (superusuario con control total). Migración `RolesEstudianteModeradorAdmin`
(renombra 1→Administrador, 2→Moderador, 3→Estudiante y elimina el rol extra), aplicada.

Matriz de permisos aplicada **en el backend con JWT** y reflejada en el frontend:

| Acción | Estudiante | Moderador | Administrador |
|---|:-:|:-:|:-:|
| Dashboard con su propia actividad | ✔ | ✔ | ✔ |
| Dashboard analítico global (KPIs) | — | ✔ | ✔ |
| Desglose de usuarios por perfil | — | — | ✔ |
| Estado del sistema / API | — | — | ✔ |
| Pantalla de Usuarios (ver) | — | ✔ | ✔ |
| Crear cuentas | — | — | ✔ (cualquier rol) |
| Activar / suspender cuentas | — | ✔ | ✔ |
| Editar usuarios | — | ✔ | ✔ |
| Eliminar usuarios | — | — | ✔ |
| Cambiar rol | — | — | ✔ |
| Restablecer contraseña | — | — | ✔ |
| Validar/crear/editar/eliminar habilidades del catálogo | — | ✔ | ✔ |
| Ofertas, solicitudes, coincidencias, intercambios | ✔ (lo suyo) | ✔ | ✔ |

- **Estudiante:** usuario final. Gestiona su perfil, ofertas, solicitudes, coincidencias e
  intercambios. Su dashboard muestra **solo su propia actividad** (no ve métricas globales ni
  el desglose de usuarios).
- **Moderador:** cuida la calidad del catálogo (validar/editar/eliminar habilidades), supervisa
  perfiles y ofertas, y puede **activar o suspender** cuentas. Ve el dashboard analítico, pero
  no el desglose por perfil ni el estado del sistema.
- **Administrador:** superusuario. Control total: gestión de usuarios (crear con cualquier rol,
  editar, eliminar, cambiar rol, restablecer contraseña, activar/suspender), catálogo, auditoría
  y estado del sistema.

> Nota: las funciones de "reportes" y "aprobación formal de habilidades" descritas para el
> Moderador se cubren con los permisos de moderación existentes (editar/eliminar habilidades y
> suspender cuentas). No existe aún una entidad de *reportes* en el modelo de datos; agregarla
> sería una ampliación futura.

### Seguridad: autenticación real con JWT
- Paquete `Microsoft.AspNetCore.Authentication.JwtBearer`. Configuración en `appsettings.json` → sección `Jwt`.
- `Services/TokenService.cs` emite un token firmado (HMAC-SHA256) con el rol como *claim*.
- `AuthController.Login` devuelve `token` y `expiresAt`.
- `Program.cs`: `AddAuthentication(JwtBearer)` + `UseAuthentication/UseAuthorization`.
- Todos los controladores llevan `[Authorize]`; las acciones sensibles usan `[Authorize(Roles = "...")]`.
- **Frontend:** interceptor `interceptors/auth-interceptor.ts` adjunta el `Bearer` token en cada
  petición y, ante un 401, cierra la sesión y regresa al login. El token se guarda en `localStorage`.

Pruebas realizadas (API en vivo):
- Endpoint protegido sin token → **401**.
- Login incorrecto → **401** `"Correo o contraseña incorrectos."`; campos vacíos → **400**.
- Login correcto → token válido con `claim` de rol; `GET /api/Skills` con token → **200**.
- Token de Estudiante en `/api/Admin/analytics` y `/api/Students` → **403** (prohibido por rol).

### Login: validación y mensaje de error
- Validación del lado del cliente (correo/contraseña vacíos y formato de correo).
- Mensaje de error visible (ya con *signals*): credenciales incorrectas, cuenta desactivada
  o API apagada (`status 0`). El backend devuelve un mensaje **genérico** a propósito
  (no revela si falló el correo o la contraseña) por buena práctica de seguridad.

### Gestión de cuentas
Nuevos endpoints en `AdminController`:
`POST /api/Admin/users` (crear, **Administrador**), `PUT /api/Admin/users/{id}/role` (**Administrador**),
`PUT /api/Admin/users/{id}/status` (activar/suspender, **Administrador y Moderador**),
`PUT /api/Admin/users/{id}/password` (restablecer, **Administrador**).
> Las contraseñas se mantienen **cifradas con hash** (no se pueden mostrar en texto, es lo correcto).
> El Administrador puede **restablecer** la contraseña de un usuario, pero nunca ve la original.

### Para asignar el rol Administrador a tu usuario
`luis@mail.com` no tiene rol (entra como Estudiante). Ejecuta una vez en PostgreSQL y vuelve a entrar
(el `RoleId = 1` corresponde ahora a **Administrador**):
```sql
INSERT INTO "UserRoles" ("UserId", "RoleId")
SELECT "Id", 1 FROM "Users" WHERE "Email" = 'luis@mail.com';
```

---

**Fecha:** 2026-10-02
**Alcance:** corrección de los problemas detectados en el análisis y desarrollo de lo que faltaba para cumplir el enunciado:

> Un estudiante puede ofrecer algo que sabe hacer y solicitar algo que quiere aprender. El sistema intenta encontrar coincidencias.
> CRUD: Estudiantes, Habilidades, Ofertas, Solicitudes, Intercambios, con filtros y relaciones entre datos.

Se respetó la estructura que ya tenía el proyecto:
- Backend: controladores con `SkillSwapDbContext` inyectado, proyecciones anónimas y mensajes `{ message }` en español.
- Frontend: componentes standalone en `components/<nombre>/<nombre>.ts|html|css`, servicios en `services/<nombre>.ts` con `baseUrl` propio, inyección por constructor, `*ngIf`/`*ngFor` y `FormsModule`.

---

## 1. Cómo ejecutar

```bash
# Backend (puerto 5066)
cd backend/SkillSwap.API
dotnet run --launch-profile http

# Frontend (puerto 4200)
cd frontend/SkillSwap-web
npm install        # solo en una copia nueva del proyecto; no se agregaron dependencias nuevas
npm start
```

Las migraciones **ya se aplicaron** a la base local `SkillSwap`. En otra máquina hay que ejecutar `dotnet ef database update`.

**Para tener un usuario Master** (la gestión de roles requiere uno), ejecuta una vez en PostgreSQL:

```sql
INSERT INTO "UserRoles" ("UserId", "RoleId")
SELECT "Id", 1 FROM "Users" WHERE "Email" = 'luis@mail.com';
```

Después cierra sesión y vuelve a entrar. Desde ese momento puedes cambiar los roles en la pantalla **Estudiantes**.

---

## 2. Causa de la "pantalla en blanco" / datos que no aparecían

El proyecto estaba en modo **zoneless** (sin `zone.js`): nada disparaba la detección de cambios de Angular al terminar una petición HTTP, así que la vista no se redibujaba aunque los datos llegaran. Lo comprobé en el navegador: el componente recibía los datos (`loading: false`, 1 oferta), pero la pantalla seguía en "Cargando...". Por lo mismo, el mensaje de error del login tampoco se mostraba.

**Solución aplicada (sin dependencias nuevas): signals.**
- `app.config.ts` usa `provideZonelessChangeDetection()` (modo zoneless oficial de Angular 22, no requiere `zone.js`).
- El estado que llega del backend de forma asíncrona se declara como `signal(...)` (listas, `loading`, `errorMessage`, `successMessage`, `analytics`, etc.). Al hacer `.set(...)`, la vista se actualiza sola.
- Los indicadores derivados del dashboard usan `computed(...)`.
- Los campos de formulario con `[(ngModel)]` siguen siendo propiedades normales: se actualizan con los eventos del usuario.
- Todos los componentes usan `changeDetection: ChangeDetectionStrategy.OnPush` (lo idiomático con signals).

Así se arregla también el mensaje de error del login, que antes no aparecía.

> Nota: en una versión intermedia esto se resolvió agregando `zone.js`, pero se cambió a **signals** para no añadir ninguna dependencia al proyecto.

Además, el error **"token must be defined"** de la captura venía de la recarga en caliente (HMR) al cambiar el constructor con `ng serve` encendido. Con el código actual no se reproduce.

---

## 3. Backend (.NET 10 + EF Core + PostgreSQL)

### 3.1 Correcciones

| Problema | Solución | Archivo |
|---|---|---|
| La migración `AddRolesSeed` estaba **pendiente** porque PostgreSQL no convierte `text` → `timestamp` sin `USING`. Por eso la tabla `Roles` estaba vacía. | Se reescribió la conversión con `USING` y se aplicó. | `Migrations/20261002182433_AddRolesSeed.cs` |
| Referencias circulares al serializar entidades (Skill ↔ Offer ↔ Student) en los GET por id. | Todos los GET devuelven **proyecciones**. Además se agregó `ReferenceHandler.IgnoreCycles` como respaldo. | Controladores, `Program.cs` |
| Los GET por id exponían `PasswordHash`. | Las proyecciones no incluyen datos sensibles. | Controladores |
| Contraseñas guardadas en **texto plano**. | Hash con `PasswordHasher<User>` (incluido en ASP.NET Core, sin paquetes nuevos). Las cuentas antiguas se migran a hash en su primer login. | `AuthController.cs`, `StudentsController.cs` |
| `AdminController` tenía `[Authorize]` sin esquema de autenticación configurado (error 500) y devolvía números fijos. | Devuelve datos reales de la BD. Se retiraron los `[Authorize]` (ver pendientes: JWT). | `AdminController.cs` |
| `oCategory` en la respuesta de Requests. | Renombrado a `category`. | `RequestsController.cs` |
| Los `PUT` no validaban la existencia ni las llaves foráneas. | Validación de existencia, de llaves foráneas y de duplicados. | Offers, Requests, Exchanges, Skills |
| Se podía borrar una habilidad en uso (con borrado en cascada de ofertas y solicitudes). | Se bloquea con un mensaje claro. | `SkillsController.cs` |
| Los usuarios sin rol no aparecían en las estadísticas. | Se cuentan como `Student`, el mismo criterio que usa el login. | `AdminController.cs` |

### 3.2 Modelo y base de datos

- **`Exchange`** ahora registra qué se intercambia: `OfferedSkillId` (lo que enseña el iniciador) y `RequestedSkillId` (lo que aprende), ambos opcionales y relacionados con `Skill` (`SetNull` al borrar).
- **Índices únicos** `(StudentId, SkillId)` en `Offers` y `Requests`: un estudiante no puede ofrecer ni solicitar dos veces la misma habilidad.
- Nueva migración: `20261003021154_ExchangeSkillsAndUniqueIndexes` (aplicada).
- Antes de migrar se sacó un respaldo con `pg_dump`. Los datos existentes se conservaron: 1 usuario, 2 habilidades, 1 oferta y 1 solicitud.

### 3.3 Nuevo: `StudentsController` (CRUD de Estudiantes, que faltaba)

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/Students?name=&skillName=` | Lista con filtros por nombre/usuario o por habilidad ofrecida/solicitada. |
| GET | `/api/Students/{id}` | Perfil con ofertas, solicitudes y conteo de intercambios por estado. |
| POST | `/api/Students` | Crea usuario + perfil de estudiante + rol Student. |
| PUT | `/api/Students/{id}` | Actualiza datos, biografía y estado activo. |
| DELETE | `/api/Students/{id}` | Elimina el estudiante y su usuario. Se bloquea si tiene intercambios (en ese caso se sugiere desactivarlo). |

### 3.4 Endpoints ampliados

| Controlador | Novedades |
|---|---|
| **Skills** | Filtro `search`, conteo de ofertas y solicitudes activas, `GET /api/Skills/categories`, validación de nombre duplicado. |
| **Offers / Requests** | Filtros `studentId` e `includeInactive`. No se permite **solicitar lo que ya ofreces** ni **ofrecer lo que ya solicitas**. |
| **Exchanges** | Filtros `studentId` y `status`. Habilidades del intercambio. **Flujo de estados** con `PUT /api/Exchanges/{id}/status`: `Pendiente → Aceptado / Rechazado / Cancelado`, `Aceptado → Completado / Cancelado`. Calificación de 1 a 5 y comentario al completar. No se permiten intercambios duplicados en curso. |
| **Matches** | `GET /api/Exchanges/matches/{id}?includePartial=true`. Por defecto devuelve solo coincidencias **mutuas** (la lógica original). Con `includePartial` también devuelve las de una sola dirección, como el ejemplo del enunciado: Juan quiere Photoshop de María, pero María no busca Python. Devuelve `skillId` para poder proponer el intercambio. |
| **Admin** | `analytics` y `system-health` con datos reales, `GET /api/Admin/users`, `PUT /api/Admin/users/{userId}/role`. |

---

## 4. Frontend (Angular 22)

### 4.1 Correcciones

| Problema | Solución | Archivo |
|---|---|---|
| Vista sin refrescar / pantalla en blanco | **signals** + `provideZonelessChangeDetection()` + `OnPush` (ver sección 2). Sin dependencias nuevas. | `app.config.ts`, todos los componentes |
| `localStorage` fallaba al pre-renderizar en el servidor | Solo `/login` se pre-renderiza. El resto usa `RenderMode.Client`. | `app.routes.server.ts` |
| El guard dejaba entrar sin sesión (`getUserRole()` devolvía `'Student'` por defecto) | Devuelve `''` sin sesión. El guard usa `isLoggedIn()` y redirige con `UrlTree`. | `services/auth.ts`, `guards/guard.ts` |
| El login solo navegaba si existía `studentId` (bloqueaba a Master y Technical) | Navega cuando existe `userId`. Se quitó el guardado duplicado en `localStorage`. Si ya hay sesión, va directo al dashboard. | `components/login/login.ts` |
| El mensaje "Registro exitoso" nunca se mostraba (`toggleMode` lo borraba) | Se asigna después de cambiar de pestaña y se rellena el correo. | `components/login/login.ts` |
| `HttpClient` sin configurar para SSR | `provideHttpClient(withFetch())`. | `app.config.ts` |
| El dashboard tenía números fijos | Usa `/api/Admin/analytics`, `/system-health` y el perfil del estudiante. Se mantuvo el diseño y las clases originales. | `components/dashboard/*` |
| Tests rotos (importaban `App`, `Login`, `Dashboard`, `Auth`) | Corregidos con los nombres reales y los providers necesarios. | `*.spec.ts` |
| `ng build` de producción fallaba (`login.css` de 13 kB superaba el límite de 8 kB) | Presupuesto de estilos: 8 kB (aviso) / 16 kB (error). Bundle inicial: aviso en 600 kB. | `angular.json` |

### 4.2 Servicios nuevos (`src/app/services/`)

`student.ts`, `skill.ts`, `offer.ts`, `request.ts`, `exchange.ts`, `admin.ts`. Cada uno incluye interfaces tipadas.
A `auth.ts` se agregaron `getUsername()`, `getUserId()`, `getStudentId()`, `isLoggedIn()`, `isMaster()` e `isAdmin()`. Todos son seguros durante el renderizado en servidor.

### 4.3 Componentes nuevos (`src/app/components/`)

| Componente | Ruta | Qué hace |
|---|---|---|
| `layout` | (contenedor) | Menú lateral con enlaces según el rol, usuario y cerrar sesión. Envuelve todas las páginas privadas. |
| `students` | `/students` | CRUD de estudiantes con filtros. El Master puede cambiar el rol de cada usuario. |
| `skills` | `/skills` | CRUD de habilidades con filtros por nombre y categoría, y conteo de ofertas/solicitudes. |
| `offers` | `/offers` | CRUD de ofertas. Filtros por habilidad, categoría, estudiante e inactivas. "Ver solo mis ofertas". |
| `requests` | `/requests` | CRUD de solicitudes (misma estructura que ofertas). |
| `matches` | `/matches` | Motor de coincidencias: mutuas o parciales, y **proponer intercambio** eligiendo qué enseñas y qué aprendes. |
| `exchanges` | `/exchanges` | Seguimiento de intercambios: aceptar, rechazar, cancelar, completar con calificación ★ y comentario. |

Los estilos compartidos (prefijo `ss-`) están en `src/styles.css` y usan la paleta del dashboard.

### 4.4 Permisos por rol (aplicados en el frontend)

| Acción | Student | Technical | Master |
|---|:-:|:-:|:-:|
| Dashboard, Habilidades (ver/crear), Coincidencias, Intercambios | ✔ | ✔ | ✔ |
| Editar/eliminar habilidades | — | ✔ | ✔ |
| Ofertas y solicitudes | Solo las propias | Todas | Todas |
| Aceptar/rechazar intercambios | Si es el receptor | ✔ | ✔ |
| Pantalla Estudiantes | — | ✔ | ✔ |
| Cambiar roles | — | — | ✔ |

---

## 5. Verificación realizada

- `dotnet build`: sin errores ni advertencias.
- Prueba completa de la API con el ejemplo del enunciado (María/Juan, Photoshop/Python/Excel): creación, duplicados, coincidencia parcial → mutua, propuesta, transiciones de estado inválidas y válidas, calificación, bloqueos de borrado y login con hash. Los datos de prueba se borraron al final y la BD quedó como estaba.
- `ng build` de producción: sin errores ni avisos.
- `ng test`: **18 archivos, 22 tests aprobados** (incluye pruebas del guard por rol).
- Navegador real (Chrome headless): todas las pantallas muestran datos y no hay errores en consola. Un Student es redirigido desde `/students` y sin sesión se vuelve a `/login`.

---

## 6. Pendientes y recomendaciones

1. **El frontend no está versionado en git.** `frontend/SkillSwap-web` figura como submódulo (entrada `160000`), pero su carpeta `.git` ya no existe, así que git ignora todos sus archivos (login, dashboard y lo nuevo). Para incluirlo en el repositorio principal:
   ```bash
   git rm --cached frontend/SkillSwap-web
   git add frontend/SkillSwap-web
   ```
2. **Quitar `bin/` y `obj/` del repositorio.** El nuevo `.gitignore` evita que se agreguen otra vez, pero los archivos ya rastreados siguen ahí:
   ```bash
   git rm -r --cached backend/SkillSwap.API/bin backend/SkillSwap.API/obj
   ```
3. **Autenticación real (JWT).** Hoy el rol se guarda en `localStorage` y la API no valida quién llama; el control de acceso es solo del frontend. El siguiente paso es agregar `Microsoft.AspNetCore.Authentication.JwtBearer`, emitir el token en el login y volver a poner `[Authorize(Roles = ...)]`.
4. **Credenciales en `appsettings.json`.** Mover la contraseña de PostgreSQL a *user secrets* o a variables de entorno.
5. **Fechas como texto.** `User.CreatedAt`, `User.UpdatedAt` y `Announcement.DatePosted` son `string`. Conviene migrarlas a `DateTime` (con `USING`, igual que en `AddRolesSeed`).
6. **Modelos sin uso.** `Teacher` y `Announcement` existen en la BD, pero todavía no tienen controladores ni pantallas.
7. **Sintaxis de plantillas.** Angular 22 marca `*ngIf` y `*ngFor` como obsoletos (son solo avisos). Se mantuvieron para seguir el estilo del proyecto; más adelante se pueden migrar a `@if` / `@for` con `ng generate @angular/core:control-flow`.
