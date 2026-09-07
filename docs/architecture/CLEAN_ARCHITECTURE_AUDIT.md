# Clean Architecture Audit

## 1. Executive Summary

Estado general: `MEJORABLE`

Conclusiones principales:

- La solucion compila en el estado actual y la direccion general de dependencias entre proyectos es razonable.
- La arquitectura real no es una Clean Architecture completa; es una solucion de 4 proyectos con capas nominales correctas, pero con varias responsabilidades mezcladas dentro de `Testing` y `Testing.Application`.
- El mayor problema no es `ProjectReference`, sino que `ConsultaViajes.razor` concentra UI, coordinacion, filtros, agregaciones, armado jerarquico y parte importante del procesamiento de datos.
- `Testing.Application` contiene logica util del caso de uso, pero tambien detalles de persistencia (`SQL`, `[Column]`) y detalles de presentacion/exportacion (`Word`, slides), lo que debilita el limite Application/Infrastructure/Presentation.
- `Testing.Domain` es practicamente anemico: no contiene entidades ni reglas de negocio del dominio; hoy solo contiene `Result` y `Error`.
- No existen proyectos ni archivos de prueba. Cualquier refactorizacion de `ConsultaViajes`, de reglas derivadas desde `ruta/tipo_operacion/fecha_ingreso`, o del acceso SQL debe considerarse de riesgo alto hasta crear tests.

Calificacion global:

- Presentation / Blazor: `INCORRECTO`
- Application: `MEJORABLE`
- Infrastructure: `MEJORABLE`
- Domain: `ACEPTABLE`

## 2. Estado actual del repositorio

Ruta analizada: `C:\Proyects\TestingBlazor\TestingRepository`

Mapa base:

- Solucion: `Testing.slnx`
- Proyectos: `Testing`, `Testing.Application`, `Testing.Domain`, `Testing.Infrastructure`
- Archivos relevantes no generados analizados: `86`
- Archivos `.cs`: `30`
- Archivos `.razor`: `31`
- Archivos `.css`: `11`
- Archivos `.js`: `2`
- Archivos `.json`: `3`
- Proyectos de test: `0`

Git:

- Rama actual: `Feat-Implement-New-Interface`
- Ultimos commits:
  - `536731d` - `2026-09-01` - `Update`
  - `25d5460` - `2026-08-31` - `Add executive summary and hierarchy views`
  - `4c98693` - `2026-08-31` - `Add pivot view, KPIs, and ruta parsing`
  - `c1ed26b` - `2026-08-28` - `Redesign viajes dashboard and filters`
  - `5fed1b0` - `2026-08-28` - `Update Files`
  - `6c01eb8` - `2026-08-28` - `Initial commit`

Working tree al 2026-09-01:

- `20` archivos modificados
- `8` archivos sin seguimiento
- Los cambios recientes se concentran casi por completo en la funcionalidad `ConsultaViajes` / `Resumen Ejecutivo`

Baseline de build:

- `dotnet restore Testing.slnx`: correcto
- `dotnet build Testing.slnx`: correcto, `0` warnings, `0` errores
- `dotnet test Testing.slnx`: sin proyectos de prueba ejecutables en la solucion

## 3. Arquitectura encontrada

| Proyecto | Responsabilidad esperada | Responsabilidad actual | Dependencias actuales | Dependencias correctas | Problemas | Cumplimiento |
| --- | --- | --- | --- | --- | --- | --- |
| `Testing.Domain` | Conceptos de negocio puros | Solo `Result` y `Error` | Ninguna | Ninguna | Dominio anemico; tipos genericos de resultado mas cercanos a Application que a Domain | `ACEPTABLE` |
| `Testing.Application` | Casos de uso, DTOs, validacion, abstracciones | Query `GetViajes`, validacion, muchas calculadoras, parseo de campos, modelos de resumen, slides y exportador Word | `Testing.Domain` | `Testing.Domain` | Conoce SQL, atributos de mapeo, y detalles de presentacion/exportacion | `MEJORABLE` |
| `Testing.Infrastructure` | Persistencia y detalles tecnicos | DI, `DbContextFactory`, adaptador `IApplicationDbContext`, `ZemogContext` scaffolded y entidad `TraficoCombustible` | `Testing.Application`, `Testing.Domain` | `Testing.Application`, `Testing.Domain` | Infraestructura sobredimensionada para el flujo real; `DbContext` grande casi sin uso funcional | `MEJORABLE` |
| `Testing` | UI Blazor, layouts, componentes, composicion root | UI + parte importante de filtros, agregacion, pivote, arbol, resumen visual y exportacion | `Testing.Application`, `Testing.Infrastructure` | `Testing.Application` y composicion root hacia `Infrastructure` | `ConsultaViajes.razor` y algunos componentes tienen demasiada logica | `INCORRECTO` |

## 4. Dependency Graph

Dependencias de proyecto reales:

```text
Testing
  -> Testing.Application
  -> Testing.Infrastructure

Testing.Infrastructure
  -> Testing.Application
  -> Testing.Domain

Testing.Application
  -> Testing.Domain

Testing.Domain
  -> (ninguna)
```

Flujo real del caso `ConsultaViajes`:

```text
ConsultaViajes.razor
  -> ISender.Send(GetViajesQuery)
  -> GetViajesQueryHandler
  -> IApplicationDbContext.QueryAsync(sql, params)
  -> ApplicationDbContext
  -> ZemogContext.Database.SqlQueryRaw
  -> SP [Operaciones].[sp_ConsultaViajesZemog]
  -> List<ViajesDto>
  -> filtros / KPIs / pivote / arbol / resumen
  -> render de componentes Blazor
```

## 5. Violaciones de Clean Architecture

### ARCH-001

- Archivo: `Testing.Application/GetAllViajes/GetViajesQueryHandler.cs`
- Lineas: `12-32`
- Categoria: Clean Architecture / Application / Persistence
- Severidad: `P1`
- Riesgo: `ALTO`
- Problema: el handler de Application conoce el nombre del stored procedure, la forma del SQL y los parametros tecnicos.
- Codigo actual: `GetViajesQueryHandler` arma `SpConsultaViajes`, `QueryParameter[]` y llama `QueryAsync(sql, params)`.
- Por que es un problema: Application deja de depender de una abstraccion de negocio y pasa a depender del detalle de acceso a datos.
- Cambio recomendado: introducir una abstraccion de lectura especifica del caso de uso, por ejemplo `IConsultaViajesReadService` o `IViajesQueryService`, y mover el SQL a Infrastructure.
- Destino sugerido: interfaz en `Testing.Application/Abstractions`, implementacion en `Testing.Infrastructure/Persistence/Viajes`.
- Dependencias afectadas: `Testing.Application`, `Testing.Infrastructure`.
- Tests requeridos: integration test del mapping de SP a DTO, test del handler con fake service.
- Cambio funcional esperado: `NINGUNO`

### ARCH-002

- Archivo: `Testing.Application/GetAllViajes/ViajesDto.cs`
- Lineas: `1-8`
- Categoria: Clean Architecture / DTO / EF Core
- Severidad: `P1`
- Riesgo: `MEDIO`
- Problema: `ViajesDto` tiene `[Column("base")]`, un detalle tecnico de persistencia dentro de Application.
- Codigo actual: DTO de aplicacion con atributo de mapeo para resolver una columna reservada.
- Por que es un problema: Application queda atado a convenciones del proveedor y del mecanismo de materializacion.
- Cambio recomendado: separar un modelo de lectura infraestructural o resolver el alias en la consulta/SP para que el DTO de Application no necesite atributos EF.
- Destino sugerido: row model en `Testing.Infrastructure/Persistence/Viajes` o alias desde el SP.
- Dependencias afectadas: `Testing.Application`, `Testing.Infrastructure`.
- Tests requeridos: integration test del mapping.
- Cambio funcional esperado: `NINGUNO`

### ARCH-003

- Archivo: `Testing/Components/Pages/ConsultaViajes.razor`
- Lineas: `224-719`
- Categoria: Blazor / SRP / Clean Architecture
- Severidad: `P0`
- Riesgo: `CRITICO`
- Problema: el componente mezcla rendering, estado de UI, temas, filtros, filtrado en memoria, calculo de KPIs, armado de pivote, armado de arbol jerarquico y manejo de errores.
- Codigo actual: `RecalcularCascada`, `ObtenerViajesFiltrados`, `RecalcularPivote`, `RecalcularArbol`, `CalcularMetricas`, `CalcularKpis`, `TextoCorte`, mas el dispatch del query.
- Por que es un problema: rompe SRP, dificulta testear la logica sin UI, y convierte el componente principal en el centro del negocio.
- Cambio recomendado: primero mover a `ConsultaViajes.razor.cs`; despues extraer procesamiento a clases de Application o a procesadores feature-specific sin cambiar HTML/CSS.
- Destino sugerido: `Testing/Components/Pages/ConsultaViajes.razor.cs` + `Testing.Application/Features/Viajes/Processing`.
- Dependencias afectadas: `Testing`, `Testing.Application`.
- Tests requeridos: unit tests para filtros, KPIs, pivote, arbol, corte mensual; component tests para estados de UI.
- Cambio funcional esperado: `NINGUNO`

### ARCH-004

- Archivo: `Testing/Components/Pages/ConsultaViajes.razor`
- Lineas: `396-616`
- Categoria: Application logic in UI / Performance / Testability
- Severidad: `P1`
- Riesgo: `ALTO`
- Problema: la cascada de filtros y la agregacion para pivote/arbol viven en UI y recalculan colecciones completas repetidamente.
- Codigo actual: multiples `.Where(...).ToList()`, `GroupBy`, `Aggregate`, `DistinctBy` sobre `viajesCargados` y `viajesFiltrados`.
- Por que es un problema: no genera mas viajes a BD, pero si acopla la logica al componente y vuelve costosa la evolucion del feature.
- Cambio recomendado: encapsular el pipeline de procesamiento en un servicio/clase de Application que reciba viajes y estado de filtros.
- Destino sugerido: `Testing.Application/Features/Viajes/Processing/ConsultaViajesDataProcessor.cs`
- Dependencias afectadas: `Testing`, `Testing.Application`.
- Tests requeridos: unit tests de cascada, agrupaciones, comparativas y participacion.
- Cambio funcional esperado: `NINGUNO`

### ARCH-005

- Archivo: `Testing.Application/GetAllViajes/ResumenEjecutivoCalculator.cs`
- Lineas: `17-484`
- Categoria: Application / SOLID / Maintainability
- Severidad: `P1`
- Riesgo: `ALTO`
- Problema: una sola clase calcula meses cerrados, bloque de nivel, arbol comparativo, asignacion, destinos cayendo, agencias desaparecidas y semaforo.
- Codigo actual: clase estatica de casi `400` lineas con varios subdominios operativos.
- Por que es un problema: es un "god calculator"; cualquier cambio en una parte obliga a tocar una unidad demasiado grande y sin proteccion de pruebas.
- Cambio recomendado: dividir por subcaso (`MesesCerrados`, `BloqueNivel`, `ArbolComparativo`, `Semaforo`, `Destinos`, `Agencias`).
- Destino sugerido: `Testing.Application/Features/Viajes/ResumenEjecutivo/*`
- Dependencias afectadas: `Testing.Application`, componentes de resumen.
- Tests requeridos: unit tests por subcalculadora.
- Cambio funcional esperado: `NINGUNO`

### ARCH-006

- Archivo: `Testing.Application/GetAllViajes/ResumenEjecutivoWordExporter.cs`
- Lineas: `24-230`
- Categoria: Clean Architecture / Export / Presentation detail
- Severidad: `P1`
- Riesgo: `MEDIO`
- Problema: Application genera HTML especifico para Word, con MIME, estilos inline y formato documental.
- Codigo actual: exporter que escribe markup y tablas HTML completas.
- Por que es un problema: esto es detalle de entrega/presentacion, no regla de negocio.
- Cambio recomendado: mover el exportador a Infrastructure o a una capa de Presentation service y consumirlo via interfaz si la exportacion es caso de uso formal.
- Destino sugerido: `Testing.Infrastructure/Documents/ResumenEjecutivoWordExporter.cs`
- Dependencias afectadas: `Testing.Application`, `Testing.Infrastructure`, `Testing`.
- Tests requeridos: golden master test del HTML generado.
- Cambio funcional esperado: `NINGUNO`

### ARCH-007

- Archivo: `Testing.Application/GetAllViajes/PresentacionModels.cs` y `SlidesPresentacionCalculator.cs`
- Lineas: `PresentacionModels 3-110`, `SlidesPresentacionCalculator 14-92`
- Categoria: Boundary leak / Presentation semantics in Application
- Severidad: `P2`
- Riesgo: `MEDIO`
- Problema: Application conoce numero de slides, pasos de revelado y textos de una vista especifica.
- Codigo actual: DTOs con `PasosRevelado`, `TotalSlides` y textos fijos para overlay.
- Por que es un problema: acerca Application a un contrato visual especifico.
- Cambio recomendado: mantener el armado de datos en Application si se desea, pero mover metadata de navegacion/presentacion al proyecto `Testing`.
- Destino sugerido: `Testing/Features/ResumenPresentacion`.
- Dependencias afectadas: `Testing.Application`, `Testing`.
- Tests requeridos: component tests de navegacion de slides.
- Cambio funcional esperado: `NINGUNO`

### ARCH-008

- Archivo: `Testing.Infrastructure/Persistence/Zam/ZemogContext.cs` y `TraficoCombustible.cs`
- Lineas: `ZemogContext 7-374`, `TraficoCombustible 6-128`
- Categoria: Infrastructure / Dead code / EF Core
- Severidad: `P2`
- Riesgo: `MEDIO`
- Problema: el feature auditado usa `Database.SqlQueryRaw` como host, pero el modelo scaffolded `TraficoCombustible` no participa en el flujo funcional.
- Codigo actual: `DbSet<TraficoCombustible>` y mapeo extenso no referenciados fuera del contexto.
- Por que es un problema: agrega peso cognitivo y costo de mantenimiento a Infrastructure.
- Cambio recomendado: aislar el scaffold en una carpeta explicita de legado o reemplazar el contexto por uno minimo si solo se necesita ejecutar consultas SQL.
- Destino sugerido: `Testing.Infrastructure/Persistence/Legacy` o `Testing.Infrastructure/Persistence/Viajes/ZemogSqlContext.cs`
- Dependencias afectadas: `Testing.Infrastructure`.
- Tests requeridos: integration test de ejecucion del query antes de tocar el contexto.
- Cambio funcional esperado: `NINGUNO`

### ARCH-009

- Archivo: `Testing.Domain/Common/Result.cs` y `Error.cs`
- Lineas: `Result 1-31`, `Error 1-10`
- Categoria: Domain / Modeling
- Severidad: `P2`
- Riesgo: `BAJO`
- Problema: el proyecto Domain no contiene conceptos del negocio; contiene utilidades genericas de resultado.
- Codigo actual: `Result`, `Result<T>`, `Error`.
- Por que es un problema: el nombre de la capa promete un dominio que hoy no existe, y esos tipos encajan mejor en `Application/Common`.
- Cambio recomendado: no reescribir el dominio; solo documentar que hoy es anemico y considerar mover `Result/Error` a Application cuando exista trabajo de limpieza.
- Destino sugerido: `Testing.Application/Common`.
- Dependencias afectadas: `Testing.Application`, `Testing.Domain`, `Testing.Infrastructure`.
- Tests requeridos: unit tests basicos de `Result`.
- Cambio funcional esperado: `NINGUNO`

### ARCH-010

- Archivo: `Testing/Components/Pages/*`
- Lineas: repetidas en `ConsultaViajes`, `TablaPivoteViajes`, `ArbolJerarquiaViajes`, `ResumenBloqueNivel`, `ResumenArbolComparativo`, `ResumenRotacionOperadores`, `TablaOperadores`, `ResumenPresentacion`
- Categoria: Duplication / Presentation
- Severidad: `P2`
- Riesgo: `BAJO`
- Problema: hay formateadores repetidos (`FormatoNumero`, `FormatoDinero`, `ClaseDelta`, `FormatoDeltaPct`, `CultureInfo es-MX`) en multiples componentes.
- Codigo actual: helpers locales duplicados por archivo.
- Por que es un problema: cada ajuste visual o de formato obliga a tocar varios componentes.
- Cambio recomendado: extraer solo la parte claramente comun a un helper de Presentation, no a Application.
- Destino sugerido: `Testing/Features/Common/Formatting` o `Testing/Components/Shared`.
- Dependencias afectadas: `Testing`.
- Tests requeridos: unit tests de formato si se extrae logica no trivial.
- Cambio funcional esperado: `NINGUNO`

### ARCH-011

- Archivo: `repositorio completo`
- Lineas: `N/A`
- Categoria: Testability
- Severidad: `P0`
- Riesgo: `CRITICO`
- Problema: no hay proyectos de test ni baseline de comportamiento automatizado.
- Codigo actual: `dotnet test` no ejecuta nada.
- Por que es un problema: la mayor parte de la logica critica se apoya en parsing, agrupaciones, comparativos y reglas derivadas; moverla sin tests es propenso a regresiones invisibles.
- Cambio recomendado: crear primero `UnitTests`, luego `IntegrationTests` del query, luego `ComponentTests` de `ConsultaViajes`.
- Destino sugerido: `tests/Testing.Application.UnitTests`, `tests/Testing.Infrastructure.IntegrationTests`, `tests/Testing.ComponentTests`
- Dependencias afectadas: toda la solucion.
- Tests requeridos: ver seccion 20.
- Cambio funcional esperado: `NINGUNO`

### ARCH-012

- Archivo: `Testing.Application/GetAllViajes/GetViajesQueryValidator .cs`
- Lineas: archivo completo
- Categoria: Naming / Maintainability
- Severidad: `P3`
- Riesgo: `BAJO`
- Problema: el nombre del archivo contiene un espacio antes de `.cs`.
- Codigo actual: `GetViajesQueryValidator .cs`
- Por que es un problema: genera ruido en tooling, scripts y revisiones.
- Cambio recomendado: renombrar cuando se entre a una fase de limpieza, junto con tests verdes.
- Destino sugerido: mismo directorio, nombre sin espacio.
- Dependencias afectadas: tooling, git history.
- Tests requeridos: ninguno especifico.
- Cambio funcional esperado: `NINGUNO`

### ARCH-013

- Archivo: `Testing/appsettings.Development.json`, `Testing.Application/GetAllViajes/CamposDerivadosViajes.cs`
- Lineas: `appsettings.Development.json` completo, `CamposDerivadosViajes 89-176`
- Categoria: Data correctness / Security / Domain assumptions
- Severidad: `P1`
- Riesgo: `ALTO`
- Problema: existen reglas de negocio derivadas desde `ruta`, `tipo_operacion` y `fecha_ingreso` basadas en ejemplos limitados, y ademas la cadena de conexion de desarrollo esta versionada en texto plano.
- Codigo actual: parseos documentados en comentarios y conexion con usuario/password en repo.
- Por que es un problema: mezcla riesgo funcional con riesgo operativo; cualquier refactor que consolide esas reglas sin pruebas puede fijar suposiciones incorrectas.
- Cambio recomendado: cubrir primero con tests de fixtures reales y sacar secretos a user-secrets/variables de entorno.
- Destino sugerido: reglas validadas en tests; configuracion en secretos de desarrollo.
- Dependencias afectadas: `Testing.Application`, `Testing`.
- Tests requeridos: unit tests con muestras reales del SP.
- Cambio funcional esperado: `NINGUNO`

## 6. Auditoria Domain

Hallazgos:

- No hay entidades, value objects, enums de negocio ni reglas del dominio.
- No hay dependencias tecnicas indebidas dentro de Domain.
- El dominio actual es neutro, pero no representa el negocio de viajes.

Veredicto:

- Lo bueno: independencia tecnica correcta.
- Lo malo: el proyecto Domain hoy no aporta frontera de negocio.

## 7. Auditoria Application

Lo correcto:

- `GetViajesQuery`, `GetViajesQueryValidator`, `ValidationBehavior` y la DI de MediatR estan bien ubicados.
- `CamposDerivadosViajes`, `CorteMensual`, `ContribucionViajeProyectada`, `KpisComparativaCalculator` y buena parte del `ResumenEjecutivo` si pertenecen conceptualmente a Application.

Lo mejorable:

- `GetViajesQueryHandler` conoce SQL.
- `ViajesDto` conoce mapeo de persistencia.
- `ResumenEjecutivoCalculator` necesita particion.
- `ResumenEjecutivoWordExporter` esta mal ubicado.
- `PresentacionModels` y `SlidesPresentacionCalculator` estan en zona gris, mas cerca de Presentation que de Application puro.

## 8. Auditoria Infrastructure

Lo correcto:

- La implementacion concreta de acceso a datos esta en Infrastructure.
- La composicion DI usa `AddDbContextFactory<ZemogContext>` y `AddScoped<IApplicationDbContext, ApplicationDbContext>`, lo cual es razonable para Blazor Server.

Problemas:

- `IApplicationDbContext` es una abstraccion demasiado generica y leaky: expone `sql` y `parameters` a Application.
- `ZemogContext` y `TraficoCombustible` parecen scaffolding residual para un flujo que en realidad se apoya en una sola consulta raw.
- No existe una implementacion feature-specific para `ConsultaViajes`.

## 9. Auditoria Blazor

Componentes mas pesados:

- `ConsultaViajes.razor` - `628` lineas
- `TablaPivoteViajes.razor` - `316` lineas
- `ArbolJerarquiaViajes.razor` - `289` lineas
- `ResumenPresentacion.razor` - `228` lineas
- `ResumenOperadores.razor` - `127` lineas
- `ResumenArbolComparativo.razor` - `149` lineas

Que debe permanecer en Blazor:

- render
- estado visual
- toggles
- navegacion de tabs/slides
- JS interop
- binding
- dispatch del caso de uso

Que debe salir de Blazor:

- cascada de filtros por selector
- filtrado de viajes cargados
- agrupacion pivote
- armado del arbol jerarquico
- calculo de metricas por grupo
- parte de la transformacion para ranking/operadores

## 10. Auditoria ConsultaViajes

Flujo real:

```text
UI
-> click en Actualizar
-> ConsultaViajes.razor: ConsultarAsync
-> ISender.Send(GetViajesQuery)
-> GetViajesQueryHandler
-> IApplicationDbContext.QueryAsync
-> ApplicationDbContext
-> ZemogContext.Database.SqlQueryRaw
-> SP Operaciones.sp_ConsultaViajesZemog
-> List<ViajesDto>
-> CorteMensual.Calcular
-> RecalcularCascada
-> ObtenerViajesFiltrados
-> KpisComparativaCalculator.Calcular
-> RecalcularPivote / RecalcularArbol
-> TablaPivoteViajes o ArbolJerarquiaViajes
-> ResumenEjecutivo opcional
```

Responsabilidades mezcladas detectadas:

- Query orchestration: `619-663`
- Filter metadata and cascade: `303-446`
- Grouping rules and labels: `267-301`, `448-524`
- Hierarchy building: `531-603`
- Metrics aggregation: `577-616`
- KPI formatting: `665-700`
- Visual state and theme: `347-373`

Recomendacion de capas:

- `ConsultaViajes.razor` debe quedar como orquestador visual.
- El procesamiento de viajes debe pasar a Application.
- Los modelos `FilaPivote`, `NodoJerarquia`, `MesColumna` pueden seguir en Presentation en una primera fase, pero si el armado se mueve a Application convendra crear DTOs equivalentes alli.

## 11. Auditoria EF Core

Resultado:

- No hay `Include`, `ThenInclude`, tracking innecesario ni N+1 clasicos del lado LINQ, porque el acceso actual se hace por un solo stored procedure.
- `AsNoTracking()` no aplica al flujo actual de `SqlQueryRaw<TResult>` sobre DTOs no-entidad.
- El riesgo real esta en traer demasiados datos y luego procesarlos completamente en memoria en Blazor/Application.

Puntos concretos:

- `ConsultaViajes.razor` usa multiples `Where(...).ToList()` en cascada.
- `ResumenEjecutivoCalculator` y `OperadoresRotacionCalculator` hacen `First(...)` dentro de loops aun teniendo diccionarios ya construidos.
- La solucion actual privilegia simplicidad de entrega; si el volumen crece, la primera optimizacion correcta es mover agregaciones especificas a Application/Infrastructure, no introducir patrones genericos.

## 12. Auditoria Dependency Injection

Registros propios encontrados:

- `Testing.AddPresentation`: `AddRazorComponents`, `AddInteractiveServerComponents`, `AddApexCharts`
- `Testing.Application.AddApplication`: `AddMediatR`, `AddValidatorsFromAssembly`, `AddTransient(IPipelineBehavior<,>, ValidationBehavior<,>)`
- `Testing.Infrastructure.AddInfrastructure`: `AddDbContextFactory<ZemogContext>`, `AddScoped<IApplicationDbContext, ApplicationDbContext>`

Evaluacion:

- `DbContextFactory`: correcto para Blazor Server
- `IApplicationDbContext` scoped: aceptable
- `ValidationBehavior` transient: correcto
- `ApexCharts`: registrado, pero hoy no hay una grafica productiva renderizada; es deuda baja, no defecto critico

No se detectaron servicios usados sin registrar.

## 13. Problemas SOLID

- `SRP`: `Testing/Components/Pages/ConsultaViajes.razor` maneja UI, consulta, filtros, procesamiento y agregacion.
- `SRP`: `Testing.Application/GetAllViajes/ResumenEjecutivoCalculator.cs` concentra demasiados subproblemas.
- `OCP`: `ConsultaViajes.razor` requiere editar `select`, diccionario de etiquetas y `switch` de agrupacion para cada nueva dimension.
- `OCP`: `ResumenPresentacion.razor` requiere editar multiples bloques `if/else` para cada nuevo slide.
- `DIP`: `GetViajesQueryHandler` depende del detalle `sql + parametros` en lugar de una abstraccion de lectura del caso.
- `ISP`: no hay un problema fuerte detectado.
- `LSP`: no hay herencia polimorfica relevante; no se detecta problema material.

## 14. Codigo duplicado

Duplicacion real:

- helpers de formato y `CultureInfo` repetidos en varios componentes Razor
- logica de comparativa/colores muy parecida entre `TablaPivoteViajes` y `ArbolJerarquiaViajes`
- subtitulo de meses cerrados repetido entre `ResumenEjecutivo.razor` y `SlidesPresentacionCalculator`

No recomiendo extraer agresivamente:

- textos de negocio muy locales por slide
- componentes pequenos que ya son legibles

## 15. Servicios candidatos

| Nombre sugerido | Tipo | Responsabilidad | Interfaz sugerida | Proyecto interfaz | Proyecto implementacion | Metodos aproximados | Reemplazaria | Beneficio |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `IConsultaViajesReadService` | Application read service | Ejecutar la lectura de viajes para la query | Si | `Testing.Application` | `Testing.Infrastructure` | `Task<IReadOnlyList<ViajesDto>> GetViajesAsync(GetViajesQuery, CancellationToken)` | `GetViajesQueryHandler` + `IApplicationDbContext.QueryAsync(sql,...)` | Oculta SQL y mejora testabilidad |
| `ConsultaViajesDataProcessor` | Application service | Aplicar filtros, agrupar, calcular KPIs, pivote y arbol | No inicialmente | `N/A` | `Testing.Application` | `ApplyFilters`, `BuildPivot`, `BuildHierarchy`, `BuildKpis` | Bloques `396-616` de `ConsultaViajes.razor` | Saca logica de UI sin sobrearquitectura |
| `ResumenEjecutivoAssembler` | Application service | Coordinar subcalculadoras del resumen | No inicialmente | `N/A` | `Testing.Application` | `Build` | `ResumenEjecutivoCalculator.Calcular` | Reduce tamaÃ±o y acoplamiento |
| `IResumenEjecutivoDocumentExporter` | Infrastructure service | Exportar el resumen a Word/HTML | Si, solo si se mantendra la exportacion como use case estable | `Testing.Application` | `Testing.Infrastructure` | `string Export(ResumenEjecutivoDto)` | `ResumenEjecutivoWordExporter` | Desacopla Application del formato Word |

## 16. Interfaces candidatas

Interfaces que si tienen sentido:

- `IConsultaViajesReadService`
- `IResumenEjecutivoDocumentExporter` si se confirma que habra mas de un formato o se quiere aislar Word

Interfaces que no recomiendo por ahora:

- interfaces para cada calculator estatico
- `IFiltroService` generico
- `IGenericRepository`
- `IUnitOfWork`

## 17. Archivos a mover

| Archivo actual | Ubicacion actual | Ubicacion sugerida | Motivo | Riesgo |
| --- | --- | --- | --- | --- |
| `ResumenEjecutivoWordExporter.cs` | `Testing.Application/GetAllViajes` | `Testing.Infrastructure/Documents` | detalle Word/HTML | `MEDIO` |
| `PresentacionModels.cs` | `Testing.Application/GetAllViajes` | `Testing/Features/ResumenPresentacion` | modelo de una vista concreta | `MEDIO` |
| `SlidesPresentacionCalculator.cs` | `Testing.Application/GetAllViajes` | `Testing/Features/ResumenPresentacion` o Application coordinado por view models | logica de presentacion especifica | `MEDIO` |
| `ViajesDto.cs` | `Testing.Application/GetAllViajes` | `Application DTO` sin atributos + row model en Infrastructure | eliminar `[Column]` de Application | `MEDIO` |
| `PivoteViajesModels.cs` | `Testing/Features` | `Testing/Features/ConsultaViajes` | feature cohesion | `BAJO` |
| `JerarquiaViajesModels.cs` | `Testing/Features` | `Testing/Features/ConsultaViajes` | feature cohesion | `BAJO` |
| `ConsultaViajesFilterModel.cs` | `Testing/Features` | `Testing/Features/ConsultaViajes` | feature cohesion | `BAJO` |

## 18. Archivos a dividir

| Archivo | Lineas aprox. | Responsabilidades | Division sugerida | Prioridad |
| --- | ---: | --- | --- | --- |
| `ConsultaViajes.razor` | 628 | UI, filtros, KPI, pivote, arbol, query orchestration | `ConsultaViajes.razor` + `ConsultaViajes.razor.cs` + procesador de datos | `P0` |
| `ResumenEjecutivoCalculator.cs` | 397 | meses, nivel, arbol, asignacion, destinos, agencias, semaforo | subcalculadoras por bloque | `P1` |
| `ZemogContext.cs` | 374 | configuracion EF monolitica | configuraciones separadas o contexto minimo | `P2` |
| `TablaPivoteViajes.razor` | 316 | render + orden + helpers + comparativa | opcional `razor.cs` si se toca el componente | `P2` |
| `ArbolJerarquiaViajes.razor` | 289 | render + aplanado + orden + comparativa | opcional `razor.cs` | `P2` |
| `ResumenPresentacion.razor` | 228 | overlay + navegacion + teclado + formateo | `razor` + `razor.cs` | `P2` |
| `ResumenEjecutivoWordExporter.cs` | 195 | varias secciones del documento | helpers por seccion | `P2` |

## 19. Archivos correctamente ubicados

- `Testing/Program.cs`
- `Testing/DependencyInjection.cs`
- `Testing.Application/DependencyInjection.cs`
- `Testing.Application/Behaviors/ValidationBehavior.cs`
- `Testing.Application/GetAllViajes/GetViajesQuery.cs`
- `Testing.Application/GetAllViajes/GetViajesQueryValidator .cs` - ubicacion correcta, nombre incorrecto
- `Testing.Infrastructure/DependencyInjection.cs`
- `Testing/Components/Shared/Filters/MultiSelectFiltro.razor`
- `Testing/Components/Shared/Data/KpiCard.razor`
- `Testing/Components/Shared/Feedback/DataStateBanner.razor`
- `Testing/Components/Layout/ViajesZemogLayout.razor`

## 20. Tests faltantes

Unit Tests prioritarios:

- `CamposDerivadosViajes.ParsearRuta`
- `CamposDerivadosViajes.ParsearClienteZona`
- `CamposDerivadosViajes.ObtenerFechaNegocio`
- `CamposDerivadosViajes.ObtenerTarifa`
- `CorteMensual.Calcular` y `FactorPara`
- `ContribucionViajeProyectada`
- `KpisComparativaCalculator.Calcular`
- `ResumenEjecutivoCalculator` por bloque
- `OperadoresRotacionCalculator`

Integration Tests prioritarios:

- mapping de `sp_ConsultaViajesZemog` a `ViajesDto`
- ejecucion del servicio de lectura de viajes con DB real o ambiente controlado

Component Tests prioritarios:

- `ConsultaViajes`: estados `loading`, `error`, `sin datos`
- `ConsultaViajes`: cascada de filtros
- `ConsultaViajes`: cambio de agrupacion entre pivote y arbol
- `ResumenEjecutivo`: render condicional segun meses cerrados

Bloqueos razonables:

- `BLOQUEADO HASTA CREAR TEST`: mover SQL fuera del handler
- `BLOQUEADO HASTA CREAR TEST`: extraer pipeline de filtros/pivote/arbol de `ConsultaViajes.razor`
- `BLOQUEADO HASTA CREAR TEST`: tocar reglas derivadas de `ruta/tipo_operacion/fecha_ingreso`

## 21. Riesgos de regresion

- SQL/SP mapping: `CRITICO`
- filtros en cascada: `ALTO`
- KPIs comparativos: `ALTO`
- arbol jerarquico: `ALTO`
- pivote mensual/comparativo: `ALTO`
- corte mensual/proyeccion: `ALTO`
- resumen ejecutivo: `ALTO`
- exportacion Word: `MEDIO`
- navegacion de presentacion: `MEDIO`
- formatos visuales: `BAJO`

## 22. Matriz completa de cambios

| # | Archivo actual | Problema | Cambio sugerido | Archivo destino | Prioridad | Riesgo |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `GetViajesQueryHandler.cs` | SQL en Application | mover lectura a servicio especifico | `Infrastructure/Persistence/Viajes/*` | `P1` | `ALTO` |
| 2 | `ViajesDto.cs` | atributo de persistencia | separar DTO y row model | `Application + Infrastructure` | `P1` | `MEDIO` |
| 3 | `ConsultaViajes.razor` | demasiadas responsabilidades | separar code-behind y extraer pipeline | `Testing + Application` | `P0` | `CRITICO` |
| 4 | `ResumenEjecutivoCalculator.cs` | god class | dividir por bloques | `Application/Features/Viajes/ResumenEjecutivo` | `P1` | `ALTO` |
| 5 | `ResumenEjecutivoWordExporter.cs` | detalle Word en Application | mover a Infrastructure | `Infrastructure/Documents` | `P1` | `MEDIO` |
| 6 | `PresentacionModels.cs` | semantica de view | mover a Presentation feature | `Testing/Features/ResumenPresentacion` | `P2` | `MEDIO` |
| 7 | `SlidesPresentacionCalculator.cs` | semantica de view | mover o adelgazar | `Testing/Features/ResumenPresentacion` | `P2` | `MEDIO` |
| 8 | `ZemogContext.cs` | contexto legado pesado | aislar o simplificar | `Infrastructure/Persistence` | `P2` | `MEDIO` |
| 9 | `PivoteViajesModels.cs` y `JerarquiaViajesModels.cs` | cohesion de feature | reubicar por feature | `Testing/Features/ConsultaViajes` | `P3` | `BAJO` |
| 10 | `GetViajesQueryValidator .cs` | naming/tooling | renombrar archivo | mismo directorio | `P3` | `BAJO` |

## 23. Arquitectura propuesta

Objetivo: preservar la UI y la funcionalidad, limpiar responsabilidades y no introducir patrones innecesarios.

Principios:

- mantener MediatR actual
- no introducir Generic Repository
- no introducir CQRS adicional masivo
- no mover logica visual a Domain
- encapsular primero el acceso de lectura y el procesamiento in-memory del feature

## 24. Arbol de carpetas propuesto

```text
src/

Testing.Domain/
  Common/

Testing.Application/
  Abstractions/
    Data/
    Viajes/
  Features/
    Viajes/
      Queries/
      Models/
      Processing/
      ResumenEjecutivo/
      Parsing/

Testing.Infrastructure/
  Persistence/
    Viajes/
    Legacy/
  Documents/

Testing/
  Components/
    Layout/
    Pages/
      ConsultaViajes.razor
      ConsultaViajes.razor.cs
  Features/
    ConsultaViajes/
    ResumenPresentacion/

tests/
  Testing.Application.UnitTests/
  Testing.Infrastructure.IntegrationTests/
  Testing.ComponentTests/
```

## 25. Plan incremental de refactorizacion

### Fase 0

- crear baseline de tests
- cubrir parsing, corte y calculadoras criticas

### Fase 1

- mover `ConsultaViajes.razor` a code-behind sin tocar HTML/CSS

### Fase 2

- extraer `ConsultaViajesDataProcessor` desde el componente

### Fase 3

- introducir `IConsultaViajesReadService` y mover SQL a Infrastructure

### Fase 4

- separar `ViajesDto` de detalles de mapeo

### Fase 5

- dividir `ResumenEjecutivoCalculator`

### Fase 6

- mover exportador Word fuera de Application

### Fase 7

- limpiar duplicaciones de Presentation y aislar legado EF

Cada fase debe:

- compilar
- ejecutarse
- probarse
- validarse visualmente
- poder commitearse sola

## 26. Quick Wins

- crear `ConsultaViajes.razor.cs`
- renombrar `GetViajesQueryValidator .cs`
- agrupar `Testing/Features/*` por feature
- extraer helpers de formato de Presentation
- documentar que `Domain` es anemico y no forzar DDD artificial

## 27. Cambios que NO recomiendo realizar

- reescribir el stored procedure a LINQ solo por pureza
- introducir microservicios
- introducir `GenericRepository`
- introducir `UnitOfWork` personalizado
- introducir `Specification Pattern`
- mover todos los helpers a interfaces
- mover todo a Domain aunque hoy no exista dominio rico
- redisenar la UI, layout o CSS

## 28. Orden exacto recomendado de implementacion

1. Crear proyectos de tests y fixtures reales del SP
2. Aislar `ConsultaViajes` en code-behind
3. Extraer pipeline de filtros/KPIs/pivote/arbol
4. Encapsular acceso de lectura con interfaz especifica
5. Separar DTO de mapeo de persistencia
6. Dividir `ResumenEjecutivoCalculator`
7. Mover exportador Word
8. Revisar `PresentacionModels` y `SlidesPresentacionCalculator`
9. Limpiar `ZemogContext` legado
10. Resolver naming/duplicaciones menores

## 29. Conclusion

La solucion ya tiene una direccion util: separa Web, Application, Domain e Infrastructure, usa MediatR y evita acceso directo a `DbContext` desde Blazor. Sin embargo, la arquitectura limpia actual es mas nominal que real. El problema principal esta en la concentracion de procesamiento en `ConsultaViajes.razor` y en los detalles de persistencia/presentacion que todavia viven dentro de `Testing.Application`.

La siguiente refactorizacion debe ser progresiva y respaldada por tests. No recomiendo una reescritura. Recomiendo consolidar primero el comportamiento actual como baseline, mover la logica fuera del componente principal, y despues limpiar los limites Application/Infrastructure sin alterar la UI ni los resultados actuales.

---

# Addendum: Large Query Performance Audit

## 30. Executive Summary de rendimiento

La lentitud de `ConsultaViajes` no apunta primero a Blazor ni a microoptimizaciones de C#. La evidencia medida el 2026-09-01 muestra que el mayor costo aparece en la ejecucion del stored procedure `[Operaciones].[sp_ConsultaViajesZemog]` y en la transferencia de un resultset amplio. Despues de eso, el tiempo total sigue creciendo por el reprocesamiento en memoria dentro de `ConsultaViajes.razor` y por la construccion de tablas sin paginacion ni virtualizacion.

Hallazgo principal:

- el cuello de botella dominante esta en `SQL + transferencia/materializacion`
- el segundo cuello de botella esta en `procesamiento C# repetido sobre listas grandes`
- el tercer cuello de botella esta en `renderizado de tablas/pivotes/arboles con muchos elementos`

Lo que NO aparecio como problema principal en esta revision:

- N+1 de base de datos en `ConsultaViajes`
- `Include` / `ThenInclude` excesivos en esta funcionalidad
- `ChangeTracker` inflado por EF Core
- graficas activas pesadas en la version actual del repositorio

## 31. Sintoma observado

El comportamiento observado es consistente con este patron:

1. consultas pequenas responden de forma aceptable
2. al ampliar el rango de fechas, el stored procedure crece de forma no lineal
3. al recibir miles de filas, la pagina vuelve a recorrer el dataset varias veces para filtros, KPIs, pivote, arbol y resumenes
4. si el usuario mantiene la tabla visible, Blazor debe renderizar una cantidad alta de celdas sin paginacion real

## 32. Flujo completo de la consulta principal

Flujo real de `ConsultaViajes`:

`Testing/Components/Pages/ConsultaViajes.razor`
`ConsultarAsync`
? envia `GetViajesQuery`
? `Testing.Application/Features/Viajes/Queries/GetViajesQueryHandler.cs`
`Handle`
? construye `EXEC [Operaciones].[sp_ConsultaViajesZemog] @FechaInicial, @FechaFinal`
? `Testing.Infrastructure/Data/ApplicationDbContext.cs`
`QueryAsync<TResult>`
? `Database.SqlQueryRaw<TResult>(...).ToListAsync(...)`
? SQL Server devuelve filas
? materializacion a `ViajesDto`
? `ConsultaViajes.razor`
`CorteMensual.Calcular`
? `CalcularMetricas`
? `ResumenEjecutivoCalculator.Calcular`
? `KpisComparativaCalculator.Calcular`
? `RecalcularCascada`
? `ObtenerViajesFiltrados`
? `RecalcularPivote` o `RecalcularArbol`
? componentes de presentacion:
`TablaPivoteViajes.razor`
`ArbolJerarquiaViajes.razor`
`ResumenOperadores.razor`

## 33. Baseline medido

Medicion directa ejecutada el 2026-09-01 con `SqlClient` contra la misma cadena de conexion configurada en `Testing/appsettings.Development.json`, llamando al mismo stored procedure. Esta medicion aísla `SQL + transferencia/lectura`; no incluye render de Blazor.

| Rango | Filas | Open | Execute/SQL | Read/Transferencia | Total |
| --- | ---: | ---: | ---: | ---: | ---: |
| 1 dia | 5 | 232 ms | 386 ms | 17 ms | 403 ms |
| 7 dias | 2,640 | 216 ms | 14,624 ms | 310 ms | 14,934 ms |
| 31 dias | 12,789 | 234 ms | 16,264 ms | 4,492 ms | 20,756 ms |
| 2026-01-01 a 2026-09-01 | NO MEDIDO | NO MEDIDO | > 90 s sin terminar | NO MEDIDO | > 90 s |

Conclusiones del baseline:

- el problema ya es severo antes de que Blazor procese la respuesta
- el salto de 5 a 2,640 filas dispara el tiempo de SQL a ~14.6 s
- al subir a 12,789 filas, SQL sigue dominando y la transferencia/lectura crece a ~4.5 s
- existe un problema probable de plan, selectividad, cantidad de joins o cardinalidad dentro del stored procedure

## 34. Consultas analizadas

Consulta SQL principal:

- `Testing.Application/Features/Viajes/Queries/GetViajesQueryHandler.cs`

Procesamientos grandes posteriores:

- `Testing/Components/Pages/ConsultaViajes.razor`
- `Testing.Application/Features/Viajes/Services/ResumenEjecutivoCalculator.cs`
- `Testing.Application/Features/Viajes/Services/KpisComparativaCalculator.cs`
- `Testing.Application/Features/Viajes/Services/OperadoresRotacionCalculator.cs`
- `Testing/Components/Viajes/TablaPivoteViajes.razor.cs`
- `Testing/Components/Viajes/ArbolJerarquiaViajes.razor.cs`
- `Testing/Components/Viajes/ResumenOperadores.razor`

## 35. SQL generado y EF Core

SQL llamado desde la aplicacion:

```sql
EXEC [Operaciones].[sp_ConsultaViajesZemog]
    @FechaInicial,
    @FechaFinal
```

Observaciones:

- la funcionalidad depende casi por completo de un stored procedure, no de LINQ composable
- no hay `Include()` ni `ThenInclude()` en esta ruta
- no hay N+1 a nivel EF Core en esta ruta; la pagina hace una sola consulta de base de datos para obtener los viajes
- `AsNoTracking()` no es la mejora principal aqui, porque `SqlQueryRaw<TResult>` ya proyecta DTOs y no carga entidades rastreadas del modelo

Bloqueo actual:

- no se incluyo la definicion del stored procedure dentro del repositorio
- sin el texto del SP ni su execution plan, no se puede atribuir con precision si el costo viene de scans, joins, sorts o parameter sniffing

Estado:

- `SQL interno del SP`: BLOQUEADO HASTA INSPECCIONAR DEFINICION Y PLAN
- `punto de llamada desde C#`: MEDIDO

## 36. Materializacion, columnas y forma del resultset

`ViajesDto` es ancho y el procedimiento devuelve muchas columnas por fila. En el flujo actual se materializa el conjunto completo y despues varias vistas utilizan solo subconjuntos de esos datos.

Campos claramente usados con frecuencia por Presentation/Application:

- `_base`
- `expedicion`
- `ruta`
- `fecha_ingreso`
- `tipo_operacion`
- `kms_viaje`
- `subtotal_factura`
- `operador1`
- `peaje_ingresos`
- `peaje_costos`

Problema:

- aun cuando una vista concreta solo necesita agregados, la consulta trae el detalle completo
- el costo no es solo SQL; tambien aumenta el volumen de transferencia y la materializacion a objetos C#

Recomendacion futura:

- separar consultas de detalle y consultas agregadas
- proyectar DTOs especificos para pivote, KPIs y resumenes cuando el comportamiento actual ya este protegido con tests

## 37. Procesamiento C# y complejidad

Problemas detectados en `Testing/Components/Pages/ConsultaViajes.razor`:

- `RecalcularCascada` vuelve a filtrar `viajesCargados` varias veces y crea listas temporales
- `ObtenerViajesFiltrados` vuelve a recorrer el dataset completo
- `RecalcularPivote` agrupa y vuelve a recorrer grupos/meses para reconstruir celdas
- `RecalcularArbol` reconstruye toda la jerarquia desde cero
- `CalcularMetricas` ejecuta varias agregaciones separadas sobre la misma lista
- `CalcularKpis` vuelve a disparar calculadoras sobre el conjunto ya filtrado

Problemas detectados en servicios/calculadoras:

- `ResumenEjecutivoCalculator.cs` contiene varias pasadas y busquedas repetidas
- `OperadoresRotacionCalculator.cs` usa patrones con `First`/`Any` dentro de loops
- `CamposDerivadosViajes.cs` recalcula parsing de ruta, destino, movimiento y fecha de negocio varias veces sobre los mismos viajes

Impacto:

- al crecer el dataset, la aplicacion no solo hace una consulta grande; tambien multiplica el costo con varios recorridos adicionales
- la complejidad combinada se acerca a `O(n * m)` en varias etapas y tiene puntos con comportamiento cercano a `O(n²)` por busquedas repetidas

## 38. Blazor Rendering, tablas y graficas

Hallazgos:

- no existe paginacion server-side en `ConsultaViajes`
- no existe `Virtualize` en tablas o listas grandes
- `TablaPivoteViajes.razor.cs` recalcula totales, ordenamiento y filas derivadas en cada cambio de parametros
- `ArbolJerarquiaViajes.razor.cs` aplana y ordena jerarquias completas
- `ResumenOperadores.razor` vuelve a ordenar y recortar datos ya agregados

Sobre graficas:

- el repositorio registra `ApexCharts`, pero en la version analizada al 2026-09-01 no aparece una grafica pesada activa en la ruta critica de `ConsultaViajes`
- por lo tanto, hoy el render de tablas/pivotes/arboles pesa mas que las graficas

## 39. Paginacion y cancelacion

Paginacion:

- no hay evidencia de `Skip()` / `Take()` sobre la consulta principal
- la pagina trae todo y luego decide que mostrar
- esto es adecuado para rangos pequenos, pero escala mal cuando el usuario pide historicos amplios

Cancelacion:

- `GetViajesQueryHandler` y `ApplicationDbContext.QueryAsync` aceptan `CancellationToken`
- `ConsultaViajes.razor` no cancela la consulta anterior cuando el usuario lanza otra
- una consulta larga puede seguir consumiendo SQL Server y memoria aunque el usuario ya no necesite el resultado

## 40. Indices

No propongo aun indices concretos por tabla porque la logica esta encapsulada en `[Operaciones].[sp_ConsultaViajesZemog]` y esa definicion no esta versionada en el repositorio. Cualquier recomendacion mas precisa seria especulativa.

Lo que si puede afirmarse:

- si el SP filtra por fecha al inicio, el indice sobre la columna real usada por `@FechaInicial/@FechaFinal` es el primer candidato natural
- si el SP junta tablas por viaje, operador, ruta o cliente, las claves de join deben validarse en el execution plan
- si el SP hace `ORDER BY` o agregaciones grandes sobre columnas no indexadas, el costo puede crecer rapido con historicos amplios

Estado:

- propuesta de indices detallada: BLOQUEADO HASTA VER SP Y PLAN

## 41. Hallazgos detallados

### PERF-001

Archivo:
`Testing.Application/Features/Viajes/Queries/GetViajesQueryHandler.cs`

Metodo:
`Handle`

Categoria:
SQL / EF Core

Severidad:
P0

Problema:
La consulta principal depende de un stored procedure que ya tarda entre ~14.6 s y ~20.8 s con volumenes moderados, y supera 90 s en un rango amplio.

Evidencia:

- 2,640 filas: `Execute/SQL = 14,624 ms`
- 12,789 filas: `Execute/SQL = 16,264 ms`
- rango `2026-01-01` a `2026-09-01`: no termino antes de 90 s

Volumen donde se manifiesta:
desde miles de filas

Tiempo aproximado:
14 s a 90+ s

Causa:
Costo interno del SP, plan no observado, joins/agregaciones/selectividad no validados

Solucion propuesta:
inspeccionar definicion del SP, obtener execution plan real, separar detalle de agregados si aplica

Riesgo:
MEDIO

Impacto esperado:
MUY ALTO

Cambio funcional esperado:
NINGUNO

### PERF-002

Archivo:
`Testing.Infrastructure/Data/ApplicationDbContext.cs`

Metodo:
`QueryAsync<TResult>`

Categoria:
EF Core / Transferencia / Materializacion

Severidad:
P1

Problema:
La aplicacion materializa el resultset completo a `ViajesDto` antes de empezar filtros o agregados derivados.

Evidencia:

- el handler siempre ejecuta `QueryAsync<ViajesDto>`
- el tiempo de lectura/transferencia crece de `310 ms` a `4,492 ms` entre 2,640 y 12,789 filas

Volumen donde se manifiesta:
dataset grande y DTO ancho

Tiempo aproximado:
0.3 s a 4.5 s solo en lectura/transferencia

Causa:
resultset ancho y sin especializacion por escenario

Solucion propuesta:
introducir queries separadas para detalle y agregados, reducir columnas para vistas agregadas

Riesgo:
ALTO

Impacto esperado:
ALTO

Cambio funcional esperado:
NINGUNO

### PERF-003

Archivo:
`Testing/Components/Pages/ConsultaViajes.razor`

Metodo:
`RecalcularCascada`

Categoria:
C# / Procesamiento en memoria

Severidad:
P1

Problema:
La cascada de filtros vuelve a recorrer `viajesCargados` y crea listas temporales repetidamente.

Evidencia:

- varios `Where(...).ToList()` sobre el dataset completo
- el metodo se invoca despues de consultar y tambien al cambiar filtros

Volumen donde se manifiesta:
miles de filas y multiples cambios de filtro

Tiempo aproximado:
NO MEDIDO dentro de la app; impacto inferido por complejidad y cantidad de pasadas

Causa:
filtrado repetido y construccion de colecciones intermedias

Solucion propuesta:
construir un pipeline de filtros reutilizable y caches de valores derivados por dataset

Riesgo:
MEDIO

Impacto esperado:
ALTO

Cambio funcional esperado:
NINGUNO

### PERF-004

Archivo:
`Testing/Components/Pages/ConsultaViajes.razor`

Metodo:
`RecalcularPivote`

Categoria:
C# / Agregaciones / UI

Severidad:
P1

Problema:
El pivote recalcula agrupaciones, meses y metricas en memoria a partir de la lista ya cargada.

Evidencia:

- `GroupBy` sobre el dataset filtrado
- ciclos por grupo y por mes para recomponer celdas

Volumen donde se manifiesta:
consultas grandes con muchos grupos y meses visibles

Tiempo aproximado:
NO MEDIDO dentro de la app; impacto alto por cardinalidad de filas x meses

Causa:
agregacion tardia en C# y no en SQL

Solucion propuesta:
evaluar query agregada especifica para pivote o precalculo reusable posterior a la consulta base

Riesgo:
ALTO

Impacto esperado:
ALTO

Cambio funcional esperado:
NINGUNO

### PERF-005

Archivo:
`Testing/Components/Pages/ConsultaViajes.razor`

Metodo:
`RecalcularArbol`

Categoria:
C# / UI / Memoria

Severidad:
P2

Problema:
La jerarquia se reconstruye completa para cada recalculo, incluso si solo cambian filtros secundarios.

Evidencia:

- reconstruccion total del arbol desde el dataset filtrado
- componentes hijos vuelven a ordenar y aplanar

Volumen donde se manifiesta:
miles de filas con muchas combinaciones de base/destino/movimiento

Tiempo aproximado:
NO MEDIDO

Causa:
estructura derivada sin cache y con recomputacion completa

Solucion propuesta:
memoizar estructura derivada por filtro efectivo o construir modelo preprocesado una sola vez

Riesgo:
MEDIO

Impacto esperado:
MEDIO

Cambio funcional esperado:
NINGUNO

### PERF-006

Archivo:
`Testing.Application/Features/Viajes/Utils/CamposDerivadosViajes.cs`

Metodo:
varios helpers estaticos

Categoria:
C# / CPU

Severidad:
P1

Problema:
El parsing de `ruta`, destino, movimiento y fecha de negocio se recalcula repetidamente sobre los mismos registros.

Evidencia:

- los helpers son consumidos por filtros, resumenes, pivote, jerarquia y KPIs
- no se observa cache del valor derivado por viaje

Volumen donde se manifiesta:
10k+ filas con multiples pantallas derivadas

Tiempo aproximado:
NO MEDIDO

Causa:
coste repetido de parsing y normalizacion

Solucion propuesta:
precalcular campos derivados una vez por dataset o proyectarlos desde una consulta preparada

Riesgo:
MEDIO

Impacto esperado:
ALTO

Cambio funcional esperado:
NINGUNO

### PERF-007

Archivo:
`Testing/Components/Viajes/TablaPivoteViajes.razor.cs`

Metodo:
`OnParametersSet` y helpers de ordenamiento/totales

Categoria:
Blazor / Render / C#

Severidad:
P2

Problema:
El componente vuelve a ordenar filas y recalcular totales por mes en cada actualizacion de parametros, sin paginacion ni virtualizacion.

Evidencia:

- ordenamiento completo de `Filas`
- `ToDictionary` y agregaciones por mes
- render potencialmente muy grande para tablas comparativas

Volumen donde se manifiesta:
cientos o miles de filas pivote

Tiempo aproximado:
NO MEDIDO

Causa:
trabajo de preparacion de vista dentro del componente y render de demasiadas celdas

Solucion propuesta:
preparar el modelo de tabla fuera del componente y limitar filas renderizadas

Riesgo:
BAJO

Impacto esperado:
MEDIO

Cambio funcional esperado:
NINGUNO

### PERF-008

Archivo:
`Testing/Components/Viajes/ArbolJerarquiaViajes.razor.cs`

Metodo:
pipeline de aplanado y ordenamiento

Categoria:
Blazor / Render

Severidad:
P2

Problema:
El arbol ordena y aplana la jerarquia completa en cada render relevante.

Evidencia:

- preparacion de nodos visible dentro del componente
- no hay virtualizacion

Volumen donde se manifiesta:
jerarquias profundas o amplias

Tiempo aproximado:
NO MEDIDO

Causa:
render y preparacion acoplados

Solucion propuesta:
separar modelo de arbol ya calculado y limitar expansiones/renders

Riesgo:
BAJO

Impacto esperado:
MEDIO

Cambio funcional esperado:
NINGUNO

### PERF-009

Archivo:
`Testing.Application/Features/Viajes/Services/ResumenEjecutivoCalculator.cs`

Metodo:
`Calcular`

Categoria:
C# / Complejidad algoritmica

Severidad:
P2

Problema:
El resumen ejecutivo hace varias pasadas y busquedas sobre colecciones relacionadas.

Evidencia:

- multiples agregaciones y comparaciones derivadas
- patrones de busqueda repetida que pueden sustituirse por diccionarios/lookups

Volumen donde se manifiesta:
datasets grandes con resumenes comparativos

Tiempo aproximado:
NO MEDIDO

Causa:
estructura de calculo orientada a claridad funcional, no a costo por volumen

Solucion propuesta:
preindexar por claves de agrupacion y consolidar pasadas

Riesgo:
MEDIO

Impacto esperado:
MEDIO

Cambio funcional esperado:
NINGUNO

### PERF-010

Archivo:
`Testing/Components/Pages/ConsultaViajes.razor`

Metodo:
`ConsultarAsync`

Categoria:
UX / Concurrencia / Recursos

Severidad:
P2

Problema:
Las consultas largas no se cancelan cuando el usuario lanza una nueva.

Evidencia:

- el pipeline acepta `CancellationToken`
- no se observa `CancellationTokenSource` controlado por la pagina

Volumen donde se manifiesta:
consultas largas y cambios rapidos de filtros/rangos

Tiempo aproximado:
NO MEDIDO

Causa:
falta de cancelacion cooperativa desde la UI

Solucion propuesta:
introducir cancelacion por solicitud y descartar resultados obsoletos

Riesgo:
BAJO

Impacto esperado:
MEDIO

Cambio funcional esperado:
NINGUNO

## 42. Top Bottlenecks

1. costo interno del stored procedure `sp_ConsultaViajesZemog`
2. resultset amplio materializado completo en `ViajesDto`
3. ausencia de paginacion real para consultas de detalle
4. reprocesamiento repetido en `ConsultaViajes.razor`
5. recalculo repetido de campos derivados por viaje
6. agregaciones de pivote hechas completamente en memoria
7. resumenes/kpis con multiples pasadas sobre el dataset
8. tablas y jerarquias sin virtualizacion
9. consultas largas sin cancelacion desde la UI
10. trabajo de preparacion de vista acoplado a componentes Blazor

## 43. Quick Wins

- inspeccionar y perfilar el SP antes de tocar Blazor; es el mayor retorno esperado
- separar consulta de detalle vs consultas agregadas para KPIs/resumen/pivote
- evitar recalcular campos derivados mas de una vez por dataset
- reducir pasadas sobre `viajesFiltrados` en `ConsultaViajes.razor`
- introducir paginacion server-side o virtualizacion en tablas de detalle

## 44. Matriz de rendimiento

| ID | Archivo | Metodo | Problema | Tiempo/impacto actual | Solucion | Mejora esperada | Riesgo |
| --- | --- | --- | --- | --- | --- | --- | --- |
| PERF-001 | `GetViajesQueryHandler.cs` | `Handle` | SP lento | 14 s a 90+ s | perfilar y corregir SP/plan | MUY ALTA | MEDIO |
| PERF-002 | `ApplicationDbContext.cs` | `QueryAsync` | materializacion total | 0.3 s a 4.5 s en lectura/transferencia | DTOs agregados y detalle separado | ALTA | ALTO |
| PERF-003 | `ConsultaViajes.razor` | `RecalcularCascada` | filtros repetidos en memoria | NO MEDIDO | pipeline/cache de filtros | ALTA | MEDIO |
| PERF-004 | `ConsultaViajes.razor` | `RecalcularPivote` | pivote en memoria | NO MEDIDO | query agregada o precalculo reusable | ALTA | ALTO |
| PERF-005 | `ConsultaViajes.razor` | `RecalcularArbol` | reconstruccion total del arbol | NO MEDIDO | cache estructural | MEDIA | MEDIO |
| PERF-006 | `CamposDerivadosViajes.cs` | helpers varios | parsing repetido | NO MEDIDO | precalculo por dataset | ALTA | MEDIO |
| PERF-007 | `TablaPivoteViajes.razor.cs` | `OnParametersSet` | tabla sin virtualizacion | NO MEDIDO | limitar render y preparar modelo fuera | MEDIA | BAJO |
| PERF-008 | `ArbolJerarquiaViajes.razor.cs` | varios | aplanado/ordenamiento por render | NO MEDIDO | desacoplar preparacion del render | MEDIA | BAJO |
| PERF-009 | `ResumenEjecutivoCalculator.cs` | `Calcular` | multiples pasadas/busquedas | NO MEDIDO | lookups/diccionarios | MEDIA | MEDIO |
| PERF-010 | `ConsultaViajes.razor` | `ConsultarAsync` | sin cancelacion | NO MEDIDO | `CancellationTokenSource` por consulta | MEDIA | BAJO |

## 45. Plan incremental de optimizacion

### Fase 0

- conservar baseline actual
- instrumentar tiempos por etapa sin dejar codigo permanente
- obtener definicion y execution plan del stored procedure

### Fase 1

- resolver `PERF-001`
- validar indices y plan real
- repetir baseline con mismos rangos

### Fase 2

- separar consultas agregadas de consultas detalle
- reducir columnas y tamano de transferencia donde aplique

### Fase 3

- extraer pipeline de procesamiento de `ConsultaViajes`
- precalcular campos derivados una sola vez por dataset

### Fase 4

- reducir pasadas y busquedas repetidas en calculadoras
- introducir estructuras `Dictionary` / `Lookup` donde corresponda

### Fase 5

- paginacion server-side para detalle
- virtualizacion o carga incremental para tablas largas

### Fase 6

- cancelacion de consultas en UI
- desacoplar preparacion pesada de componentes Blazor

### Fase 7

- reevaluar cache solo si, despues de optimizar SQL/EF/C#, todavia existe latencia relevante

## 46. Riesgos y cambios que NO recomiendo

No recomiendo como primer movimiento:

- tocar `foreach` vs `for`
- introducir cache para ocultar un SP lento
- mover todo el procesamiento a Blazor WebAssembly o redisenar la UI
- reescribir el stored procedure completo a LINQ sin evidencia de beneficio
- optimizar graficas antes de corregir SQL y tablas

## 47. Orden recomendado de implementacion

1. obtener texto y execution plan del SP
2. medir nuevamente `1 dia / 7 dias / 31 dias`
3. resolver el mayor costo SQL
4. separar detalle y agregados
5. extraer y simplificar pipeline de `ConsultaViajes`
6. precalcular campos derivados
7. optimizar pivote/resumen/jerarquia
8. agregar paginacion/virtualizacion
9. agregar cancelacion de consultas
10. benchmark final antes/despues

## 48. Conclusion de rendimiento

La aplicacion se vuelve lenta con consultas grandes principalmente porque el cuello de botella aparece antes de la UI: el stored procedure tarda demasiado y devuelve un conjunto amplio que luego se reprocesa varias veces en memoria. Blazor empeora la experiencia cuando intenta presentar grandes tablas derivadas, pero no es la causa inicial del problema.

La optimizacion con mayor impacto esperado no esta en microcambios de C#, sino en:

1. entender y optimizar `sp_ConsultaViajesZemog`
2. reducir el tamaño y la forma del resultset segun el caso de uso
3. eliminar reprocesamientos innecesarios en `ConsultaViajes`
4. evitar renderizar mas elementos de los que el usuario puede consumir realmente
