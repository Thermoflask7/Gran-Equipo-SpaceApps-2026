# Tablero de tareas

Diseño completo en [GDD.md](GDD.md). Loop: Menú → Planeación (escritorio) → Lanzamiento (salida → órbita lunar → retorno) → Resultados.

Antes de tomar una tarea, lee [AGENTS.md](AGENTS.md) y [AI_COLLAB.md](AI_COLLAB.md). Para reclamarla, en el mismo PR llena **IA asignada** y **Rama**, y pasa **Estado** a `en curso`.

Estados válidos: `pendiente`, `en curso`, `en PR`, `hecho`.

**Responsable humano** se queda en `por asignar` hasta que alguien del equipo escriba su nombre.

| ID | Tarea | Rol | Responsable humano | IA asignada | Rama | Estado |
|---|---|---|---|---|---|---|
| 0 | GameManager + cambio de escenas + estado | P1 | Elena | Claude | `ia/claude/gamemanager` | hecho |
| 1 | `00_Menu`: pantalla de título | P1 | Mateo | Grok | `feat/mateo/menu` | hecho |
| 2 | `01_Planeacion`: escritorio base, cámara y navegación entre estaciones | P1 | por asignar | ninguna | — | pendiente |
| 3 | Botón de lanzamiento + checklist de tareas | P1 | Elena | Claude | `ia/claude/boton-lanzamiento` | hecho |
| 4 | Folder: misión + actualizaciones por eventos | P3 | por asignar | ninguna | — | pendiente |
| 5 | Folder: perfiles de astronautas + entrenamiento | P3 | por asignar | ninguna | — | pendiente |
| 6 | Globo terráqueo + calendario (sitio, clima, viento) | P3 | por asignar | ninguna | — | pendiente |
| 7 | Computadora: escritorio de apps, ajustes y apagar | P2 | por asignar | ninguna | — | pendiente |
| 8 | Computadora: app SpaceMart (comprar piezas) | P2 | por asignar | ninguna | — | pendiente |
| 9 | Modelo de cohete: armado con piezas compradas | P2 | por asignar | ninguna | — | pendiente |
| 10 | `02_Lanzamiento`: pantalla dividida (control de misión 2D + cohete 3D) | P4 | Mateo | Grok | `feat/mateo/lanzamiento` | en curso |
| 11 | Fases del vuelo: salida, órbita lunar, retorno | P4 | por asignar | ninguna | — | pendiente |
| 12 | Minijuego de pilotaje | P4 | KvotheGuile | Claude-M | `ia/claude-m/pilotaje` | pendiente |
| 13 | Minijuego de mantenimiento | P4 | por asignar | ninguna | — | pendiente |
| 14 | Minijuego de investigación (recolección de datos) | P4 | por asignar | ninguna | — | pendiente |
| 15 | Eventos aleatorios (planeación y vuelo) | P4 | Elena | Claude | `ia/claude/eventos` | hecho |
| 16 | `03_Resultados`: puntaje final y explicación educativa | P1 | Elena | Claude | `ia/claude/puntuacion-artemis` (puntuación V16 de Cristina, ajustada a Artemis) | en PR |
| 17 | Física: Δv, masa y energía | P5 | Elena, Cristina | Claude | `ia/claude/catalogo` (corrección + catálogo de Cristina) | hecho |
| 18 | Datos: piezas, astronautas, eventos, sitios y clima, misión lunar | P5 | Cristina | ninguna | — (Drive de Cristina) | en curso |
| 19 | Build WebGL | P1 | por asignar | ninguna | — | pendiente |
| 20 | Pitch para los jueces | P1–P5 | por asignar | ninguna | — | pendiente |
| 21 | (Opcional) Tech tree como app de la computadora | P2 | por asignar | ninguna | — | pendiente |
