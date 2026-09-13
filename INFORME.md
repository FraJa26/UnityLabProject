# Informe breve — Nivel jugable (Unidad 2)

## Objetivo

Continuar el prototipo de plataformas 2D (Unidad 1: `PlayerController`, laboratorio de física)
y convertirlo en un recorrido jugable completo, aplicando Tilemaps, cámara, animaciones, UI,
audio, partículas y una arquitectura basada en componentes (Sesiones 3 y 4).

## Continuidad respecto al entregable anterior

Se mantiene íntegro el `PlayerController.cs` original (movimiento por `GetAxisRaw`, salto
solo-en-suelo, gravedad variable) y la escena `Lab.unity` como banco de pruebas. Se agrega el
cambio de orientación del sprite (requisito que faltaba) y getters de solo lectura para que
`PlayerAnimator` lea su estado sin acoplarse a él. El nuevo nivel jugable vive en una escena
separada, `Level1.unity`.

## Mapeo de requisitos

| # | Requisito | Dónde |
|---|---|---|
| 2-3 | Escenario con Tilemap: suelo/plataformas, fondo, obstáculos, coleccionables | `LevelBuilder.cs` pinta `Tilemap_Ground` (Tile+Tilemap+TilemapCollider2D+CompositeCollider2D); fondo `Background_Sky`/`Background_Hills` con parallax; 2 púas (`Hazard.cs`); 11 monedas (`Coin.cs`) |
| 4 | Movimiento, orientación, salto, física/colisiones | `PlayerController.cs` (existente + flip de sprite nuevo) |
| 5 | Cámara que sigue al jugador | `CameraFollow.cs` (SmoothDamp + límites del nivel) |
| 6-7 | Animator con ≥3 estados | `PlayerAnimator.cs` + `PlayerAnimator.controller`: Idle/Run/Jump/Fall, transiciones por `Speed`/`IsGrounded`/`VerticalVelocity` |
| 8 | Coleccionable | `Coin.cs`: suma monedas y puntos, dispara partícula y SFX, se destruye |
| 9 | UI (monedas, puntos, vidas, tiempo) | Canvas + TextMeshPro, armado en `LevelBuilder.CreateUI` |
| 10 | Audio (música + efectos) | `AudioManager.cs` + 5 clips generados por síntesis (`Assets/_Project/Audio`) |
| 11 | Scripts con responsabilidades separadas | Ver arquitectura abajo |
| 12 | Recorrido con inicio, desarrollo y final | `Level1`: inicio en x=0, staircases/huecos progresivos, `Goal.cs` en la bandera final (~x=60) |
| 13 | Ejecuta desde la ventana Game | Verificado por inspección de la escena generada (ver más abajo); pendiente de confirmación jugando en el Editor |

Partículas (punto adicional de la Sesión 4): `CoinSparkleFX.prefab`, instanciado por `Coin.cs` al recoger.

## Arquitectura

Siguiendo el patrón de la Sesión 4 (evitar meter toda la lógica en `Player.cs`):

```
GameManager      -> estado del juego: monedas, puntos, vidas, tiempo, Game Over/Victoria, UI
AudioManager     -> reproduce música y efectos (nadie más toca AudioSource directamente)
PlayerController -> movimiento, salto, orientación (ya existía)
PlayerAnimator   -> traduce el estado físico del jugador a parámetros del Animator
Coin / Hazard / Goal -> cada uno resuelve su propio trigger y delega el efecto al GameManager
CameraFollow / ParallaxLayer -> cámara y fondo
```

## Decisiones técnicas relevantes

- **Diseño del nivel calculado, no a ojo.** Con `Gravity Scale 4.5` y `Jump Force 14` (heredados
  del laboratorio), la altura máxima real de salto es `14² / (2·4.5·9.81) ≈ 2.22` unidades. Cada
  desnivel del `Level1` se limitó a **1 tile de subida** (margen ~10%) tras la lección aprendida en
  el laboratorio, donde una plataforma quedó inalcanzable por no hacer este cálculo.
- **Tilemap real**, no GameObjects sueltos: `TilemapCollider2D` + `CompositeCollider2D` +
  `Rigidbody2D` estático, tal como lo describe la Sesión 3.
- **Arte y audio generados por código.** No se importaron assets externos; los sprites (tiles,
  personaje, púas, bandera, fondo) y los 5 clips de audio se generan proceduralmente vía scripts
  de Editor / síntesis de ondas. Esto prioriza que los sistemas (Tilemap, Animator, Audio, UI)
  queden correctamente integrados y funcionando, aunque el resultado visual/sonoro sea sencillo.

## Uso de IA (Claude)

Se usó Claude Code para generar y depurar todo el código de esta entrega a partir del enunciado
de la actividad y los PDF de las 4 sesiones del curso (se extrajo su contenido con `pdftotext`
para alinear nombres de clases, patrones y API con lo enseñado, p. ej. `GameManager.AgregarMoneda()`,
`OnTriggerEnter2D`, `TMP_Text`, `AudioSource.PlayOneShot`).

La construcción de la escena se automatizó con un script de Editor (`LevelBuilder.cs`) ejecutado
vía `Unity.exe -batchmode -executeMethod`, en vez de armarla a mano — el mismo enfoque ya usado
en el laboratorio de la Unidad 1. Al agregar TextMeshPro fue necesario añadir la dependencia
`com.unity.ugui` al `manifest.json` (no estaba en el proyecto base); tras eso la escena compiló y
se generó sin errores en el primer intento.

## Estado del repositorio

Repositorio: **https://github.com/FraJa26/UnityLabProject**

Commits de esta entrega (además de los 4 de la Unidad 1):
- `Agrega arquitectura de sistemas: GameManager, AudioManager y gameplay`
- `Genera audio sintetizado y sprites procedurales para el nivel`
- `Arma el nivel jugable Level1: Tilemap, UI, Animator y meta`

**Pendiente de verificación manual:** jugar `Level1.unity` en el Editor (botón Play) para
confirmar sensación de salto en el nivel completo y que la UI/audio/partículas se vean y
escuchen como se espera — la generación automática se validó estructuralmente (componentes,
referencias, capas) pero no sustituye una prueba jugada.
