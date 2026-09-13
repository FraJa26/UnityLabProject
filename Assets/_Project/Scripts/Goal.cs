using UnityEngine;

// Marca el final del recorrido jugable: al tocarla, el jugador gana el nivel.
[RequireComponent(typeof(Collider2D))]
public class Goal : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        GameManager.Instance?.GanarNivel();
    }
}
