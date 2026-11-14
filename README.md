# Space Apps 2026 – Juego de diseño de misiones

Contexto completo del proyecto en [AGENTS.md](AGENTS.md).

## Setup para cada integrante (una sola vez)
1. Instalar **Unity Hub** y la **misma versión exacta** de Unity que el proyecto (ver `ProjectSettings/ProjectVersion.txt`).
   Al instalarla, marcar el módulo **WebGL Build Support**.
2. Instalar Git (o GitHub Desktop) y clonar este repo.
3. En Unity Hub: *Add → Add project from disk* → seleccionar la carpeta del repo. La primera vez tarda (genera `Library/`).
4. Crear tu escena de pruebas en `Assets/_Project/Scenes/_Sandbox/TuNombre.unity`.

## Reglas de Git
- Antes de empezar: **pull**. Commits chicos y seguido. Antes de push: **pull** otra vez.
- Solo editas tus escenas y carpetas (ver tabla en AGENTS.md). Si necesitas algo de otro, avisa por el chat.
- Nunca subir `Library/`, `Temp/`, `Logs/`, `UserSettings/` (ya están en `.gitignore`).
- Siempre subir los `.meta` junto con su archivo.
- Ningún archivo de más de 50 MB.
- Si hay conflicto en una escena o prefab: **no lo resuelvas a mano**, avisa a P1.

## Usar IA en el proyecto
- **Claude Code**: lee `CLAUDE.md` automáticamente (que importa `AGENTS.md`).
- **Copilot / Cursor / Codex / otros**: leen `AGENTS.md`.
- **Chat (claude.ai, ChatGPT)**: pega o sube `AGENTS.md` al inicio de la conversación.
