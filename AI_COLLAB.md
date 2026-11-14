# Protocolo entre IAs

Acuerdo de trabajo entre las dos IAs de este repo. Léelo junto con [AGENTS.md](AGENTS.md) y [TASKS.md](TASKS.md) antes de escribir código.

## Quién hace qué

| IA | Entra por | Papel |
|---|---|---|
| **Claude** | Elena | Tiene pocos tokens. Toma lo pesado y el núcleo: `GameManager`, sistemas core y lógica compleja. |
| **Grok** | Thermoflask7 (dueño del repo) | Coordina e integra. Revisa cada PR, resuelve conflictos de código, mantiene al día `TASKS.md` y la sección Contratos, y hace documentación, datos, cambios chicos y correcciones. |

## Reglas

1. Antes de programar, leer `AGENTS.md`, `AI_COLLAB.md` y `TASKS.md`.
2. Reclamar la tarea en el mismo PR: en `TASKS.md` llenar **IA asignada** y **Rama**, y poner **Estado** en `en curso`.
3. Una rama por tarea, con uno de estos nombres:
   - `ia/claude/<tarea>`
   - `ia/grok/<tarea>`
   - `feat/<persona>/<tarea>`
4. Nadie sube directo a `main`. Todo cambio entra por pull request.
5. La descripción del PR tiene que decir:
   - qué hace
   - qué archivos toca
   - si cambia un contrato compartido (campos o métodos públicos de `GameManager`, campos de un ScriptableObject, nombres de escenas)
   - cómo probarlo en Unity
6. No editar a mano el YAML de archivos `.unity` ni `.prefab`. Si aparece un conflicto en una escena o un prefab, detente y avisa a las personas del equipo.

## Contratos

API pública que las dos IAs y el equipo comparten. Claude llena la fila de `GameManager` cuando su PR esté listo. Hasta entonces esta tabla es un placeholder: no inventar firmas aquí.

| Contrato | API pública | Estado |
|---|---|---|
| `GameManager` | _Pendiente. Claude escribe los campos y métodos públicos (dinero, piezas compradas, cohete armado, tripulación y entrenamiento, sitio y fecha de lanzamiento, eventos, puntaje, cambio de escena)._ | pendiente |
| ScriptableObjects | _Pendiente. Campos públicos de piezas, astronautas, eventos, sitios de lanzamiento y misión._ | pendiente |
| Escenas | `00_Menu`, `01_Planeacion`, `02_Lanzamiento`, `03_Resultados`, en ese orden en el build. Estaciones del escritorio y minijuegos son prefabs, no escenas. Ver [GDD.md](GDD.md). | acordado |

## Mensajes entre IAs

Bitácora de solo agregar, al final de este archivo. No se editan ni se borran notas viejas.

Formato:

`- [YYYY-MM-DD HH:MM] Grok -> Claude: ...`

- [2026-11-14 13:00] Grok -> Claude: Dejé este protocolo para que trabajemos las dos sin pisarnos. Tú (con Elena, pocos tokens) tomas el núcleo y la lógica pesada; yo (con Thermoflask7) coordino, reviso cada PR, resuelvo conflictos de código y me quedo con documentación, datos, cambios chicos y correcciones. La tarea 0 «GameManager + cambio de escenas + estado» está en `TASKS.md` como tuya, en curso, con la rama `ia/claude/gamemanager` (cámbiala en el tablero si tu rama se llama distinto). Cuando ese PR esté listo, documenta la API pública de `GameManager` en la sección Contratos. No edites a mano el YAML de `.unity` ni de `.prefab`; si hay conflicto ahí, avisa al equipo.
- [2026-11-14 12:30] Grok -> Claude: **Cambió el loop.** El equipo definió el diseño final en `GDD.md`: misión de órbita lunar estilo Artemis, 2 etapas. Ya no son 7 escenas sino 4: `00_Menu`, `01_Planeacion` (escritorio con estaciones: folder, computadora/SpaceMart, modelo de cohete, globo+calendario, botón de lanzamiento), `02_Lanzamiento` (pantalla dividida, fases salida → órbita lunar → retorno con minijuegos) y `03_Resultados`. Por favor ajusta las constantes de escenas y el estado de `GameManager` a esto: además de dinero, piezas y tripulación, necesita cohete armado, entrenamiento por astronauta, sitio y fecha de lanzamiento (con clima), eventos ocurridos y el estado de las 4 tareas que desbloquean el botón de lanzamiento (ver GDD). La negociación de presupuesto quedó fuera: el presupuesto es fijo al inicio. Actualicé `AGENTS.md` y `TASKS.md` con las nuevas tareas y dueños.
