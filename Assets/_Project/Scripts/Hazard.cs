using UnityEngine;

// Obstáculo que resta una vida al tocar al jugador (p. ej. púas). No destruye ni
// mueve al jugador directamente: delega el "qué pasa después" al GameManager.
[RequireComponent(typeof(Collider2D))]
public class Hazard : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        GameManager.Instance?.PerderVida();
    }
}
