using UnityEngine;

// Responsabilidad única: reproducir audio. Ningún otro script conoce AudioClip ni
// AudioSource directamente, solo llama a AudioManager.Instance.PlaySfxX().
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Fuentes de audio")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Clips")]
    [SerializeField] private AudioClip musicClip;
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip coinClip;
    [SerializeField] private AudioClip hurtClip;
    [SerializeField] private AudioClip winClip;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (musicSource != null && musicClip != null)
        {
            musicSource.clip = musicClip;
            musicSource.loop = true;
            musicSource.Play();
        }
    }

    public void PlaySfxJump() => PlayOneShot(jumpClip);
    public void PlaySfxCoin() => PlayOneShot(coinClip);
    public void PlaySfxHurt() => PlayOneShot(hurtClip);
    public void PlaySfxWin() => PlayOneShot(winClip);

    private void PlayOneShot(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }
}
