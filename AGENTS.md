# Contexto del proyecto (para cualquier IA: Claude, Copilot, Cursor, ChatGPT…)

Juego educativo para el **NASA Space Apps Challenge 2026** (reto de diseño de misiones espaciales).
Hackathon de 48 h: entrega el **domingo 15 nov, 8:00 pm** (cierre oficial 11:59 pm).
Equipo de 5. Unity, low poly, build final en **WebGL** para que los jueces jueguen en el navegador.
Protocolo entre las dos IAs (Claude y Grok): [AI_COLLAB.md](AI_COLLAB.md).

## El juego en una frase
Diriges una agencia espacial: consigues presupuesto, compras piezas y armas la nave, eliges tripulación,
escoges ventana de lanzamiento y ejecutas la misión. Aprendes los trade-offs reales de masa, Δv, energía y presupuesto.

## Loop (una sola misión jugable, 5–10 min)
Presupuesto → Tienda/Armado → Tripulación → Lanzamiento → Misión (eventos + minijuegos) → Resultados

## Física (simplificada pero real)
- Δv de la nave: `Δv = Isp * g0 * ln(m0 / mf)` con `g0 = 9.81 m/s²`
  - `m0` = masa total con combustible, `mf` = masa sin combustible. Por etapas: se suma el Δv de cada etapa.
- Cada destino pide un Δv mínimo. Si la nave no lo alcanza, la misión falla.
- Datos reales de piezas y destinos: los prepara P5 (ver `Assets/_Project/Data/`).

## Reglas para escribir código
- **Código simple y legible.** Nada de patrones complicados, inyección de dependencias ni sistemas genéricos. Es un hackathon.
- C#, una clase por archivo, nombres en inglés para código, comentarios en español están bien.
- Datos (piezas, astronautas, eventos, destinos) como **ScriptableObjects**, no hardcodeados.
- Estado global en `GameManager` (singleton con `DontDestroyOnLoad`): dinero, piezas compradas, tripulación, destino, puntaje.
- Cada escena debe poder probarse sola: si no existe `GameManager`, se crea uno con valores de prueba.
- Compatible con WebGL: nada de hilos (`System.Threading`), ni lectura/escritura de archivos locales.
- UI con uGUI + TextMeshPro.
- No modificar escenas, prefabs o scripts de otra persona sin avisarle (evita conflictos de merge).

## Estructura de carpetas
```
Assets/_Project/
  Scenes/          00_Menu, 01_Presupuesto, 02_Tienda, 03_Tripulacion, 04_Lanzamiento, 05_Mision, 06_Resultados
  Scenes/_Sandbox/ una escena de pruebas por persona (nadie más la toca)
  Scripts/Core/        GameManager, cambio de escenas, estado      (P1)
  Scripts/Tienda/      tienda SpaceMart y armado del cohete         (P2)
  Scripts/Tripulacion/ astronautas, entrenamiento, presupuesto      (P3)
  Scripts/Mision/      fases, minijuegos, eventos                   (P4)
  Scripts/Fisica/      cálculos de Δv, masa, energía                (P5)
  Data/            ScriptableObjects (Piezas, Astronautas, Eventos, Destinos)
  Prefabs/  Art/  Audio/
Assets/ThirdParty/  assets descargados (Kenney, Quaternius, modelos NASA)
```

## Quién es dueño de qué
| Rol | Escenas / carpetas |
|---|---|
| P1 · Loop | Core, 00_Menu, 06_Resultados, integración y build WebGL |
| P2 · Tienda/Cohete | 02_Tienda, Scripts/Tienda |
| P3 · Tripulación | 01_Presupuesto, 03_Tripulacion, 04_Lanzamiento |
| P4 · Misión | 05_Mision, Scripts/Mision |
| P5 · Mecatrónica | Scripts/Fisica, Data (valores reales), balanceo |

## Fuera de alcance (no implementar)
Multiplayer, leaderboard, modo creativo / importar naves, tech tree completo, IA rival real.
