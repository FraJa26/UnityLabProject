# UnityLabProject

Proyecto 2D de laboratorio (Unity 6000.6.0f1, Built-in Render Pipeline / 2D Core) creado para practicar:
control de versiones con Git, construcción de un escenario con física 2D, y programación de un
`PlayerController` con buen *game feel*.

## Estructura del proyecto

```
Assets/
  _Project/
    Scripts/    -> PlayerController.cs, Collectible.cs
    Sprites/    -> primitivas 2D generadas (square.png, circle.png)
    Scenes/     -> Lab.unity (escena de pruebas)
    Prefabs/    -> (reservado para prefabs futuros)
    Editor/     -> LabSceneBuilder.cs (herramienta de editor para regenerar la escena)
```

## Escena "Lab"

Contiene:
- Piso, dos paredes laterales y una plataforma flotante (colliders estáticos).
- `PushableBox`: caja con `Rigidbody2D` dinámico que el jugador puede empujar.
- `Coin`: coleccionable con `Collider2D` en modo Trigger que se destruye al tocarlo.
- `Player`: `Rigidbody2D` + `BoxCollider2D` + `PlayerController.cs`.

Para regenerar la escena desde cero: menú **Lab > Build Test Room Scene** en el Editor.

## Controles

- Mover: flechas / A-D (eje `Horizontal`)
- Saltar: barra espaciadora (botón `Jump`), solo si está tocando el suelo.

Ver [`BITACORA.md`](./BITACORA.md) para la documentación de valores de *game feel* y el uso de IA.
