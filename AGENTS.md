# Contexto del proyecto (para cualquier IA: Claude, Copilot, Cursor, ChatGPT…)

Juego educativo para el **NASA Space Apps Challenge 2026** (reto de diseño de misiones espaciales).
Hackathon de 48 h: entrega el **domingo 15 nov, 8:00 pm** (cierre oficial 11:59 pm).
Equipo de 5. Unity, low poly, build final en **WebGL** para que los jueces jueguen en el navegador.
Protocolo entre las dos IAs (Claude y Grok): [AI_COLLAB.md](AI_COLLAB.md).
Diseño completo del juego: [GDD.md](GDD.md).

## El juego en una frase
Diriges una misión de **órbita lunar tripulada estilo Artemis**: desde tu escritorio de planeación compras piezas,
armas el cohete, entrenas a la tripulación y eliges sitio y fecha de lanzamiento; luego vives el vuelo desde
control de misión. Aprendes los trade-offs reales de masa, Δv, energía, clima y presupuesto.

## Misión elegida
Órbita lunar tripulada (inspirada en Artemis): llegar a órbita baja, inyección translunar, entrar a órbita lunar, recolectar datos y regresar.
Marte y Europa aparecen como misiones bloqueadas ("próximamente"). Aterrizaje lunar solo si sobra tiempo.

## Loop (una sola misión jugable, 5–10 min)
Menú → **Planeación** (escritorio) → **Lanzamiento** (salida → órbita lunar → retorno) → Resultados

## Física (simplificada pero real)
- Δv de la nave: `Δv = Isp * g0 * ln(m0 / mf)` con `g0 = 9.81 m/s²`
  - `m0` = masa total con combustible, `mf` = masa sin combustible. Por etapas: se suma el Δv de cada etapa.
- La misión pide un Δv mínimo (llegar a órbita, inyección translunar, inserción en órbita lunar, regreso). Si la nave no lo alcanza, no se puede lanzar.
- Datos reales de piezas, sitios de lanzamiento y la misión: los prepara P5 (ver `Assets/_Project/Data/`).

## Reglas para escribir código
- **Código simple y legible.** Nada de patrones complicados, inyección de dependencias ni sistemas genéricos. Es un hackathon.
- C#, una clase por archivo, nombres en inglés para código, comentarios en español están bien.
- Datos (piezas, astronautas, eventos, sitios, misión) como **ScriptableObjects**, no hardcodeados.
- Estado global en `GameManager` (singleton con `DontDestroyOnLoad`): dinero, piezas compradas, cohete armado, tripulación y entrenamiento, sitio y fecha de lanzamiento, eventos ocurridos, puntaje.
- Cada escena debe poder probarse sola: si no existe `GameManager`, se crea uno con valores de prueba.
- Cada estación del escritorio y cada minijuego es un **prefab independiente** con su propio script, para que varias personas trabajen en la misma escena sin conflictos.
- Compatible con WebGL: nada de hilos (`System.Threading`), ni lectura/escritura de archivos locales.
- UI solo con uGUI + **TextMeshPro** (`TextMeshProUGUI` / `TMP_Text`). Nada de `UnityEngine.UI.Text` legacy.
- Entrada solo con el **Input System nuevo** (`UnityEngine.InputSystem`: `Keyboard.current`, `Mouse.current` o Input Actions). Nada de `Input.GetKey`, `Input.GetAxis` ni `Input.mousePosition`: el manejador viejo está apagado en el proyecto y truena. Todo EventSystem lleva `InputSystemUIInputModule`, no `StandaloneInputModule`.
- No modificar escenas, prefabs o scripts de otra persona sin avisarle (evita conflictos de merge).

## Estructura de carpetas
```
Assets/_Project/
  Scenes/          00_Menu, 01_Planeacion, 02_Lanzamiento, 03_Resultados
  Scenes/_Sandbox/ una escena de pruebas por persona (nadie más la toca)
  Scripts/Core/        GameManager, cambio de escenas, estado, botón de lanzamiento        (P1)
  Scripts/Tienda/      computadora: app SpaceMart y armado del cohete (modelo)            (P2)
  Scripts/Tripulacion/ folder (misión, astronautas, entrenamiento), globo y calendario    (P3)
  Scripts/Mision/      etapa de lanzamiento: control de misión, fases, minijuegos, eventos (P4)
  Scripts/Fisica/      cálculos de Δv, masa, energía, clima                                (P5)
  Data/            ScriptableObjects (Piezas, Astronautas, Eventos, Destinos)
  Prefabs/  Art/  Audio/
Assets/ThirdParty/  assets descargados (Kenney, Quaternius, modelos NASA)
```

## Quién es dueño de qué
| Rol | Escenas / prefabs / carpetas |
|---|---|
| P1 · Loop | Core, 00_Menu, escena 01_Planeacion (escritorio base y navegación entre estaciones), botón de lanzamiento, 03_Resultados, integración y build WebGL |
| P2 · Tienda/Cohete | Prefabs Computadora (SpaceMart, ajustes, apagar) y ModeloCohete, Scripts/Tienda |
| P3 · Tripulación/Planeación | Prefabs Folder y GloboCalendario, Scripts/Tripulacion |
| P4 · Misión | 02_Lanzamiento, Scripts/Mision, prefabs de minijuegos |
| P5 · Mecatrónica | Scripts/Fisica, Data (valores reales), balanceo |

## Fuera de alcance (no implementar)
Multiplayer, leaderboard, modo creativo / importar naves, IA rival real. Tech tree solo si sobra tiempo (como app en la computadora).
