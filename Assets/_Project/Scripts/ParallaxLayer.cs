using UnityEngine;

// Fondo con parallax simple: se mueve más lento que la cámara para dar sensación
// de profundidad. factor 0 = fijo a la cámara, 1 = se mueve igual que el mundo.
public class ParallaxLayer : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float parallaxFactor = 0.3f;

    private Vector3 lastCameraPosition;

    private void Start()
    {
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
        if (cameraTransform != null) lastCameraPosition = cameraTransform.position;
    }

    private void LateUpdate()
    {
        if (cameraTransform == null) return;

        Vector3 delta = cameraTransform.position - lastCameraPosition;
        transform.position += new Vector3(delta.x * parallaxFactor, delta.y * parallaxFactor, 0f);
        lastCameraPosition = cameraTransform.position;
    }
}
