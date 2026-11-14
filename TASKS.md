# Tablero de tareas

Una sola misión jugable, en este orden: Presupuesto → Tienda/Armado → Tripulación → Lanzamiento → Misión (eventos y minijuegos) → Resultados. En paralelo van la física (Δv), los datos, el build WebGL y el pitch.

Antes de tomar una tarea, lee [AGENTS.md](AGENTS.md) y [AI_COLLAB.md](AI_COLLAB.md). Para reclamarla, en el mismo PR llena **IA asignada** y **Rama**, y pasa **Estado** a `en curso`.

Estados válidos: `pendiente`, `en curso`, `en PR`, `hecho`.

**Responsable humano** se queda en `por asignar` hasta que alguien del equipo escriba su nombre.

| ID | Tarea | Rol | Responsable humano | IA asignada | Rama | Estado |
|---|---|---|---|---|---|---|
| 0 | GameManager + cambio de escenas + estado | P1 | por asignar | Claude | `ia/claude/gamemanager` | en curso |
| 1 | Presupuesto (`01_Presupuesto`) | P3 | por asignar | ninguna | — | pendiente |
| 2 | Tienda y armado del cohete (`02_Tienda`) | P2 | por asignar | ninguna | — | pendiente |
| 3 | Tripulación (`03_Tripulacion`) | P3 | por asignar | ninguna | — | pendiente |
| 4 | Lanzamiento (`04_Lanzamiento`) | P3 | por asignar | ninguna | — | pendiente |
| 5 | Misión: eventos y minijuegos (`05_Mision`) | P4 | por asignar | ninguna | — | pendiente |
| 6 | Resultados (`06_Resultados`) | P1 | por asignar | ninguna | — | pendiente |
| 7 | Física: Δv, masa y energía | P5 | por asignar | ninguna | — | pendiente |
| 8 | Datos: ScriptableObjects (piezas, astronautas, eventos, destinos) | P5 | por asignar | ninguna | — | pendiente |
| 9 | Build WebGL | P1 | por asignar | ninguna | — | pendiente |
| 10 | Pitch para los jueces | P1–P5 | por asignar | ninguna | — | pendiente |

`00_Menu` es de P1 y todavía no tiene fila. Quien la arme reclama una tarea nueva en este tablero, o la cuelga de la tarea 0 si solo es la escena de entrada del loop.
