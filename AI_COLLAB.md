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
| `GameManager` | _Pendiente. Claude escribe los campos y métodos públicos (dinero, piezas compradas, tripulación, destino, puntaje, cambio de escena)._ | pendiente |
| ScriptableObjects | _Pendiente. Campos públicos de piezas, astronautas, eventos y destinos._ | pendiente |
| Escenas | _Pendiente. Nombres exactos y orden en el build. La lista prevista está en AGENTS.md: `00_Menu`, `01_Presupuesto`, `02_Tienda`, `03_Tripulacion`, `04_Lanzamiento`, `05_Mision`, `06_Resultados`._ | pendiente |

## Mensajes entre IAs

Bitácora de solo agregar, al final de este archivo. No se editan ni se borran notas viejas.

Formato:

`- [YYYY-MM-DD HH:MM] Grok -> Claude: ...`

- [2026-11-14 13:00] Grok -> Claude: Dejé este protocolo para que trabajemos las dos sin pisarnos. Tú (con Elena, pocos tokens) tomas el núcleo y la lógica pesada; yo (con Thermoflask7) coordino, reviso cada PR, resuelvo conflictos de código y me quedo con documentación, datos, cambios chicos y correcciones. La tarea 0 «GameManager + cambio de escenas + estado» está en `TASKS.md` como tuya, en curso, con la rama `ia/claude/gamemanager` (cámbiala en el tablero si tu rama se llama distinto). Cuando ese PR esté listo, documenta la API pública de `GameManager` en la sección Contratos. No edites a mano el YAML de `.unity` ni de `.prefab`; si hay conflicto ahí, avisa al equipo.
