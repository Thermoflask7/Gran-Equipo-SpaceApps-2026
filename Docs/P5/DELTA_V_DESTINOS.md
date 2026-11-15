# Δv necesario por tramo y destino (P5)

Documento técnico de referencia. **No agrega destinos jugables**: la única misión jugable sigue siendo la órbita lunar tipo Artemis (NRHO).

## Términos

| Sigla | Qué es |
|---|---|
| **LEO** | Órbita baja terrestre (unos cientos de km de altura). Primer paso de cualquier misión. |
| **TLI** | Inyección translunar: el encendido que saca a la nave de LEO hacia la Luna. |
| **NRHO** | Órbita de halo casi rectilínea alrededor de la Luna: muy alargada, cerca del polo sur lunar. Es la órbita de Artemis y del Gateway. Llegar y salir cuesta poco Δv. |
| **LLO** | Órbita lunar baja (unos 100 km). Más cerca de la superficie, pero entrar y salir cuesta mucho más Δv. |
| **TEI** | Inyección de regreso a la Tierra desde la órbita lunar. |

## Tramos (valores aproximados de NASA)

Fuente principal: NASA Architecture Workshop, junio de 2023, "The Artemis Orbit" [A]. Valores redondeados de arquitectura, no requisitos de una misión específica.

| Tramo | Δv | Tiempo aprox. | Quién lo hace (Artemis, Block 1) | Fuente | Estado |
|---|---:|---|---|---|---|
| Tierra → LEO | 10,000 m/s | — | SLS: propulsores + etapa central | [A] | PUB |
| LEO → TLI | 3,200 m/s | — | SLS: etapa superior (ICPS con 1 RL10 en Block 1; EUS en Block 1B) | [A] | PUB |
| TLI → NRHO (llegada) | 450 m/s | ~5 días | Orion (ESM) | [A], [B] | PUB |
| NRHO → trayectoria de regreso (TEI) | 450 m/s | — | Orion (ESM) | [A], [B] | PUB |
| TLI → LLO global | 900 m/s | ~4 días | Orion (ESM) | [A] | PUB |
| LLO → regreso a la Tierra | 900 m/s | ~4 días | Orion (ESM) | [A] | PUB |
| Total NRHO: llegada + salida | 1,000 m/s ("as-built") | — | Orion | [A] | PUB |

[B] (NASA NTRS, 2025) da un rango de 400 a 450 m/s para TLI→NRHO y para NRHO→TEI, lo que confirma [A].

**Por qué NRHO y no LLO:** ir y volver de LLO cuesta unos 1,800 m/s (900 + 900), y NRHO unos 900 m/s (450 + 450). Orion **no lleva suficiente propelente** para LLO con tripulación y regreso. Por eso Artemis usa NRHO, y el aterrizaje lo hace otro vehículo (HLS).

### Ojo con quién hace la TLI

En Artemis I, la TLI la hizo la **etapa superior del SLS (ICPS)**. La página de NASA del ESM dice que el motor principal del ESM realiza "el encendido de inyección translunar" [C]. En nuestro modelo seguimos lo primero: **el SLS entrega la nave ya en TLI** (`payloadCapacity` = kg a la translunar). Orion solo paga los tramos lunares. Si los jueces preguntan, esto es una simplificación consistente con Block 1. La diferencia de [C] se documenta para no presentarla como contradicción.

## Presupuesto de Orion en el juego

| Concepto | Δv | Estado |
|---|---:|---|
| Llegada a NRHO | 450 m/s | Referencia NASA [A] |
| Salida de NRHO hacia la Tierra | 450 m/s | Referencia NASA [A] |
| **Maniobras de referencia** | **900 m/s** | CALC (450 + 450) |
| Correcciones de trayectoria | 40 m/s | **Supuesto del modelo** |
| Reserva | 120 m/s | **Supuesto del modelo** |
| **Total educativo (`requiredDeltaV`)** | **1,060 m/s** | CALC + SUP |

**1,060 m/s no es un requisito oficial de NASA.** Es la suma de 900 m/s de referencia publicada más 160 m/s de supuestos del equipo. Así debe presentarse.

Comprobación con el modelo: el escenario 1 (Block 1 + antena + 2 instrumentos) da **1,103 m/s** con Tsiolkovsky (`Isp × g0 × ln(m0/mf)`, Isp = 315.1 s, 7,800 kg de propelente). Le sobran 43 m/s sobre los 1,060 requeridos.

## Lo que hace cada vehículo (resumen para el pitch)

- **SLS:** Tierra → LEO → TLI (≈ 13,200 m/s en total). En el juego no se calcula: el jugador elige Block 1 (27 t a TLI) o Block 1B (38 t). Por eso no hay Δv ni TWR del lanzador.
- **Orion/ESM:** TLI → NRHO → regreso (900 m/s + correcciones + reserva). Es lo que el juego calcula con Tsiolkovsky.

## Fuentes

- [A] NASA Architecture Workshop (junio de 2023), "The Artemis Orbit / Cis-Lunar Architecture". https://www.nasa.gov/wp-content/uploads/2023/10/nrho-artemis-orbit.pdf. Ver también el artículo técnico de NASA "Why NRHO: the Artemis Orbit": https://nasa.gov/wp-content/uploads/2023/10/acr22-wp-why-nrho-the-artemis-orbit.pdf
- [B] NASA NTRS 20250000718, "Enhancing Fast Transfer Launch Availability". https://ntrs.nasa.gov/api/citations/20250000718/downloads/Enhancing%20Fast%20Transfer%20Launch%20Availability.pdf
- [C] NASA — European Service Module. https://www.nasa.gov/reference/european-service-module/
