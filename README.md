# Space Apps 2026 – Mission design game

Full project context in [AGENTS.md](AGENTS.md).

## Setup for each team member (only once)
1. Install **Unity Hub** and the **exact same version** of Unity as the project (see `ProjectSettings/ProjectVersion.txt`).
   When installing it, check the **WebGL Build Support** module.
2. Install Git (or GitHub Desktop) and clone this repo.
3. In Unity Hub: *Add → Add project from disk* → select the repo folder. The first time takes a while (it generates `Library/`).
4. Create your test scene in `Assets/_Project/Scenes/_Sandbox/YourName.unity`.

## Git rules
- Before starting: **pull**. Small, frequent commits. Before pushing: **pull** again.
- Only edit your own scenes and folders (see the table in AGENTS.md). If you need something from someone else, ask in the chat.
- Never push `Library/`, `Temp/`, `Logs/`, `UserSettings/` (they are already in `.gitignore`).
- Always push the `.meta` files together with their file.
- No file larger than 50 MB.
- If there is a conflict in a scene or prefab: **do not resolve it by hand**, tell P1.

## Using AI in the project
- **Claude Code**: reads `CLAUDE.md` automatically (which imports `AGENTS.md`).
- **Copilot / Cursor / Codex / others**: read `AGENTS.md`.
- **Chat (claude.ai, ChatGPT)**: paste or upload `AGENTS.md` at the start of the conversation.
