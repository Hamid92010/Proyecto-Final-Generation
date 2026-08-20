# Plan de Implementación — Fondo con tiling y máscara circular sobre las rejas

**Fecha:** 2026-08-20
**Alcance:** Los dos planos de fondo (`BG_Wood` y `FG_MetalGrid`) y el agujero circular que
deja ver a Bravard a través de las rejas.
**Assets afectados:**
`Assets/Art/Shaders/SG_BG_Texture.shadergraph`,
`Assets/Art/Shaders/Mat_Shaders/M_Wood.mat`, `Assets/Art/Shaders/Mat_Shaders/M_MetalGrid.mat`,
`Assets/Scripts/Manuel/Shaders/MathematicalMask.cs`, `Assets/Scripts/Manuel/Shaders/BG_FollowPlayer.cs`

**Convención de nomenclatura:** identificadores en **inglés**; comentarios y documentación en
**español**.

---

## 1. Principio rector

> El agujero lo dibuja el **shader**, no el motor. El script solo aporta el dato que el
> shader no puede conocer por su cuenta: **dónde está el centro del agujero sobre el plano
> de las rejas**.

Corolario: el centro **no** es la posición del jugador. Es su **proyección** sobre ese plano
vista desde la cámara. Confundir ambas cosas es el fallo del que cuelgan casi todos los demás.

---

## 2. Cómo está montado

| Objeto | Z en mundo | Material | Papel |
|---|---|---|---|
| `BG_Wood` | 4.25 | `M_Wood` (tiling 0.23) | Madera de fondo, detrás del jugador |
| `Player` | 3.44 (Z congelada) | — | Bravard |
| `FG_MetalGrid` | −0.55 | `M_MetalGrid` (tiling 0.32, transparencia 0.66) | Rejas, delante del jugador |
| `FG_Spotlight` | −1 | `M_Spotlight` | Velo oscuro con foco difuminado, delante de todo |
| `Main Camera` | −6 | — | Perspectiva, FOV 60 |

Los dos planos son `SpriteRenderer` que comparten **el mismo Shader Graph**, `SG_BG_Texture`
(target URP · Sprite Lit). El grafo hace dos cosas independientes:

```
Position(World).xy ─ Tiling And Offset ─ Sample Texture 2D ─ BaseColor
                          │                       └─ A ──┐
                       _Tiling                           ├─ × _Transparency ─┐
                                                         │                   ├─ × ─ Alpha
Position(World) ──┐                                                          │
                  ├─ Distance ─ Step(Edge = _Mask_Radius) ────────────────────┘
_Player_Position ─┘
```

- **Tiling:** las UV no salen de las UV del sprite sino de la **posición de mundo** del píxel.
  Por eso la textura se queda quieta en el mundo mientras `BG_FollowPlayer` mueve el quad con
  la Y del jugador, y se ve un fondo infinito que scrollea. El script y el shader son la misma
  pieza: si el quad deja de moverse, el fondo deja de scrollear.
- **Máscara:** `Step` devuelve 0 cuando la distancia es menor que el radio (píxel transparente,
  agujero) y 1 fuera (píxel opaco, rejas).

---

## 3. Diagnóstico: por qué no funcionaba

| # | Problema | Consecuencia |
|---|---|---|
| 1 | `MathematicalMask` **no estaba en la escena** (`PruebasAnimacion_Manuel.unity`, guardada 12:22; el script se creó 12:45). Es el único que escribe `_Player_Position`. | El centro del agujero se quedaba en el origen del mundo. |
| 2 | El script pedía `Shader.PropertyToID("Player_Position")`, pero el *Reference* del grafo es **`_Player_Position`**, con guion bajo. `SetVector` con un nombre inexistente **no lanza error, no hace nada**. | Aunque el componente estuviera puesto, el shader nunca recibía el dato. |
| 3 | El script enviaba `transform.position` — la del GameObject que lo lleva —, no la del jugador. | Si se colgaba de las rejas, mandaba la posición de las rejas. |
| 4 | El nodo `Distance` trabaja en **3D**. El jugador está en Z 3.44 y las rejas en Z −0.55: casi **4 unidades** de separación, contra un `_Mask_Radius` de 2. | `Step` daba 1 en **todos** los píxeles. El agujero no aparecía jamás, ni arreglando 1–3. |
| 5 | **Paralaje.** Con cámara en perspectiva, el punto de las rejas que se ve "encima" de Bravard no es su XY. El factor aquí es (−0.55 − (−6)) / (3.44 − (−6)) = **0.577**. Con el jugador en X −6.06 y la cámara en X 1, el agujero debería centrarse en X −3.08, no en −6.06: **3 unidades de desfase.** | El agujero se despegaría del personaje en cuanto se alejara del centro de pantalla. |
| 6 | Los `.mat` guardaban la textura bajo `_ImageTexture`, pero la propiedad del grafo se llama **`_Image_Texture`** (el grafo se editó a las 12:47, después de guardar los materiales a las 10:40 y 11:31). | Ranura de textura vacía → los planos se ven en blanco. |
| 7 | `M_Wood` **comparte shader** con las rejas y hereda `_Mask_Radius` = 2 por defecto, con su `_Player_Position` en (0,0,0). | Bomba de relojería: se salvaba solo porque la madera está a 4.25 del origen. Con un radio mayor, agujero en mitad de la madera. |
| 8 | Sistema de máscara **duplicado**: `FG_PlayerMask` (SpriteMask + `Mask_FollowPlayer`) con `FG_MetalGrid` en *Visible Outside Mask*, compitiendo con la máscara del shader. Además el SpriteMask está pensado para el **Renderer 2D** de URP y el proyecto usa el **Universal Renderer** (3D), y su *Mask Alpha Cutoff* estaba en 1. | Dos sistemas peleándose por el mismo píxel, y el basado en stencil no es fiable en este pipeline. |

El punto de fondo son el **4** y el **5**: la máscara vive en un plano distinto al del personaje,
así que hay que **llevar el centro a ese plano** antes de medir distancias.

---

## 4. La proyección

Se busca el punto `C` del plano de las rejas alineado con el jugador desde el punto de vista
de la cámara. Es la intersección de la línea de visión con el plano:

```
Perspectiva:   origen = posición de la cámara,  dirección = jugador − cámara
Ortográfica:   origen = posición del jugador,   dirección = forward de la cámara

d = −(dot(n, origen) + plane.distance) / dot(n, dirección)      con n = normal del plano
C = origen + dirección · d
```

Se interseca como **recta**, no como rayo, para que dé igual que el plano quede delante o
detrás del origen.

Esto resuelve dos problemas de una vez:

1. **Paralaje:** `C` cae exactamente donde la cámara ve a Bravard sobre las rejas.
2. **Eje Z:** `C` queda **sobre el plano**, con la misma Z que todos los píxeles del sprite.
   El nodo `Distance`, aunque sea 3D, pasa a medir la distancia **dentro del plano**. No hace
   falta tocar el grafo.

Consecuencia de diseño: `_Mask_Radius` se mide **sobre el plano de las rejas**, no a la altura
del personaje. Como el plano está más cerca de la cámara, un radio de 2 se ve en pantalla como
2 / 0.577 ≈ 3.5 unidades medidas a la profundidad de Bravard. Es un número que se ajusta a ojo.

---

## 5. Cambios aplicados

### `MathematicalMask.cs` — reescrito

- Referencias explícitas: `maskedRenderer`, `playerTransform`, `viewCamera` (con autorrelleno:
  el `Renderer` del propio GameObject y `Camera.main`).
- Nombre correcto de la propiedad: **`_Player_Position`**, y `Mask_Radius` opcional.
- Proyección de la sección 4, con interruptor `compensateParallax`.
- Se actualiza en **`RenderPipelineManager.beginCameraRendering`** y no en `LateUpdate`: cuando
  salta ese evento ya se movieron el jugador **y** la cámara, así que no hay el fotograma de
  retraso que haría temblar el agujero cuando la cámara persigue al personaje. Se filtra por
  cámara para no recalcular en la vista de Scene ni en las previews.
- Comprueba con `HasVector` que el material expone la propiedad y avisa por consola si no. Es
  la red de seguridad contra el fallo silencioso del punto 2 del diagnóstico.
- Usa `Renderer.material` (copia propia, para no contaminar el asset compartido con la madera)
  y la destruye en `OnDestroy`.

### `BG_FollowPlayer.cs`

Comportamiento intacto — solo guarda contra `playerTransform` nulo y documenta por qué mover
el quad es lo que hace scrollear la textura.

### `M_Wood.mat` y `M_MetalGrid.mat`

- `_ImageTexture` → **`_Image_Texture`**, para volver a enganchar la textura al nombre que el
  grafo espera hoy.
- `M_Wood` fija **`_Mask_Radius: 0`**. Con radio 0, `Step` devuelve 1 en todo el plano y la
  madera nunca se agujerea, sea cual sea el radio que se le ponga a las rejas.

---

## 6. Montaje en la escena

Todo esto ya está aplicado en `PruebasAnimacion_Manuel.unity`:

| Paso | Estado |
|---|---|
| `FG_MetalGrid` lleva el componente `MathematicalMask` | Aplicado |
| — *Masked Renderer* → su propio `SpriteRenderer` | Aplicado |
| — *Player Transform* → la instancia de `Player` | Aplicado |
| — *View Camera* → `Main Camera` | Aplicado |
| — *Compensate Parallax* activado, *Mask Radius* en −1 (manda el material) | Aplicado |
| `FG_MetalGrid` → SpriteRenderer → *Mask Interaction*: `None` | Aplicado |
| `FG_PlayerMask` eliminado de la escena | Aplicado |
| Textura enganchada como `_Image_Texture` en los dos materiales | Aplicado |

**Nota sobre la referencia al jugador.** `Player` es una instancia del prefab
`Assets/Prefabs/Manuel/Player.prefab`, así que en el archivo de escena su transform aparece
como referencia *stripped* (`&215622166 stripped`). Es lo normal y las referencias siguen
resolviendo: apuntan ahí `CameraFollow`, los dos `BG_FollowPlayer` y el `MathematicalMask`.
Si algún día se cambia el prefab por otro, hay que reasignar los cuatro.

Lo único que queda es **ajustar a ojo el radio**: `M_MetalGrid` → *Mask Radius*, que arranca
en 2. Se toca en Play hasta que el círculo enmarque a Bravard con holgura. Recuerda que se
mide sobre el plano de las rejas (sección 4), así que se ve más grande de lo que dice el
número.

---

## 7. Qué se descarta y por qué

El `SpriteMask` (`FG_PlayerMask`) hacía el mismo trabajo por stencil. Se descarta:

- El enmascarado por `SpriteMask` está pensado para el **Renderer 2D** de URP; este proyecto
  renderiza con el **Universal Renderer** (3D), donde no es fiable.
- El SpriteMask es un objeto en el mundo, así que **también sufre el paralaje**: al estar a otra
  Z que las rejas, su círculo se proyecta con otro tamaño y otra posición.
- Su *Mask Alpha Cutoff* estaba en 1, el extremo del rango.
- Y sobre todo: dos sistemas discutiendo por el mismo píxel son imposibles de depurar.

La máscara del shader no depende del pipeline, no añade objetos a la escena y el radio es un
número, no una escala de transform.

---

## 8. Mejora opcional: borde suave

`Step` da un canto duro y con dientes de sierra. Para un degradado, en el grafo:

1. Sustituir el nodo **`Step`** por **`Smoothstep`**.
2. `In` ← salida de `Distance` (igual que ahora).
3. `Edge 1` ← `_Mask_Radius` menos el grosor del degradado (o una propiedad nueva
   `_Mask_Softness` restada con un nodo `Subtract`).
4. `Edge 2` ← `_Mask_Radius`.

El sentido es el mismo que el de `Step` — 0 dentro, 1 fuera — así que el resto del grafo no se
toca. Un degradado de 0.3–0.5 unidades basta para quitar el aliasing sin que el agujero pierda
forma.

---

## 9. El velo de foco (`FG_Spotlight`)

Segunda máscara, con el mismo motor pero **borde difuminado**: un sprite oscuro y
semitransparente que cubre la pantalla y se abre en un círculo suave alrededor de Bravard.
Es la idea de la sección 8, pero en un asset propio para no tocar el shader de las rejas,
que ya funciona.

### Shader `Scene/SpotlightMask`

Escrito a mano (`Assets/Art/Shaders/SpotlightMask.shader`) en vez de en Shader Graph. La
lógica es la misma que la de las rejas salvo el corte:

```
rejas:  alpha = texturaA · _Transparency · step(_Mask_Radius, distancia)
velo:   alpha = _Overlay_Color.a · smoothstep(_Mask_Radius, _Mask_Radius + _Mask_Softness, distancia)
```

`smoothstep(a, b, x)` vale 0 por debajo de `a`, 1 por encima de `b` y hace una rampa suave
entre medias. Traducido: `_Mask_Radius` es el radio del círculo **limpio del todo**, y
`_Mask_Softness` la anchura de la corona en la que el velo va apareciendo.

| Propiedad | Por defecto | Qué hace |
|---|---|---|
| `_Overlay_Color` | (0, 0, 0, 0.8) | Color del velo. **El alfa manda la oscuridad.** |
| `_Image_Texture` | blanco | Opcional. Se multiplica por el color, para un velo con textura en vez de plano. |
| `_Mask_Radius` | 1 | Radio del círculo completamente limpio. |
| `_Mask_Softness` | 1.5 | Anchura del degradado. A 0 el borde vuelve a ser duro. |
| `_Player_Position` | — | Lo escribe `MathematicalMask`. No se toca a mano. |

El shader reutiliza **los mismos nombres de propiedad** que el grafo de las rejas
(`_Player_Position`, `_Mask_Radius`), así que el componente `MathematicalMask` funciona sobre
él sin cambiar ni una línea de código.

Notas de implementación: `Blend SrcAlpha OneMinusSrcAlpha` con `ZWrite Off` — es un velo, no
geometría. Y `FallBack Off` a propósito: sin eso heredaría los pases de sombra y profundidad
de `Lit` y el velo acabaría proyectando sombra sobre la escena.

### Montaje

`FG_Spotlight`, objeto raíz de la escena, con:

- **SpriteRenderer** con `M_Spotlight`, capa de sorting `FG` y **order 10** (las rejas van en
  3, así que el velo queda por delante de todo).
- **`BG_FollowPlayer`** con `offsetY: 2`, que es justo el offset de `CameraFollow`: así el velo
  queda siempre centrado en la cámara.
- **`MathematicalMask`** apuntando a su propio renderer, al jugador y a la `Main Camera`.
- Transform en Z −1, delante de las rejas (−0.55).

### Cómo se traducen los números a pantalla

El velo está a 5 unidades de la cámara, que con FOV 60 ve ahí una **media altura de 2.89
unidades**. Con los valores por defecto:

- `_Mask_Radius` 1 → círculo limpio de un **35 %** de la media altura de pantalla.
- Difuminado completo a 2.5 → **87 %**, o sea que las esquinas quedan oscuras del todo.

Ese es el motivo de que el radio del velo (1) sea numéricamente menor que el de las rejas (2)
y aun así el foco no sea diminuto: el velo está más cerca de la cámara, así que cada unidad
cunde más. Si mueves el plano en Z, tendrás que reajustar el radio.
