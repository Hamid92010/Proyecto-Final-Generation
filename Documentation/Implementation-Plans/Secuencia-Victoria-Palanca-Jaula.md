# Plan de Implementación: Secuencia de Victoria (Palanca → Jaula)

**Fecha:** 2026-08-25
**Alcance:** Convertir la victoria de un único trigger instantáneo en una secuencia de dos
pasos: activar la palanca, que abre la jaula, y después acercarse a la jaula liberada.

---

## 1. Cómo funciona el WinTrigger HOY

El mecanismo actual es un único paso y está repartido en cuatro archivos:

| # | Dónde | Qué hace |
|---|---|---|
| 1 | Escena `02_GamePlay`, objeto `WinTrigger` (hijo de `Escenario`) | `SphereCollider` con `isTrigger = 1`, radio ≈ 0.99, tag `WinTrigger`. Lleva además un `MeshRenderer`, así que la esfera se ve en el juego. |
| 2 | `PlayerCollisions.OnTriggerEnter` / `OnTriggerExit` | Al detectar el tag `WinTrigger` emite `StateTriggerWin(true/false)`. |
| 3 | `PlayerMovement.StateTriggerWin` | Guarda el estado en el bool `isInWinTrigger`. |
| 4 | `PlayerMovement.Update` | Si `isGrounded && isInWinTrigger` y se pulsa **E** (acción `Interact`): suena el efecto y llama a `gameManager.FinishGame()`. |

`FinishGame()` levanta `gameFinished = true` y emite `OnGameFinished`, al que reaccionan:

- `PlayerMovement.StopMovement` — congela al jugador.
- `PlayerAnimationBridge.TriggerVictory` — anima a Bravard (ya conectado).
- `TimeManager.StopTimer` y `WaterBehaviour.StopWater`.
- `UIManager.LoadVictoryScene` — espera `victoryLapTime` (hoy **5 s**) y carga `04_Victory`.

### Dependencia que hay que respetar

`WaterProximityUI` (Val) llama a `GameManager.SearchPlatformToWin()`, que localiza el objeto
por el tag `WinTrigger` y toma su altura como `yPosToFinishGame` — la meta de la barra de
proximidad al agua. **Borrar o mover ese objeto rompe esa barra.** El plan lo conserva.

---

## 2. Estado actual de los Animators (ya montados, no hay que editarlos)

| Controller | Parámetro | Tipo | Estado por defecto | Destino |
|---|---|---|---|---|
| `AnimatorControlles_Lever` | `IsActivated` | Trigger | `Lever_Inactive` | `Lever_Active` |
| `AnimatorControlles_Jail` | `IsLeverActivated` | Trigger | `Door_Close` | `Door_Open` |
| `Animator_Chicks` | `IsWinning` | Trigger | `Chicks_Idle_` | `Chicks_Victory_Start` → `_Victory_Idle` |
| `Animator_Chicks` | `IsBeingDefeated` | Trigger | — | `Chicks_DefeatedByTime_Start` → `_Idle` |
| `Animator_Bravard` | `IsWinning` + `IsDead=false` | Trigger + Bool | — | `Victory_Start` → `_Victory_Idle` |

Los tres primeros salen de **AnyState**, así que basta con disparar el trigger. Ninguno de
los estados de la palanca ni de la jaula tiene transición de vuelta: una vez abiertos, se
quedan abiertos.

### Dónde vive cada Animator

- **Palanca:** `LeverPlatform.prefab`, en el hijo del modelo `Lever`.
- **Jaula:** `ChicksPlatform.prefab`, en el hijo del modelo `Jail`.
- **Pollitos:** `Chicks.prefab` (dentro de `ChicksPlatform`), sin ningún script asociado.

---

## 3. Flujo objetivo

```
[E sobre el trigger de la palanca]
   └─> LeverInteraction: Animator del Lever ← SetTrigger("IsActivated")
         └─ espera a que el clip Lever_Active termine (se mide en runtime)
              └─> evento OnLeverActivated
                    └─> JailDoor: Animator de la Jail ← SetTrigger("IsLeverActivated")
                          └─ habilita el trigger de victoria de la jaula
                               (1ª condición de victoria cumplida)

[el jugador entra al trigger de la jaula ya abierta]   ← 2ª condición
   └─> GameManager.FinishGame()
         ├─> PlayerAnimationBridge → Bravard IsWinning     (YA HECHO)
         ├─> ChicksAnimationBridge → Chicks IsWinning      (NUEVO)
         └─> UIManager → espera victoryLapTime (→ 3 s) → escena 04_Victory
```

El punto clave del diseño: **la victoria deja de decidirla `PlayerMovement`**. La tecla E
pasa a significar "acciono la palanca", y quien declara la victoria es el trigger de la jaula.

---

## 4. Piezas a implementar

### 4.1 `LeverInteraction.cs` (nuevo — va en `LeverPlatform`)

- Detecta al jugador dentro de su trigger (`OnTriggerEnter` / `OnTriggerExit`).
- Al recibir el aviso de interacción (E), dispara `IsActivated` en su Animator.
- Espera el final del clip **leyendo su duración en runtime**
  (`GetCurrentAnimatorStateInfo(0).length` / `normalizedTime >= 1`), sin Animation Events
  ni `StateMachineBehaviour`: así no hay que tocar ni el controller ni el FBX.
- Emite `OnLeverActivated` al terminar.
- Se acciona **una sola vez**: un bool de guarda evita reactivarla.

### 4.2 `JailDoor.cs` (nuevo — va en `ChicksPlatform`)

- Se suscribe a `LeverInteraction.OnLeverActivated`.
- Dispara `IsLeverActivated` en el Animator de la jaula.
- **Habilita el collider trigger de victoria**, que arranca deshabilitado. Ésta es la
  forma más simple de que la 2ª condición sea imposible antes de la 1ª: si el trigger no
  existe hasta que la puerta se abre, no hay orden que romper.

### 4.3 `VictoryTrigger` — FUSIONADO en `JailDoor`

Al final no se creó como script aparte: el collider de victoria vive en el mismo
GameObject que el Animator de la jaula, así que `JailDoor` puede recibir su
`OnTriggerEnter` directamente. Una pieza menos que colocar en el editor.

Incluye además `OnTriggerStay` como red de seguridad: si el jugador ya estuviera dentro
del área en el instante de habilitarse el collider, la entrada podría no llegar a contarse.

### 4.4 `ChicksAnimationBridge.cs` (nuevo — en `Chicks`)

Equivalente al puente de Bravard, mucho más pequeño:

- `OnGameFinished` → `SetTrigger("IsWinning")`.
- `OnGameOverCaused(Time)` → `SetTrigger("IsBeingDefeated")`.

> El parámetro `IsBeingDefeated` sólo tiene animación de derrota **por tiempo**
> (`Chicks_DefeatedByTime_Start`). Para la derrota por agua no hay clip de pollitos, así
> que ahí no se dispara nada.

### 4.5 Ajuste en `PlayerMovement.cs`

Sustituir la llamada directa a `FinishGame()` por un evento `OnInteract` al que se
suscriba la palanca. Se conservan `isInWinTrigger`, la exigencia de estar en el suelo y el
efecto de sonido; lo único que cambia es **qué** provoca la pulsación.

### 4.6 Ajuste de tiempo

`victoryLapTime` está hoy en **5 s** en la escena. Bajarlo a **3 s** según lo pedido. Es un
valor del Inspector de `UIManager`, no requiere código.

---

## 5. Trabajo de escena / prefabs (fuera del código)

1. **Crear el collider trigger de la jaula** en `ChicksPlatform`. Hoy no existe: los dos
   `BoxCollider` del prefab tienen `isTrigger = 0` (son los sólidos de la plataforma y de
   la jaula). Hay que añadir uno nuevo, marcarlo como trigger y dejarlo **desactivado**.
2. **Situar el trigger de la palanca.** Se reutiliza el objeto `WinTrigger` que ya existe
   (así `WaterProximityUI` conserva su referencia), colocándolo junto a `LeverPlatform`.
3. **Ocultar el `MeshRenderer`** de ese `WinTrigger`: hoy se ve como una esfera en pantalla.

---

## 6. Decisiones pendientes de confirmar

| # | Pregunta | Opción por defecto si no se indica otra cosa |
|---|---|---|
| 1 | ¿El trigger de la jaula gana al entrar, sin pulsar nada? | Sí, automático ("acercarse") |
| 2 | ¿La meta de la barra de agua debe pasar a ser la jaula? | No: se deja apuntando al `WinTrigger` (la palanca) |
| 3 | ¿Los colliders de escena los creo por YAML o se colocan a mano en el editor? | A mano, por precisión de posición y tamaño |
| 4 | ¿La palanca exige estar en el suelo, como hoy? | Sí |

---

## 7. Riesgos identificados

- **Orden de las dos condiciones.** Resuelto habilitando el trigger de la jaula sólo tras
  la animación: no hace falta un bool global que consultar.
- **`FinishGame()` sin guarda.** A diferencia de `TriggerGameOver`, puede llamarse varias
  veces y relanzar la animación de victoria. Se protege en `VictoryTrigger`.
- **Duración del clip de la palanca.** Se mide en runtime; si el clip se sustituye por otro
  más largo, la espera se ajusta sola.
- **La secuencia depende de que el `GameManager` exista en la escena**, igual que el resto
  del proyecto.


---

## 8. ESTADO FINAL — implementado el 2026-08-25

| Pieza | Archivo | Estado |
|---|---|---|
| Palanca | `Assets/Scripts/Manuel/LeverInteraction.cs` | Creado |
| Jaula + victoria | `Assets/Scripts/Manuel/JailDoor.cs` | Creado (incluye el trigger de victoria) |
| Pollitos | `Assets/Animation/.../ChicksAnimationBridge.cs` | Creado |
| La E acciona la palanca | `PlayerMovement.cs` (evento `OnInteract`) | Modificado |
| Delay de victoria | `02_GamePlay` → `victoryLapTime` | 5 s → **3 s** |
| Collider sólido de la jaula | `ChicksPlatform.prefab` | Restaurado |

**Verificado:** compila con 0 errores contra el Roslyn de Unity 6000.3.16f1, y los cuatro
parámetros de Animator usados (`IsActivated`, `IsLeverActivated`, `IsWinning`,
`IsBeingDefeated`) existen en sus controllers con el tipo correcto.

### Pendiente en el editor (no se puede hacer desde el código)

1. `LeverInteraction` → al root de **`LeverPlatform`**.
2. `JailDoor` → al GameObject de la **jaula** (el que tiene el Animator y el BoxCollider trigger).
3. `ChicksAnimationBridge` → al GameObject de **`Chicks`** (el que tiene el Animator).

Los tres resuelven sus referencias solos; no hay nada que arrastrar al Inspector.

---

## 9. Diagnóstico del 2026-08-25: la tecla E no respondía

**Causa:** ninguno de los tres componentes estaba añadido todavía. Búsqueda por GUID en
todas las escenas y prefabs:

| Script | GUID | Dónde aparece |
|---|---|---|
| `LeverInteraction` | `e462f77b8127…` | en ningún sitio |
| `JailDoor` | `dcdc4c713456…` | en ningún sitio |
| `ChicksAnimationBridge` | `fc4ee36f2b57…` | en ningún sitio |

La pulsación sí se registraba: `Interact` sigue mapeada a `<Keyboard>/e`, el `WinTrigger`
conserva su tag y su `SphereCollider` (r ≈ 0.99). Lo que fallaba es que `OnInteract` no
tenía **ningún suscriptor**, así que la llamada se perdía en silencio.

Se añadió un aviso en `PlayerMovement` para que ese caso deje de ser mudo.

### Conflicto de colliders en la jaula (hallazgo asociado)

El `BoxCollider` sólido y el trigger de victoria de la jaula tienen **prácticamente el
mismo tamaño en mundo** (el sólido mide 0.018 en un hijo escalado ×100 ≈ 1.8 u; el trigger
mide 1.95 u). Un sólido tan grande como el trigger impide que el jugador llegue a entrar en
él: la física lo detiene justo en la cara exterior, que es donde empieza el trigger.

**Estado actual:** el collider sólido de la jaula queda **desactivado**
(`m_Enabled: 0`, fileID `3502491947699567033`) para poder seguir probando la secuencia.

**Arreglo definitivo pendiente:** agrandar el trigger de victoria respecto al sólido, de
modo que sea una *zona de aproximación* alrededor de la jaula en vez de coincidir con ella.
Con eso vuelven a convivir las dos cosas que se quieren: malla infranqueable y trigger que
sí se puede pisar.
