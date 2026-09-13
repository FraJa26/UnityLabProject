using UnityEngine;

// Responsabilidad única: leer el estado físico del jugador (PlayerController) y
// traducirlo a parámetros del Animator. Separado de PlayerController para no mezclar
// "cómo se mueve" con "cómo se ve" (principio de arquitectura de la Sesión 4).
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PlayerController))]
public class PlayerAnimator : MonoBehaviour
{
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int VerticalVelocityHash = Animator.StringToHash("VerticalVelocity");

    private Animator animator;
    private PlayerController player;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        player = GetComponent<PlayerController>();
    }

    private void Update()
    {
        animator.SetFloat(SpeedHash, Mathf.Abs(player.HorizontalInput));
        animator.SetBool(IsGroundedHash, player.IsGrounded);
        animator.SetFloat(VerticalVelocityHash, player.Rigidbody.linearVelocity.y);
    }
}
