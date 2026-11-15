# Auditoría de los entregables de P5

Rama `ia/claude/p5-auditoria`, sobre `main` en `e290427` (PR #12). Antes de empezar se revisó que nada de esto estuviera ya hecho en `main`.

| Entregable | Documento |
|---|---|
| 1. Informe de auditoría y resultados de balanceo | este archivo |
| 2. Tabla científica con fuentes y clasificación | [DATOS_REFERENCIA.md](DATOS_REFERENCIA.md) |
| 3. Tabla de Δv por tramo y destino | [DELTA_V_DESTINOS.md](DELTA_V_DESTINOS.md) |
| 4. Pruebas de balanceo | Unity: **SpaceApps → Pruebas de balanceo**; Python: `python3 Docs/P5/balance.py` |
| 5. Correcciones de código | sección 6 |
| 6. Decisiones para el equipo | sección 7 |

---

## 1. Balanceo de la misión

Se repitió la enumeración de P5 con el catálogo real (`CatalogoArtemis.json`) y las mismas revisiones de `RocketPhysics`. **Los números de P5 se confirman exactamente.**

Combinaciones: 1 lanzador + Orion + hasta 1 unidad de cada uno de los 11 módulos opcionales = 2 × 2¹¹ = 4,096. Presupuesto: 3,400 créditos.

| | 12 días | 15 días |
|---|---:|---:|
| Viables | **78 (1.90 %)** | **5 (0.12 %)** |
| Viable más barata | 2,940 créditos | 3,140 créditos |
| Viables con Block 1B | 1 | 0 |
| Fallan por presupuesto | 2,401 | 2,401 |
| Fallan por masa (capacidad del SLS) | 1,202 | 1,202 |
| Fallan por Δv de Orion | 622 | 622 |
| Fallan por ciencia (< 40 puntos) | 1,536 | 1,536 |
| Fallan por transmisión (antena) | 1,792 | 1,792 |
| Fallan por consumibles | 0 | 3,584 |
| **Fallan por generación eléctrica** | **0** | **0** |
| **Fallan por batería** | **0** | **0** |

Una combinación puede fallar por varias causas a la vez.

### 1.1 ¿Hay libertad suficiente sin volverlo trivial?

**Sí hay reto, pero poca variedad de estrategias.** Las 78 viables de 12 días se reducen a dos ideas de ciencia:

- 77 usan **Block 1 + antena + SCI-LUNAR + SCI-RAD** (lo que cambia son módulos extra: reservas, navegación, blindaje…).
- 1 usa **Block 1B + antena + SCI-HEAVY**, y le sobran solo 5 créditos.

Aun así existen **al menos tres configuraciones viables con estrategias diferentes**, y las pruebas las protegen:

| Estrategia | Configuración | Créditos | Días |
|---|---|---:|---:|
| Económica | Block 1 + antena + SCI-LUNAR + SCI-RAD | 2,940 | 12 |
| Ciencia pesada | Block 1B + antena + SCI-HEAVY | 3,395 | 12 |
| Misión larga | Block 1 + antena + 2 instrumentos + reservas de agua, oxígeno y comida | 3,140 | 15 |
| Segura | Block 1 + antena + 2 instrumentos + navegación lunar + reserva de comida | 3,115 | 12 |

**Sensibilidad al presupuesto** (solo como dato para decidir, **no se cambió**):

| Presupuesto | Viables 12 días | Viables 15 días |
|---:|---:|---:|
| 3,400 | 78 | 5 |
| 3,600 | 115 | 10 |
| 3,800 | 257 | 18 |
| 4,000 | 461 | 42 |

Subir el presupuesto abre sobre todo combinaciones con Block 1B: con 3,600 créditos hay 28 viables con Block 1B en 12 días. Pero el porcentaje de combinaciones **no mide la dificultad real**: hacen falta pruebas con jugadores (decisión D1).

### 1.2 Compras repetidas

Hoy nada en el código las impedía, y SpaceMart (tarea 8) todavía no existe. Con hasta 2 unidades por pieza:

| | 12 días | 15 días |
|---|---:|---:|
| Combinaciones | 354,294 | 354,294 |
| Viables | 1,146 | 124 |
| Más barata | **2,875**: Block 1 + antena + **2 × SCI-RAD** | **3,075**: igual + reservas |

**Ventaja no contemplada:** dos SCI-RAD dan exactamente 40 puntos de ciencia por 230 créditos. Eso es más barato que SCI-LUNAR + SCI-RAD (295 créditos) y vuelve inútil al instrumento lunar. Además, el catálogo y la enumeración de P5 suponen una unidad por pieza.
**Corrección aplicada (decisión D2, reversible):** `RocketPhysics.MaxCopiesPerPart = 1`, y la validación rechaza dos unidades de la misma pieza. Para permitir repetidas basta con ponerlo en `0` (y entonces habría que rebalancear SCI-RAD).

### 1.3 Energía demasiado sencilla (confirmado)

Con el catálogo actual **ninguna combinación falla por energía**:

- El módulo de servicio genera 11,000 W, y el consumo máximo posible con todos los módulos es de 3,585 W.
- Su batería tiene 18,000 Wh, y lo máximo que se necesita es 3,585 W × 1.5 h = 5,378 Wh.
- Para que la batería limite, el periodo sin sol tendría que pasar de unas 5 h con la nave más cargada, o de 6.3 h con la configuración básica.

**Consecuencia en la puntuación:** los 6 puntos de energía (generación 3 + baterías 3) siempre salen completos. No se cambió nada, porque las fórmulas no tienen error: es un tema de datos. Las opciones van en la decisión D3.

## 2. Modelo físico

Fórmulas revisadas y correctas para el modelo educativo: Tsiolkovsky (`DeltaV`), `TliMass`, `DeliveryMargin`, `SpacecraftDeltaV` (solo propelente del ESM), `PowerBalance`, `BatteryWh`/`BatteryCoversOutage`, márgenes de consumibles, `SciencePoints` y `DownlinkSeconds`. **No se reintrodujeron** etapas del SLS, `SplitStages`, Δv del lanzador ni TWR.

Casos límite:

| Caso | Antes | Ahora |
|---|---|---|
| Misión nula | `ValidationMessage(parts, null)` se saltaba las revisiones de misión y podía dar el cohete por **válido** | "No hay misión asignada." |
| Misión con datos inválidos (Δv ≤ 0, 0 días, valores negativos) | Se aceptaba | Se rechaza (`MissionDataProblem`) |
| Pieza con valores negativos | Solo se revisaban masa y combustible; un consumo negativo daba energía gratis | Se revisan todos los campos (`PartDataProblem`), y combustible sin Isp |
| Ciencia sin datos | Instrumentos con puntos pero 0 bits daban 0 s de envío y **pasaban** | "Los instrumentos no generan datos para mandar a la Tierra" |
| Piezas repetidas | Permitidas sin límite | Máximo 1 por `catalogId` (ver 1.2) |

Todas estas revisiones pasan con el catálogo actual: no cambia ninguna configuración válida.

## 3. Catálogo de piezas y procedencia

- Se mantienen las **16 piezas y sus IDs**. No se cambió ningún valor.
- Tabla documental de SLS, ICPS, EUS, RS-25, RL10 y Orion/ESM con unidad, fuente, estado y uso: [DATOS_REFERENCIA.md](DATOS_REFERENCIA.md).
- Precios, puntos de ciencia, consumos y módulos opcionales quedan marcados como **balance ficticio**.
- `ART-ESM-ORION`: los 7,667 kg secos son una **asignación del modelo** (25,854 − 10,387 − 7,800), no la masa seca oficial. Los 7,800 kg son el **propelente disponible después de la TLI** en el modelo; el ESM real lleva unos 8.6 t. Con 8,618 kg, el Δv del escenario 1 subiría de 1,103 a unos 1,246 m/s, así que **no se sustituye** sin rebalancear (decisión D4).
- **Importador:** antes, `SpaceApps → Importar catalogo Artemis` (y las pruebas, que lo llaman) **sobrescribía sin aviso** `MisionArtemis.asset`. Ahora:
  - Si la misión fue modificada a mano, la conserva y avisa en la consola qué valores son distintos a la V13.
  - Para volver a la V13 hay un menú nuevo: **SpaceApps → Restaurar mision V13**, que pide confirmación.
  - Las piezas sí se siguen actualizando desde el JSON, como antes.

## 4. Δv por destino

Tabla completa en [DELTA_V_DESTINOS.md](DELTA_V_DESTINOS.md):

- Tierra→LEO 10,000 m/s; LEO→TLI 3,200 m/s (lo hace el SLS).
- TLI→NRHO 450 m/s; NRHO→regreso 450 m/s (lo hace Orion).
- LLO: 900 + 900 m/s.
- **1,060 m/s = 900 de referencia NASA + 40 de correcciones + 120 de reserva (supuestos).** No es un requisito oficial de NASA.

## 5. Tripulación (inconsistencia, no se cambió código)

La misión, Orion y los consumibles usan **4 astronautas** (`mission.crewCount = 4`, `crewCapacity = 4`). Pero `GameManager` solo tiene piloto, ingeniero y científico, e `IsCrewReady()` revisa tres. **No se cambió el número de tripulantes.** Es decisión de P3 y P1 (D5). Opciones:

1. **Cuarto astronauta asignable** (por ejemplo, especialista de misión o comandante). Implica un campo nuevo en `GameManager` (contrato), otro rol en el folder y quizá otro minijuego o bono.
2. **Tripulante fijo integrado:** el comandante viene asignado y el jugador elige 3 roles. La interfaz debe mostrar los 4 y explicarlo ("El comandante ya está asignado por la NASA").

Con cualquiera de las dos, la interfaz no debe mostrar 3 personas mientras los consumibles se calculan para 4.

## 6. Correcciones de código (solo las necesarias)

| Archivo | Cambio |
|---|---|
| `Scripts/Fisica/RocketPhysics.cs` | Misión nula o inválida, valores negativos de piezas, ciencia sin datos, límite de piezas repetidas (`MaxCopiesPerPart`). Nuevos métodos públicos: `DataBits`, `PartDataProblem`, `MissionDataProblem`, `RepeatedPart`. Las fórmulas no cambian. |
| `Scripts/Fisica/Editor/CatalogImporter.cs` | No sobrescribe una misión editada; menú **Restaurar mision V13**. |
| `Scripts/Fisica/Editor/BalanceTests.cs` (nuevo) | **SpaceApps → Pruebas de balanceo**: las 4,096 combinaciones de 12 y 15 días (presupuesto, masa, Δv, consumibles, generación, batería, ciencia y comunicaciones), que `ValidationMessage` coincida con las revisiones por separado, las 4 estrategias viables y los casos límite. |

**No se tocaron:** las fórmulas de puntuación, los valores del catálogo, los 3,400 créditos, escenas ni prefabs. `RocketPhysicsTests` (los 9 escenarios de la misión V13) y `MissionScoringTests` siguen igual y deben seguir pasando.

**Cómo probar en Unity:** correr **SpaceApps → Pruebas de fisica**, **Pruebas de puntuacion** y **Pruebas de balanceo**; todo debe salir OK. Este código no se compiló en Unity desde aquí. Los valores esperados de balanceo se obtuvieron con `balance.py`.

## 7. Decisiones que requieren aprobación del equipo

| # | Decisión | Opciones | Recomendación |
|---|---|---|---|
| D1 | ¿El balanceo actual da suficiente libertad? | Dejar 3,400 créditos / subir a 3,600 (abre Block 1B) / abaratar SCI-HEAVY o Block 1B | Dejar 3,400 hasta probar con jugadores; si casi nadie usa Block 1B, probar 3,600 |
| D2 | ¿Se permiten piezas repetidas en SpaceMart? | 1 por pieza (aplicado) / permitir y rebalancear SCI-RAD | 1 por pieza: es lo que suponen el catálogo y la enumeración. P2 debe reflejarlo en SpaceMart |
| D3 | La energía nunca limita | Aceptarlo y quitar o reducir sus 6 puntos de puntuación / periodo sin sol más largo con fuente (las órbitas cercanas a la Tierra y la Luna pueden tener sombras de varias horas) / bajar la batería del ESM en el modelo | Documentarlo en el pitch; si se quiere reto, usar un periodo sin sol con fuente, no un número inventado |
| D4 | Masas reales del ESM (8.6 t de propelente) | Mantener el modelo V13 (7,800 kg) / usar el dato real y subir `requiredDeltaV` o la masa | Mantener y presentarlo como modelo; cambiarlo obliga a rebalancear todo |
| D5 | Cuarto tripulante | Asignable / fijo y explicado en la interfaz | Fijo y explicado: es más simple con el tiempo del hackathon (P3/P1) |
| D6 | Ciencia mínima para ganar y minijuegos obligatorios (pendiente de la auditoría de puntuación) | Ver `MissionScoring.MinExperimentFraction` | Sigue pendiente |
| D7 | Fuentes secundarias | Reemplazar Wikipedia y Plant Services por hojas oficiales (Lockheed Martin, ESA) antes del pitch | Sí, si hay tiempo |
