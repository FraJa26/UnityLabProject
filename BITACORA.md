# Bitácora Digital — Laboratorio de Física 2D en Unity

## Misión 4: Reflexión

### 1. Valores usados y justificación (Game Feel)

**Rigidbody2D — Player**

| Propiedad | Valor | Por qué |
|---|---|---|
| Mass | 1 | Valor base neutro; el "peso" se logra con Gravity Scale, no con la masa. |
| Gravity Scale | 4.5 | El valor por defecto (1) se siente como flotar en el espacio. Multiplicarlo por ~4-5 hace que el personaje caiga rápido y se sienta sujeto al suelo, típico de plataformeros clásicos (Mario, Celeste). |
| Linear Drag (Linear Damping) | 0 | El movimiento horizontal se controla asignando `linearVelocity.x` directamente cada `FixedUpdate` (no aplicando fuerzas), así que el drag no frena el desplazamiento lateral; en cambio, si fuera distinto de 0, amortiguaría también la velocidad vertical del salto de forma impredecible. La respuesta instantánea de `GetAxisRaw` ya elimina la sensación de "inercia flotante" sin necesidad de drag. |
| Constraints | Freeze Rotation Z | Evita que el jugador caiga girando como un dado al chocar con la caja u otros colliders. |
| Collision Detection | Continuous + Interpolate | Evita que a alta velocidad el jugador atraviese el piso o la plataforma, y suaviza el render entre steps de física. |

**Variables del script `PlayerController.cs`**

| Variable | Valor | Por qué |
|---|---|---|
| `moveSpeed` | 8 | Suficientemente rápido para cruzar la sala de pruebas en ~1-2s sin sentirse lento, pero controlable. |
| `jumpForce` | 14 | Con Gravity Scale 4.5, este impulso da una altura de salto de ~1.5-2 unidades: suficiente para subir a la plataforma flotante sin que el salto se sienta "flojo". |
| `fallGravityMultiplier` | 2.5 | Truco clásico de *game feel* ("more gravity when falling"): la caída es más rápida que la subida, lo que da sensación de peso y evita el "flotar" en el pico del salto. |
| `lowJumpGravityMultiplier` | 2 | Permite salto de altura variable: si el jugador suelta el botón antes de tiempo, el personaje cae antes en vez de completar siempre la altura máxima. Da control fino, como en los plataformeros de referencia (Celeste, Hollow Knight). |
| `groundCheckRadius` | 0.15 | Lo bastante pequeño para no detectar falso positivo contra paredes verticales cercanas, lo bastante grande para no fallar por 1 frame de separación numérica. |

**Rigidbody2D — PushableBox**

| Propiedad | Valor | Por qué |
|---|---|---|
| Mass | 2 | El doble que el jugador: se puede empujar, pero cuesta un poco de esfuerzo notarlo, no sale disparada. |
| Gravity Scale | 1 | No necesita caer "pesado" como el jugador, solo apoyarse en el piso de forma normal. |
| Linear Drag | 0.5 | Sin este drag, la caja seguiría deslizando indefinidamente tras un empujón (fricción por defecto insuficiente en 2D). Con 0.5 se frena en poco tiempo, como un objeto real con fricción contra el piso. |

Proceso: los valores no se adivinaron a la primera. Se partió de los valores por defecto de Unity
(Gravity Scale 1, Drag 0), se probó, se sintió "flotante", y se fue subiendo Gravity Scale de 1 en 1
hasta que la caída se sintió "de plataformas clásico" sin llegar a ser tan brusca que el salto se
sintiera inútil.

### 2. Uso de IA (Claude)

**Prompt usado** (resumen del original dado a Claude Code, en español): se pidió completar 4 misiones
de un ejercicio de laboratorio en Unity 2D: (1) crear proyecto, iniciar Git y conectarlo a GitHub con
estructura de carpetas profesional y al menos 3 commits; (2) construir una escena de pruebas con
piso/paredes/plataformas, un obstáculo físico empujable y una moneda coleccionable con trigger;
(3) programar `PlayerController` con movimiento por `GetAxisRaw`, salto solo-en-suelo, y ajustar
Gravity Scale/Mass/Drag/fuerza de salto para lograr un movimiento "pesado y responsivo"; (4)
documentar todo en esta bitácora, incluyendo el prompt de IA usado.

**Cómo se adaptó la respuesta:**
- Claude generó el proyecto y la escena de forma **automatizada** (no manual en el Editor): se
  escribió un script de Editor (`LabSceneBuilder.cs`) que crea las primitivas 2D por código
  (texturas de cuadrado y círculo generadas en tiempo de ejecución), arma la sala, agrega los
  componentes de física y guarda la escena. Esto se ejecutó con
  `Unity.exe -batchmode -executeMethod LabSceneBuilder.BuildScene`.
- La primera compilación falló con `error CS0246: The type or namespace name 'Scene' could not be
  found` porque faltaba `using UnityEngine.SceneManagement;`. Se depuró leyendo el log de compilación
  de Unity (`-logFile`), se agregó el `using` faltante y se volvió a ejecutar hasta compilar sin errores.
- El salto con gravedad variable (`fallGravityMultiplier` / `lowJumpGravityMultiplier`) fue una
  sugerencia de Claude para cumplir mejor el requisito de "game feel" de la Misión 3; se mantuvo
  porque al probarlo en el Editor la caída se sintió notablemente menos flotante que con gravedad
  constante.
- Los valores numéricos finales (Gravity Scale 4.5, jumpForce 14, etc.) se dejaron expuestos como
  `[SerializeField]` en el Inspector precisamente para poder ajustarlos a mano y "sentir" el
  resultado, en vez de dejarlos como constantes fijas en el código.

### 3. Commits realizados

Ver historial de Git (`git log`). Como mínimo:
1. Estructura inicial del proyecto Unity + `.gitignore`.
2. Escenario de pruebas con física (piso, paredes, plataforma, caja empujable, moneda).
3. `PlayerController` con movimiento, salto y ajuste de game feel.
