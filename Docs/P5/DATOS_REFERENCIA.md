# Anexo técnico: datos de referencia y procedencia (P5)

Tabla documental, **separada del catálogo jugable**. Las 16 piezas `ART-*` y sus IDs no cambian.
Motores y tanques (RS-25, RL10, ICPS, EUS) **no son piezas jugables**: el SLS se compra como servicio completo.

**Estado de cada dato**

| Clave | Significado |
|---|---|
| **PUB** | Publicado por NASA o ESA |
| **PUB-F** | Publicado por el fabricante (L3Harris, Lockheed Martin, Airbus) o una agencia socia (DLR) |
| **SEC** | Solo se encontró en fuente secundaria (Wikipedia, prensa). Verificar antes de citarlo ante los jueces |
| **CALC** | Calculado a partir de datos publicados |
| **SUP** | Supuesto educativo del modelo |
| **JUEGO** | Balance ficticio del videojuego (no es un dato real) |

Revisado el 2026-11-14 contra `main` en `e290427`.

## 1. Lanzador SLS (servicio completo)

| Dato | Valor | Unidad | Fuente | Estado | Uso en el juego |
|---|---:|---|---|---|---|
| Carga a la Luna, Block 1 | más de 27 (59,500 lb) | t | [1] | PUB | `ART-SLS-BLOCK1.payloadCapacity` = 27,000 kg |
| Carga a la Luna, Block 1B tripulado | más de 38 (84,000 lb) | t | [1] | PUB | `ART-SLS-BLOCK1B.payloadCapacity` = 38,000 kg |
| Carga a la Luna, Block 1B solo carga | más de 42 (93,000 lb) | t | [1] | PUB | No se usa |
| Block 2 a espacio profundo | hasta 46 | t | [1] | PUB | No se usa |
| Empuje máximo, Block 1 | 8.8 millones (≈ 39.1 MN) | lbf | [1] | PUB (MN = CALC) | Solo referencia |
| Altura, Block 1 | 322 | ft | [1] | PUB | Solo referencia |
| Masa cargada, Block 1 | 5.75 millones | lb | [1] | PUB | Solo referencia |
| Etapa central | 4 motores RS-25; 733,000 galones de LH2/LOX | — | [1] | PUB | Solo referencia |
| Propulsores sólidos (c/u) | 5 segmentos; 3.6 millones de lbf; 1.6 millones de lb; 177 ft | — | [1] | PUB | Solo referencia |
| Precio Block 1 / Block 1B | 1,250 / 1,780 | créditos | Catálogo V8.3 | JUEGO | Precio en SpaceMart |
| Masa del lanzador en el modelo | 0 | kg | Catálogo V8.3 | SUP | El SLS solo aporta capacidad |

## 2. Motores (anexo, no jugables)

| Motor | Dato | Valor | Unidad | Fuente | Estado |
|---|---|---:|---|---|---|
| RS-25 (etapa central) | Empuje máximo (operación SLS) | 512,000 | lbf | [1] | PUB |
| RS-25 | Empuje 109 %: nivel del mar / vacío | 418,000 / 512,300 | lbf | [6] | PUB-F |
| RS-25 | Isp en vacío | 452.3 | s | [7] | PUB-F |
| RS-25 | Masa seca | 7,774 | lb | [7] | PUB-F |
| RS-25 ×4 | Empuje conjunto | ≈ 2 millones | lbf | [1] | PUB |
| RL10B-2 (ICPS, Block 1) | Empuje | 24,750 | lbf | [1], [5] | PUB |
| RL10B-2 | Isp | 465.5 | s | [5] | PUB-F |
| RL10B-2 | Masa | 664 | lb | [5] | PUB-F |
| RL10C-3 ×4 (EUS, Block 1B) | Empuje conjunto | 97,000 | lbf | [1] | PUB |
| RL10C-3 | Empuje / Isp (c/u) | 24,340 / 460.1 | lbf / s | [8] | PUB-F (leído de tabla por columnas: verificar) |
| OMS-E / AJ10 (ESM) | Empuje | 26.6 | kN | [12] | SEC |
| R4D-11 (ESM, 8 auxiliares) | Empuje conjunto | 3.92 | kN | [12] | SEC |

**Tanques / etapas:** NASA no publica en [1] las masas de propelente de la ICPS ni de la EUS. Quedan pendientes. **No se inventan**.

## 3. Orion y Módulo de Servicio Europeo (ESM)

| Dato | Valor | Unidad | Fuente | Estado | Uso en el juego |
|---|---:|---|---|---|---|
| Tripulación / duración sin acoplar | 4 / hasta 21 | personas / días | [3] | PUB | `crewCapacity` = 4, `maxMissionDays` = 21 |
| Masa bruta al despegue (CM + ESM + LAS) | 78,000 | lb | [3] | PUB | Solo referencia |
| Masa de la cápsula (CM) al despegue | 10,387 | kg | [10] | PUB-F vía SEC | `ART-CM-ORION.dryMass` |
| Masa en la translunar (TLI) | 26,536 | kg | [10] | PUB-F vía SEC | Base de `tliExtraMass` |
| Masa después de la TLI | 25,854 | kg | [10] | PUB-F vía SEC | Base del modelo AM-17/18 |
| `tliExtraMass` | 682 = 26,536 − 25,854 | kg | — | CALC | `DestinationData.tliExtraMass` |
| Propelente utilizable del ESM | 8,618 (19,000 lb) | kg | [10] | PUB-F vía SEC | **No se usa** (ver nota ESM) |
| Propelente del ESM | 8.6 | t | [9], [11] | PUB-F | Referencia |
| Masa del ESM al despegue | poco más de 13 | t | [9], [11] | PUB-F | Referencia |
| Masa seca del ESM (revisión de diseño preliminar) | 3.5 | t | [13] | PUB (NTRS, 2014) | Referencia |
| Motores del ESM | 33: 1 principal, 8 auxiliares R4D-11, 24 de control de reacción | — | [4] | PUB | Solo referencia |
| Paneles | 4 alas, 15,000 celdas de arseniuro de galio | — | [4] | PUB | Solo referencia |
| Potencia de los paneles | ≈ 11 | kW | [12] | SEC | `ART-ESM-ORION.power` = 11,000 W |
| Isp del ESM (modelo) | 315.1 | s | Catálogo V8.3 | SUP (no verificado en esta auditoría) | `ART-ESM-ORION.isp` |
| **Masa seca del ESM en el modelo** | 7,667 | kg | 25,854 − 10,387 − 7,800 | CALC / SUP | `ART-ESM-ORION.dryMass` |
| **Propelente del ESM en el modelo** | 7,800 | kg | Misión V13 | SUP | `ART-ESM-ORION.fuelMass` |
| Batería / consumo base | 18,000 / 2,500 | Wh / W | Catálogo V8.3 | SUP | Energía del modelo |

### Nota sobre `ART-ESM-ORION` (puntos 5 a 7 de la auditoría)

- **7,667 kg no es la masa seca oficial del ESM.** Es una **asignación del modelo**: lo que queda de la masa posterior a la TLI (25,854 kg) después de restar la cápsula (10,387 kg) y el propelente del modelo (7,800 kg). Por eso incluye, en la práctica, propelente residual, consumibles y el adaptador. Referencias publicadas de masa seca: 3.5 t en la revisión de diseño preliminar [13]; unos 6.2 t en fuente secundaria [12].
- **7,800 kg es el propelente disponible en el modelo después de la translunar**, no la carga completa del ESM real: 8.6 t publicadas [9], [11], o 8,618 kg utilizables [10]. Es un supuesto de la misión V13 que deja reserva para maniobras que el modelo no simula.
- **Impacto de cambiarlo:** con 8,618 kg y la misma masa total, el Δv de Orion en el escenario 1 subiría de 1,103 a unos 1,246 m/s. Las 4,096 combinaciones pasarían a fallar mucho menos por Δv: hoy fallan 622. **No se debe sustituir** sin recalcular toda la misión y el balanceo (decisión D4).

## 4. Misión (`MisionArtemis.asset`, V13)

| Dato | Valor | Unidad | Fuente | Estado |
|---|---:|---|---|---|
| Δv requerido de Orion | 1,060 | m/s | Ver `DELTA_V_DESTINOS.md` | CALC + SUP (900 de referencia NASA + 40 + 120) |
| Tripulación | 4 | personas | [3] | PUB (capacidad de Orion) |
| Duración | 12 (y 15 en las pruebas) | días | Misión V13 | SUP / JUEGO |
| Agua / oxígeno / comida por persona y día | 2.5 / 0.84 / 0.75 | kg | Misión V13 | SUP (valores típicos de planeación; no verificados en esta auditoría) |
| Agua / oxígeno / comida base en Orion | 135 / 47 / 44 | kg | Misión V13 | SUP |
| Horas sin sol que cubre la batería | 1.5 | h | Misión V13 | SUP |
| Ciencia mínima / tiempo máximo de envío | 40 puntos / 3,600 s | — | Misión V13 | JUEGO |

## 5. Módulos opcionales (todos JUEGO)

Precios, masas de módulos opcionales, consumos, puntos de ciencia, tasas de datos y bits de experimentos de `ART-CREW-*`, `ART-PWR-BACKUP`, `ART-RAD-SHIELD`, `ART-NAV-LUNAR`, `ART-COMMS-DS` y `ART-SCI-*` son **balance ficticio del videojuego**. Están inspirados en sistemas reales, pero **no deben presentarse como valores de NASA**.

## Fuentes

1. NASA — Space Launch System (referencia). https://www.nasa.gov/reference/space-launch-system/
2. NASA — SLS Block 1B (referencia). https://www.nasa.gov/reference/sls-space-launch-system-block-1b/ (no se pudo abrir en esta auditoría; los datos de Block 1B vienen de [1])
3. NASA — Orion overview. https://www.nasa.gov/humans-in-space/orion-spacecraft/orion-overview/
4. NASA — European Service Module (referencia). https://www.nasa.gov/reference/european-service-module/
5. L3Harris — RL10 engine. https://www.l3harris.com/all-capabilities/rl10-engine
6. L3Harris — RS-25 spec sheet (2024). https://l3harris.com/sites/default/files/2024-07/l3harris-ar-rs-25-spec-sheet.pdf
7. L3Harris — RS-25 spec sheet (2023). https://www.L3Harris.com/sites/default/files/2023-07/AJRD-LHX_RS-25_SpecSheet_CMYK_061423_APPROVED.pdf
8. L3Harris — RL10 spec sheet (2024). https://l3harris.com/sites/default/files/2024-07/l3harris-ar-rl10-spec-sheet.pdf y https://www.l3harris.com/sites/default/files/2024-01/AJRD-LHX_RL10_SpecSheet_CMYK_APPROVED_011024.pdf
9. Airbus — Orion European Service Module. https://airbus.com/en/space/exploration/moon/orion-european-service-module
10. Lockheed Martin, cifras citadas en Plant Services, "Inside Lockheed Martin's Orion crew capsule". https://www.plantservices.com/industrial-science-report/article/55378102/inside-lockheed-martins-orion-crew-capsule (buscar la hoja oficial de Lockheed para citar ante jueces)
11. DLR — The European Service Module. https://www.dlr.de/en/research-and-transfer/featured-topics/mission-moon/artemis-programme/the-european-service-module-esm
12. Wikipedia — European Service Module (secundaria). https://en.wikipedia.org/wiki/European_Service_Module
13. NASA NTRS 20140010527 — The MPCV European Service Module (PDR). https://ntrs.nasa.gov/citations/20140010527

Las páginas de ESA ([ESM](https://www.esa.int/Science_Exploration/Human_and_Robotic_Exploration/Orion/European_Service_Module), [ESM-4](https://www.esa.int/ESA_Multimedia/Images/2022/10/European_Service_Module-4_in_production)) no se pudieron abrir en esta auditoría. Los datos de ESM se tomaron de NASA, Airbus, DLR y NTRS. Conviene revisarlas a mano.
