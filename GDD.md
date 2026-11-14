# Documento de diseño (GDD)

**Misión:** órbita lunar tripulada estilo Artemis. Despegar, viajar a la Luna, orbitarla haciendo ciencia y regresar a salvo.

El juego tiene **2 etapas**: Planeación de misión y Lanzamiento. Al final se calcula el puntaje con el desempeño en ambas.

---

## Etapa 1 · Planeación de misión (`01_Planeacion`)

El jugador está frente a un **escritorio** con varios objetos. Cada objeto es una **estación** (un prefab independiente) que se abre al hacer clic.

| Estación | Qué hace | Dueño |
|---|---|---|
| **Folder** | Página 1: descripción de la misión y actualizaciones por eventos aleatorios. Página 2: perfiles de los astronautas y asignación de entrenamiento. Páginas extra opcionales (info del cohete, etc.). | P3 |
| **Computadora** | Escritorio con apps: **SpaceMart** (tienda tipo Amazon para comprar piezas), **Ajustes** del juego, **Apagar** (pregunta si quieres salir). Tech tree como app solo si sobra tiempo. | P2 |
| **Modelo de cohete** | Armado del cohete final con las piezas compradas. Muestra masa, Δv y si alcanza para la misión. | P2 |
| **Globo terráqueo + calendario** | Elegir sitio de lanzamiento en el globo; el calendario muestra el clima y el viento del sitio en el día de lanzamiento. | P3 |
| **Botón de lanzamiento** | Bloqueado hasta completar todas las tareas. Al hacer clic pasa a la etapa de Lanzamiento. | P1 |

### Tareas que desbloquean el botón
1. Misión leída (folder abierto al menos una vez).
2. Tripulación asignada y con entrenamiento elegido.
3. Cohete armado y válido: Δv ≥ el que pide la misión y masa dentro del límite.
4. Sitio y fecha de lanzamiento elegidos.

El presupuesto es fijo al inicio (la negociación de presupuesto quedó fuera de esta versión) y se gasta en SpaceMart.

---

## Etapa 2 · Lanzamiento (`02_Lanzamiento`)

**Pantalla dividida:**
- **Izquierda (2D):** control de misión. Gente en sus computadoras y, al fondo, una pantalla grande con el estado y los datos del vuelo.
- **Derecha (3D):** el cohete haciendo su ruta Tierra → Luna → Tierra.

**Fases del vuelo:**

| Fase | Contenido |
|---|---|
| **Salida** | Despegue. Minijuegos de pilotaje y mantenimiento. Pueden surgir eventos aleatorios que disparan minijuegos. |
| **Órbita lunar** | Minijuegos de investigación (recolección de datos) y eventos aleatorios. |
| **Retorno** | Igual que la salida pero más rápido. Reentrada. |

Cada minijuego devuelve un resultado (0 a 1) que se multiplica por la habilidad del astronauta del rol correspondiente (piloto, ingeniero, investigador).

Los eventos aleatorios dependen de lo que se decidió en la planeación (por ejemplo, mal clima en el sitio elegido aumenta el riesgo en la salida).

---

## Resultados (`03_Resultados`)

Puntaje final = desempeño en la planeación (presupuesto restante, cohete eficiente, buena fecha y sitio, entrenamiento) + desempeño en los minijuegos de la misión. Pantalla con el desglose y una explicación educativa de cada decisión.

---

## Escenas

| Escena | Contenido |
|---|---|
| `00_Menu` | Pantalla de título |
| `01_Planeacion` | Escritorio con las 5 estaciones |
| `02_Lanzamiento` | Pantalla dividida, 3 fases, minijuegos |
| `03_Resultados` | Puntaje y explicación |

Los minijuegos y las estaciones son **prefabs**, no escenas, para que cada quien trabaje en su propio archivo.
