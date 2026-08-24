# Plan de Implementación — Sistema de Animación de Bravard

**Fecha:** 2026-08-19
**Alcance:** Capa que conecta la física del jugador con el `Animator_Bravard`.
**Animator afectado:** `Assets/Animation/Characters_Animation/Characters_Animators/Animator_Bravard.controller`

**Convención de nomenclatura:** identificadores en **inglés**; comentarios y documentación en
**español**.

---

## 1. Principio rector

> El Animator es la máquina de estados. El código solo mide la física y escribe parámetros.
> Nada en el código decide qué animación se reproduce.

Única excepción: los dos triggers de salto. "Se ejecutó un salto" es un **evento**, no un
estado derivable de la velocidad, y hacen falta para que el Animator distinga el salto simple
del doble.

Este principio se adoptó tras un primer intento fallido, documentado en la sección 7.

---

## 2. Diagnóstico: por qué no funcionaba

El `PlayerAnimationBridge` original leía la física por su cuenta, en paralelo a
`PlayerMovement`. Eso produjo cuatro fuentes de verdad y ninguna coincidía.

| # | Problema | Consecuencia |
|---|---|---|
| 1 | `HorizontalSpeed` se tomaba de `rb.linearVelocity.x/z`, pero `PlayerMovement.FixedUpdate` mueve al jugador con `rb.MovePosition()`, que reposiciona un Rigidbody dinámico **sin generar velocidad lineal**. Además el eje Z está congelado (`m_Constraints: 88`). | `HorizontalSpeed` valía siempre ≈ 0. Bravard quedaba en **Idle permanente**. |
| 2 | El bridge repetía el `Physics.CheckBox` del suelo con sus propios campos, y su `groundLayer` estaba en **Nothing** (`m_Bits: 0`) frente al `m_Bits: 8` de `PlayerMovement`. | `IsGrounded` valía siempre **false**. `JumpSystem` nunca llegaba a `Jump_End`. |
| 3 | El bridge se suscribía por su cuenta a la acción `Jump` del Input System y hacía `Enable()`/`Disable()`, peleando con el `FindActionMap("Player").Enable()` de `PlayerMovement`. | `JumpTrigger` se disparaba **también con saltos rechazados**, y el Input quedaba con dos dueños. |
| 4 | `DoubleJumpTrigger` existía en el Animator pero **ningún script lo escribía**. | La animación de doble salto era inalcanzable. |
| 5 | Los `SetTrigger` nunca se limpiaban. | Triggers residuales que disparaban saltos fantasma. |

El punto 1 es el bug de fondo: **no se puede leer la velocidad horizontal del Rigidbody en este
proyecto.** Hay que medirla.

---

## 3. Arquitectura

```
 ┌──────────────────────────────────────────────────────────────┐
 │  PlayerMovement                                              │
 │  Única fuente de verdad: física, suelo, saltos               │
 │  · mide la velocidad horizontal REAL por desplazamiento      │
 │  · expone HorizontalSpeed, VerticalVelocity, IsGrounded,     │
 │    IsAboutToLand, IsAnticipatingJump                         │
 │  · publica OnGroundJump / OnAirJump                          │
 └───────────────────────────┬──────────────────────────────────┘
                             │
 ┌───────────────────────────▼──────────────────────────────────┐
 │  PlayerAnimationBridge   (~100 líneas, 1 archivo)            │
 │  Traduce física a parámetros. No decide estados.             │
 └───────────────────────────┬──────────────────────────────────┘
                             │  HorizontalSpeed, VerticalVelocity,
                             │  IsGrounded, IsAboutToLand, FallPlaybackSpeed,
                             │  JumpTrigger, DoubleJumpTrigger
 ┌───────────────────────────▼──────────────────────────────────┐
 │  Animator_Bravard                                            │
 │  Blend Tree + estados + transiciones. Toda la lógica.        │
 └──────────────────────────────────────────────────────────────┘
```

| Archivo | Rol |
|---|---|
| `Characters_AnimationScripts/PlayerAnimationBridge.cs` | Único archivo de la capa de animación. |
| `Scripts/Andres/PlayerMovement.cs` | Cambios **aditivos**: medición de velocidad real, 5 propiedades, 2 eventos, ventana de anticipación, aceleración horizontal y aviso adelantado de aterrizaje. |

### Aceleración horizontal

`moveInput.x` con teclado vale -1, 0 o 1, así que el movimiento pasaba de 0 a `moveSpeed` en un
frame. `FixedUpdate` interpone ahora una rampa: `currentHorizontalSpeed` persigue con
`Mathf.MoveTowards` a la velocidad que pide el input.

| Campo | Valor | Significado |
|---|---|---|
| `timeToTopSpeed` | 0.15 s | de parado a `moveSpeed` |
| `timeToStop` | 0.12 s | de `moveSpeed` a parado |

Se frena (y no se acelera) cuando el objetivo es menor en magnitud **o va en sentido contrario**,
así que cambiar de dirección a plena carrera frena hasta cero y vuelve a acelerar en lugar de
invertirse de golpe. La rotación sigue leyendo el input en crudo, de modo que Bravard se gira al
instante aunque todavía se deslice hacia el otro lado.

Poniendo ambos tiempos a 0 se recupera exactamente el movimiento instantáneo anterior.

El nombre de archivo y de clase `PlayerAnimationBridge` **no debe cambiarse**: la escena lo
referencia por el GUID de su `.meta`, y Unity resuelve el MonoBehaviour por coincidencia entre
nombre de clase y de archivo.

Los campos serializados existentes de `PlayerMovement` **tampoco se renombraron**: las escenas
guardan sus valores por nombre y un renombre los borraría del Inspector.

---

## 4. Medición de la velocidad horizontal real

```csharp
// En PlayerMovement.FixedUpdate, antes de aplicar el movimiento
Vector3 realDisplacement = rb.position - previousPhysicsPosition;
previousPhysicsPosition = rb.position;
realHorizontalSpeed = new Vector2(realDisplacement.x, realDisplacement.z).magnitude / Time.fixedDeltaTime;
```

Se mide dentro de `PlayerMovement` y no en el bridge para que el orden de ejecución de scripts
no altere el resultado. Es la velocidad **efectiva**: si un obstáculo bloquea al jugador el
valor cae a 0 y Bravard vuelve a Idle, que es el comportamiento correcto.

La velocidad vertical sí se lee de `rb.linearVelocity.y`: el salto usa `AddForce` y la caída la
integra la gravedad, así que ahí el Rigidbody sí es fuente válida.

---

## 5. Parámetros del Animator

| Parámetro | Origen | Notas |
|---|---|---|
| `HorizontalSpeed` (float) | `PlayerMovement.HorizontalSpeed` | Con suavizado, y **multiplicado por el peso de locomoción** (ver abajo) |
| `VerticalVelocity` (float) | `rb.linearVelocity.y` | **Sin** suavizar: las condiciones comparan contra 0.2 y -0.1 |
| `IsGrounded` (bool) | `PlayerMovement.IsGrounded` | Único chequeo de suelo del proyecto |
| `IsAboutToLand` (bool) | `PlayerMovement.IsAboutToLand` | Aviso adelantado del aterrizaje (sección 6) |
| `FallPlaybackSpeed` (float) | calculado en el bridge | Multiplica la velocidad del clip de caída. **Default 1**, nunca 0 |
| `JumpTrigger` | evento `OnGroundJump` | Se limpia `DoubleJumpTrigger` antes de activarlo |
| `DoubleJumpTrigger` | evento `OnAirJump` | Se limpia `JumpTrigger` antes de activarlo |

El bridge lleva `[DefaultExecutionOrder(100)]` para correr **después** de `PlayerMovement`
(orden 0). Sin eso el orden entre ambos `Update()` es indefinido y el Animator podría recibir
los valores del frame anterior, con hasta un frame (~16 ms) de desfase.

El Rigidbody del jugador usa `Interpolate`. La física corre a 50 Hz y el juego dibuja a 60+, así
que sin interpolación la posición se repite en algunos frames y el movimiento se ve a tirones.

### El suavizado de `HorizontalSpeed`

```csharp
animator.SetFloat(hashHorizontalSpeed, playerMovement.HorizontalSpeed, horizontalSpeedDamping, Time.deltaTime);
```

**Este parámetro debe quedarse pequeño (0.05 s).** En su día fue un apaño: como el movimiento
saltaba de 0 a `moveSpeed` en un frame, el suavizado era lo único que hacía que el Blend Tree
cruzara el umbral de caminata (0 / 2 / 5) antes de llegar a la carrera.

Desde que `PlayerMovement` tiene aceleración real (`timeToTopSpeed` / `timeToStop`), esa función
desapareció: la velocidad medida ya sube de forma gradual por sí sola. Lo único que le queda al
suavizado es limar el escalón entre la medición (50 Hz de `FixedUpdate`) y la lectura (60+ Hz de
`Update`), más los picos por colisión.

Subirlo produce **doble suavizado** y la animación se queda por detrás del personaje. Llegó a
estar en 0.5 s en la escena: con 0.15 s de rampa física encima, la carrera tardaba casi 0.7 s en
aparecer mientras Bravard ya corría a tope desde el 0.15. Si la mezcla `Idle → Caminata →
Carrera` se ve mal, el mando correcto es `timeToTopSpeed` en `PlayerMovement`, no este.

### El silenciado de la locomoción

Síntoma reportado: al saltar **en movimiento**, la pose de anticipación se veía como una
combinación entre el desplazamiento horizontal y el encogimiento vertical, y el encogimiento
quedaba desdibujado. Saltando quieto se veía bien.

La sospecha inicial fue el retraso del impulso (`groundJumpAnticipation`). **No era eso.** El
retraso es lo que hace visible la pose. Medido: la transición de entrada dura 0.075 s con
`Fixed Duration`, contra una ventana de 0.12 s, así que el **62 % de la ventana era un crossfade**
entre la carrera y la anticipación. Y el compañero de mezcla era el peor posible: con
`moveSpeed = 5` el Blend Tree está exactamente en el umbral de `Run`, que además corre a
TimeScale 1.5.

Hay dos formas de hacer esa mezcla invisible:

| Vía | Qué hace | Coste |
|---|---|---|
| Acortar el crossfade | Menos tiempo mezclando | Paga con la entrada suave afinada a mano, y arriesga un salto de pose visible: el estilo **no es cartoon** |
| **Silenciar la locomoción** | Cambia *qué hay al otro lado* de la mezcla: Run pasa a ser Idle | Bravard se desliza con la animación detenida |

Se eligió la segunda. El problema nunca fue "el crossfade es largo", fue **"el compañero del
crossfade es una pose de carrera"**. Con la locomoción en Idle, la mezcla es Idle + encogimiento,
y la pose de Idle es casi el frame 0 de la anticipación: la mezcla se vuelve visualmente un
no-op. **No se tocó ningún valor del Animator**, así que el riesgo de pop es cero por
construcción.

> **Excepción explícita al principio de §1.** El bridge reporta aquí un `HorizontalSpeed` que
> **no** es el medido. Es deliberado y está acotado a dos ventanas conocidas. No duplica la
> máquina de estados —el Animator sigue decidiendo cada transición—, solo modela una entrada de
> presentación. Si alguien lo "arregla" para que vuelva a reportar el valor crudo, reaparece la
> mezcla.

```csharp
float reportedHorizontalSpeed = playerMovement.HorizontalSpeed * locomotionWeight;
```

`locomotionWeight` va de 1 (locomoción a pleno) a 0 (callada) con rampas configurables. **El
personaje no se detiene:** sigue desplazándose con toda su inercia. Solo se calla la animación.

`HorizontalSpeed` se lee en exactamente **dos** sitios del Animator, así que silenciarlo tiene
exactamente dos efectos, y los dos son los buscados:

| Dónde se lee | Efecto de silenciarlo |
|---|---|
| Blend Tree de `Locomotion` (umbrales 0 / 2 / 5) | Cae a Idle → escenario limpio para la anticipación |
| Condición `Jump_End → Locomotion` (`HorizontalSpeed > 0.1`, exit 0.4) | No puede disparar → el aterrizaje se reproduce entero por la salida incondicional de exit 0.9 |

### Las dos ventanas se detectan de forma distinta

No es una inconsistencia: cada una necesita empezar en un momento que la otra vía no sabría ver.

| Ventana | Cómo se detecta | Por qué no la otra vía |
|---|---|---|
| Anticipación del salto | `PlayerMovement.IsAnticipatingJump` | Hay que empezar a callar en el **mismo frame** de la pulsación, cuando el Animator está todavía *en* la transición de entrada y su estado actual sigue siendo `Locomotion`. Un Tag no lo vería |
| Aterrizaje | Tag `Landing` en el estado `Jump_End` | El estado ya está a peso completo; no hace falta adivinar duraciones y no hay un segundo mando que afinar |

**Estar en transición devuelve la voz a la locomoción** (`animator.IsInTransition(0)`), salvo
durante la anticipación. Es lo que hace que al arrancar la salida del aterrizaje hacia
`Locomotion` el Blend Tree ya venga subiendo hacia la carrera, en lugar de llegar a Idle y
acelerar después — que se vería como un doble parpadeo Idle → Run.

| Campo (en el bridge) | Valor | Regla de ajuste |
|---|---|---|
| `locomotionMuteAttack` | 0.05 s | **Por debajo** de la transición de entrada a la anticipación (0.075 s). Si tarda más, el Blend Tree aún va camino de Idle cuando la mezcla ya terminó. A 0 se vería el salto de pose |
| `locomotionMuteRelease` | 0.18 s | Parecido a la transición de salida hacia `Locomotion` (0.2 s), para que el Blend Tree llegue ya en carrera al final de la mezcla |

El Tag se pone a mano en el campo **Tag** del Inspector del estado. Hoy solo lo lleva `Jump_End`
**el de `JumpSystem`**, que es donde silenciar surte efecto: su salida temprana es la que depende
de `HorizontalSpeed`.

### El canje aceptado: el patinaje

Bravard se desliza mientras su animación de locomoción está detenida. A `moveSpeed = 5`:

| Ventana | Desplazamiento con la animación detenida |
|---|---|
| Anticipación (0.12 s) | ~0.6 unidades — apenas se nota |
| Aterrizaje (exit 0.9 = 0.48 s, antes 0.21 s) | ~2.4 unidades en pose de recomposición |

Es una decisión tomada a conciencia: se prefiere la animación completa al deslizamiento. Si el
patinaje del aterrizaje molesta, el mando **no** es la máscara: es quitarle el Tag a `Jump_End`,
con lo que vuelve la salida temprana de exit 0.4 y el aterrizaje se acorta a 0.21 s.

> **Límite conocido.** Esto no se aplica al caer de una plataforma **sin saltar**. Ese camino usa
> la copia de `Jump_End` del `Base Layer` (ver §10), que no tiene ninguna condición sobre
> `HorizontalSpeed` —solo una salida incondicional a exit 0.531—, así que silenciar no alargaría
> nada. Y esa copia arrastra `Transition Offset 0.369`: arranca en el frame 5.9 de 16 y se salta
> el impacto del aterrizaje. Se dejó así a propósito en esta pasada.

---

## 6. La caída: velocidad de animación y aterrizaje adelantado

### Multiplicador de velocidad del clip de caída

`Jump_GoingDown` es un ciclo de 20 frames (0.667 s a 30 fps) **en bucle**. Se reproducía siempre
a velocidad 1, así que Bravard caía igual resbalando de un escalón que desplomándose desde lo
alto: la caída no transmitía peso.

Los dos estados `Jump_GoingDown` usan ahora `m_SpeedParameterActive` con el parámetro
`FallPlaybackSpeed`, que el bridge calcula a partir de la velocidad vertical:

```csharp
float fallSpeed = Mathf.Max(0f, -playerMovement.VerticalVelocity);
float fallProgress = Mathf.InverseLerp(fallSpeedForNormalPlayback, fallSpeedForMaxPlayback, fallSpeed);
animator.SetFloat(hashFallPlaybackSpeed, Mathf.Lerp(1f, maxFallPlaybackSpeed, fallProgress));
```

| Campo (en el bridge) | Valor | Significado |
|---|---|---|
| `fallSpeedForNormalPlayback` | 4 u/s | Hasta aquí, velocidad normal |
| `fallSpeedForMaxPlayback` | 12 u/s | Aquí llega al máximo |
| `maxFallPlaybackSpeed` | 2.0 | Multiplicador tope |

Con `jumpForce = 7` y masa 1, un salto normal vuelve al suelo a ~7 u/s, o sea a mitad de la rampa.

Dos detalles que no son opcionales:

- **El default de `FallPlaybackSpeed` en el Animator debe ser 1, no 0.** Un default de 0 congela
  el clip durante los frames anteriores a la primera escritura del bridge.
- `Mathf.InverseLerp` ya recorta su resultado entre 0 y 1, así que el multiplicador nunca baja de
  1 (la animación no se ralentiza) ni pasa del máximo. El cálculo vive en el **bridge**, no en
  `PlayerMovement`: a qué ritmo se reproduce un clip es presentación, no física.

### El aterrizaje llegaba tarde

Síntoma reportado: Bravard choca con el piso, se queda un instante estático, y de repente aparece
la animación de aterrizaje **ya empezada**.

No era latencia de detección. La causa estaba en la transición `Jump_GoingDown → Jump_End`:

| Ajuste | Valor anterior | Coste |
|---|---|---|
| `Transition Duration` | 0.12 s | 0.12 s en los que la pose visible sigue siendo la de caída |
| `Transition Offset` | 0.2124 | Se saltaba los **3.4 primeros frames** del aterrizaje |

`Jump_End` dura 16 frames (0.533 s). Entre el offset y el crossfade, cuando la pose de aterrizaje
por fin dominaba la mezcla el clip iba ya por el **frame 7 de 16**. Y lo que se veía durante esos
0.12 s era el ciclo de caída, que **está en bucle**: seguía girando con Bravard ya parado en el
suelo. De ahí la sensación de parón.

### El aviso adelantado

Además de corregir la transición (offset a 0, duración a 0.08), `PlayerMovement` avisa del
aterrizaje **antes** de que ocurra. Es una trampa deliberada: el clip necesita unos frames para
levantar la pose, y si se lanza justo al tocar, esos frames se ven con Bravard ya en el suelo.

```csharp
// Cuanto más rápido cae, más lejos mira: el aviso llega siempre con los mismos
// segundos de antelación, independientemente de la velocidad
float lookAheadDistance = groundCheckDistance + fallSpeed * landingAnticipation;
```

La caja **se estira** desde los pies hasta el punto previsto en lugar de desplazarse hasta él,
para que no se pueda colar una plataforma intermedia sin detectar. Si `isGrounded` ya es cierto,
el aviso lo es también: así nunca puede llegar tarde.

`landingAnticipation` vale **0.08 s**, igualado a propósito a la duración de la transición, de
modo que la mezcla **termina** en el instante del contacto: al tocar el piso Bravard está al
100 % en `Jump_End`, y con offset 0 lo está desde su primer frame.

Las dos transiciones `Jump_GoingDown → Jump_End` (la del salto y la del duplicado de `Base Layer`)
condicionan ahora sobre `IsAboutToLand` en lugar de `IsGrounded`. Son las **únicas** dos que
cambiaron: las otras cuatro que consultan `IsGrounded` se quedan como estaban.

Un gizmo amarillo dibuja la caja de adelanto en Play para afinar el valor mirando la escena.
Poner `landingAnticipation` a 0 devuelve el aviso al contacto real.

---

## 7. Intento descartado: máquina de estados en C#

Se construyó primero una capa de **738 líneas y 12 tipos** (enum, struct, interfaz y 9 clases)
que replicaba en código los estados del Animator, más 177 líneas en `PlayerMovement` con 6
eventos y un salto de tres fases. Se descartó por dos motivos:

1. **Lógica duplicada.** El Animator ya es una máquina de estados. Tener la misma lógica en el
   asset y en el código creó exactamente el problema que este trabajo debía eliminar. Provocó
   desincronías silenciosas al borrar `Jump_MaxHeight` y al cambiar umbrales de transición.
2. **La ventana de anticipación estaba mal dimensionada.** Se fijó igual a la duración
   **completa** del clip (0.533 s), lo que metía más de medio segundo entre pulsar y despegar.
   El concepto era correcto; el número no. Volvió después a 0.12 s (sección 8) y con ese
   valor funciona.

**Lección:** el intento mezcló tres trabajos distintos —corregir bugs, elegir arquitectura y
ajustar la sensación del salto— en un solo cambio, lo que impidió evaluarlos por separado. La
máquina de estados sobraba; la ventana solo necesitaba un valor razonable.

---

## 8. Estado actual del Animator

| Desde | Hacia | Condición | Exit time | Dur. |
|---|---|---|---|---|
| *Any State* | `Jump_Anticipation` | `JumpTrigger` | no | 0.075 |
| *Any State* | `DoubleJump_Anticipation` | `DoubleJumpTrigger` | no | 0.088 *(offset 0.320)* |
| `Locomotion` | `Jump_GoingDown` | `VerticalVelocity < -0.1 && !IsGrounded` | no | 0.25 |
| `Jump_Anticipation` | `Jump_GoingUp` | `!IsGrounded && VerticalVelocity > 0.2` | no | 0.09 |
| `Jump_Anticipation` | `Jump_GoingDown` | `!IsGrounded && VerticalVelocity < -0.1` | no | 0.08 |
| `Jump_Anticipation` | `Locomotion` | `IsGrounded` *(salvavidas)* | **sí** 0.9 | 0.15 |
| `DoubleJump_Anticipation` | `Jump_GoingUp` | `VerticalVelocity > 0.2` | no | 0.02 |
| `DoubleJump_Anticipation` | `Jump_GoingDown` | `VerticalVelocity < -0.1` | no | 0.08 |
| `Jump_GoingUp` | `Jump_GoingDown` | `VerticalVelocity < -0.1` | no | 0.25 |
| `Jump_GoingDown` | `Jump_End` | `IsAboutToLand` *(sección 6)* | no | 0.08 |
| `Jump_End` | `Locomotion` | *(sin condiciones)* | **sí** 0.9 | 0.2 |
| `Jump_End` | `Locomotion` | `HorizontalSpeed > 0.1` | **sí** 0.4 | 0.15 |

> **No tocar las dos entradas a anticipación.** Sus duraciones (0.075 y 0.088) y el offset de
> 0.320 del doble salto están puestos a mano y a propósito: así es como mejor se ve la animación.
> Se propuso bajarlas a 0.02 por coherencia con el resto y se descartó. Los valores que ya están
> ajustados en el Animator son decisiones, no pendientes.

### Regla: `Has Exit Time` y condiciones no se mezclan

`Has Exit Time` significa "espera **además** a que el clip actual llegue al X% antes de permitir
esta transición". En una transición con condiciones eso produce un retardo de arranque: si el
clip origen está en bucle, la ventana solo aparece una vez por ciclo, así que un trigger lanzado
en el momento equivocado espera a la siguiente vuelta.

Tres transiciones lo tenían activado y causaban retardos medidos de hasta 1.6 s:

| Transición | Exit time | Clip origen | Retardo máximo |
|---|---|---|---|
| `AnyState → DoubleJump_Anticipation` | 0.657 | `Jump_GoingUp`/`GoingDown`, 0.667 s en bucle | ~0.87 s |
| `Locomotion → Jump_GoingDown` | 0.812 | Blend Tree, 1.33 s en bucle | ~1.6 s |
| `Locomotion → JumpSystem` (`JumpTrigger`) | — | — | ~0.68 s (ver abajo) |

A eso se sumaba que **`JumpTrigger` solo era alcanzable desde `Locomotion`**: al saltar justo
después de aterrizar, el trigger quedaba encolado durante los ~0.68 s que `Jump_End` tarda en
volver a `Locomotion` (exit time 0.9 sobre un clip de 0.533 s, más 0.2 s de mezcla).

Se corrigió quitando el exit time de las dos primeras y sustituyendo `Locomotion → JumpSystem`
por una transición desde *Any State*, simétrica con la del doble salto.

### La regla real: exit time sí, pero no en cualquier transición

Quitar el exit time de todo produce el problema opuesto: las poses se **cortan** a los 2-3
frames, porque una transición sin exit time dispara en el instante exacto en que la condición se
vuelve cierta, sin importar cuánto lleve reproducido el clip.

Pasó en `Jump_End → Locomotion` con `HorizontalSpeed > 0.1`: al aterrizar vienes moviéndote
(~5 de velocidad horizontal), así que la condición ya era cierta en el frame 1 y la pose de
aterrizaje no llegaba a verse. Síntoma reconocible: **se veía entera solo si aterrizabas
completamente quieto.**

La pregunta que decide el ajuste:

> ¿Esta transición tiene que dispararse en cuanto la condición sea cierta, o la animación actual
> merece un mínimo de tiempo primero?

| Propósito de la transición | `Has Exit Time` | Si te equivocas |
|---|---|---|
| Reaccionar a un evento: trigger de salto, tocar suelo, empezar a caer | **off** | Retardo de arranque: la animación tarda en enterarse |
| Dejar que la pose actual se vea antes de continuar | **on**, con el mínimo justo | Corte: la pose dura 2-3 frames |

Valores actuales del segundo grupo:

| Transición | Exit time | Tiempo en pantalla |
|---|---|---|
| `Jump_End → Locomotion` (`HorizontalSpeed > 0.1`) | 0.40 | ~0.21 s |
| `Jump_End → Locomotion` (sin condiciones) | 0.90 | ~0.48 s |

### La ventana de anticipación

Las dos anticipaciones **no** usan exit time. Su tiempo en pantalla lo fija el código: al pulsar,
`PlayerMovement` acepta el salto, arranca la animación y **espera antes de aplicar el impulso**.

```
groundJumpAnticipation = 0.12 s   (clip completo: 0.533 s, 16 frames a 30 fps)
airJumpAnticipation    = 0.12 s   (clip completo: 0.567 s, 17 frames a 30 fps)
```

Un solo mando, en el Inspector de `PlayerMovement`. Poner 0 devuelve el salto instantáneo.
Deliberadamente **no** hay exit time en las salidas de anticipación: con dos mandos para lo
mismo, bajar la ventana no surtiría efecto hasta cruzar el exit time.

Durante la ventana el Animator sostiene la pose **sin parámetros extra**, porque la física dice
la verdad: en el salto de suelo Bravard sigue en el suelo, y las dos salidas exigen
`!IsGrounded`. En el salto aéreo hace falta una pieza más — ver abajo.

### El salto aéreo cuelga en el aire

Durante la ventana aérea, `PlayerMovement.FixedUpdate` mantiene la velocidad vertical en cero:

```csharp
if (isAnticipatingJump && !pendingJumpIsFromGround)
    rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
```

No basta con anularla una vez al pulsar: la gravedad la reacelera y a los pocos milisegundos
cruzaría `VerticalVelocity < -0.1`, con lo que el Animator cortaría la pose para pasar a la
caída. Sosteniéndola en cero, el valor se queda en la banda muerta entre -0.1 y 0.2 y la pose
aguanta sola.

`ApplyJumpImpulse()` además pone la velocidad vertical a cero antes del `AddForce`, de modo que
el salto alcanza siempre la misma altura. Eso arregla de paso que el doble salto casi no se
notara al caer rápido: cayendo a -7, un impulso de +7 dejaba a Bravard prácticamente parado.

**La banda muerta importa.** Los umbrales de subida (0.2) y bajada (-0.1) dejan un hueco entre
-0.1 y 0.2 donde ninguna transición dispara. Sin ese hueco —por ejemplo con `> 0.2` y `< 0.2`—
las dos condiciones cubren toda la recta numérica y el estado se abandona en el primer frame
evaluado, que fue justo el bug que cortaba la anticipación.

`Jump_MaxHeight` se eliminó del Animator por decisión de diseño (no se veía como se esperaba).

---

## 9. Verificación

1. `dotnet build Assembly-CSharp.csproj` → 0 errores, 0 advertencias.
2. Con la ventana del Animator abierta en Play:
   - Quieto → `HorizontalSpeed` en 0, Blend Tree en Idle.
   - Caminar → sube suavemente y cruza el umbral 2 antes del 5.
   - Saltar → despega en el mismo frame de la pulsación, sin retardo.
   - **Saltar corriendo, sin soltar la dirección** → el encogimiento se lee **vertical y limpio**,
     no como una diagonal mezclada con la zancada. Mirando `HorizontalSpeed` en la ventana del
     Animator debe **caer hacia 0** al pulsar y volver a subir al despegar, mientras Bravard
     **sigue desplazándose** en la escena. Si todavía se mezcla, bajar `locomotionMuteAttack`.
   - **Aterrizar corriendo** → la recomposición se reproduce entera (~0.48 s) en lugar de
     cortarse a ~0.21 s, y al salir hacia `Locomotion` Bravard llega **ya en carrera**: si se ve
     pasar por Idle y acelerar después, subir `locomotionMuteRelease`.
   - **Poner `locomotionMuteAttack` y `locomotionMuteRelease` a 0** → vuelve el comportamiento
     anterior salvo el corte de pose. Quitarle el Tag a `Jump_End` devuelve el aterrizaje corto.
   - Doble salto en el aire → entra `DoubleJump_Anticipation`.
   - Aterrizar → `Jump_End` entra sin retardo y se ve **desde su primer frame**, sin instante
     congelado previo.
   - Caerse de una plataforma sin saltar → entra `Jump_GoingDown` y aterriza igual que saltando
     (es el camino del duplicado de `Base Layer`).
   - Caída larga → `FallPlaybackSpeed` sube hacia 2 y **se estabiliza ahí**, sin pasarse.
   - Doble salto en plena caída rápida → `FallPlaybackSpeed` vuelve a 1 al subir.
3. Comprobar que el `groundLayer` de `PlayerMovement` apunta a la capa del suelo
   (`m_Bits: 8`). Es la única máscara del sistema.
4. Poner `landingAnticipation` en 0 y `maxFallPlaybackSpeed` en 1: debe volver el comportamiento
   anterior. Es la prueba de que ambos cambios son aditivos y tienen un solo mando cada uno.
5. El gizmo amarillo de la caja de adelanto debe estirarse al acelerar la caída y tocar el suelo
   un instante antes que las patas.

---

## 10. Pendiente

- **La escena de juego no tiene animado a Bravard.** El `Animator` y el `PlayerAnimationBridge`
  existen **solo** en `PruebasAnimacion_Manuel.unity`. Nada de lo descrito aquí se ve en
  `02_GamePlay.unity` hasta que se lleve allí el personaje animado. Es el siguiente paso natural
  del sistema.
- **Anticipación durante el ascenso.** El exit time de 0.25 hace que el encogimiento se
  reproduzca ~0.11 s con el personaje ya despegado. Es deliberado, pero si al pulirlo se ve
  forzado, la alternativa limpia es reanimar el clip a 3-4 frames en lugar de 16, para que quepa
  en el tiempo real que Bravard pasa en el suelo (~0.02 s) y quitar entonces el exit time.
- **Estados duplicados.** `Jump_GoingDown` y `Jump_End` existen dos veces (una copia en
  `Base Layer` para la caída desde borde, otra en `JumpSystem`). **Han derivado**, y la
  diferencia es un bug medido: la corrección de §6 (offset a 0 en `Jump_GoingDown → Jump_End`)
  se aplicó solo a la copia de `JumpSystem`. La del `Base Layer` sigue con **offset 0.369**, así
  que arranca en el frame 5.9 de 16 y **el aterrizaje tardío sigue ocurriendo al caerse de una
  plataforma sin saltar**. Por lo mismo, el silenciado de la locomoción (§5) no tiene efecto en
  ese camino: esa copia no condiciona sobre `HorizontalSpeed`.
  Arreglo mínimo: poner ese offset a 0, un solo campo. Arreglo completo: apuntar la transición
  de `Locomotion` a la copia de `JumpSystem`, tagear su `Jump_End` y borrar las del `Base Layer`.
  Se dejó pendiente a propósito.
- **Saltos gastados en el suelo.** `PlayerMovement.Update` permite gastar los dos saltos
  estando en el suelo: `isGrounded` sigue en `true` uno o dos frames tras saltar.
- **Pausa duplicada.** `GameManager` y `PauseManager` la implementan por separado, ambos con la
  tecla P. `PauseManager` sí toca `Time.timeScale`; `GameManager` no.
