using UnityEngine;

// Sigue el patrón de la Sesión 4: el coleccionable conoce al GameManager,
// dispara su propio efecto de partículas y se destruye a sí mismo.
[RequireComponent(typeof(Collider2D))]
public class Coin : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private GameObject efectoRecogida;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        GameManager.Instance?.AgregarMoneda();

        if (efectoRecogida != null)
        {
            Instantiate(efectoRecogida, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}
