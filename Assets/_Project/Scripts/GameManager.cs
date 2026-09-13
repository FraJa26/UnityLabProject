using TMPro;
using UnityEngine;

// Centraliza el estado general del juego (monedas, puntuación, vidas, tiempo,
// victoria/derrota), como se plantea en la Sesión 4: separar "estado del juego"
// de "acciones del jugador" (que vive en PlayerController) y de "presentación" (UI).
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Estado del juego")]
    [SerializeField] private int monedas = 0;
    [SerializeField] private int puntuacion = 0;
    [SerializeField] private int vidas = 3;
    [SerializeField] private int puntosPorMoneda = 10;

    [Header("UI")]
    [SerializeField] private TMP_Text textoMonedas;
    [SerializeField] private TMP_Text textoPuntos;
    [SerializeField] private TMP_Text textoTiempo;
    [SerializeField] private TMP_Text textoVidas;
    [SerializeField] private GameObject panelGameOver;
    [SerializeField] private GameObject panelVictoria;

    [Header("Reaparición")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform puntoDeInicio;

    private float tiempoTranscurrido;
    private bool juegoTerminado;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        ActualizarTextoMonedas();
        ActualizarTextoPuntos();
        ActualizarTextoVidas();
        if (panelGameOver != null) panelGameOver.SetActive(false);
        if (panelVictoria != null) panelVictoria.SetActive(false);
    }

    private void Update()
    {
        if (juegoTerminado) return;

        tiempoTranscurrido += Time.deltaTime;
        if (textoTiempo != null)
        {
            int segundos = Mathf.FloorToInt(tiempoTranscurrido);
            textoTiempo.text = $"Tiempo: {segundos / 60:00}:{segundos % 60:00}";
        }
    }

    public void AgregarMoneda()
    {
        monedas++;
        puntuacion += puntosPorMoneda;
        ActualizarTextoMonedas();
        ActualizarTextoPuntos();
        AudioManager.Instance?.PlaySfxCoin();
    }

    public void PerderVida()
    {
        if (juegoTerminado) return;

        vidas--;
        ActualizarTextoVidas();
        AudioManager.Instance?.PlaySfxHurt();

        if (vidas <= 0)
        {
            MostrarGameOver();
        }
        else if (player != null && puntoDeInicio != null)
        {
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
            player.position = puntoDeInicio.position;
        }
    }

    public void GanarNivel()
    {
        if (juegoTerminado) return;

        juegoTerminado = true;
        if (panelVictoria != null) panelVictoria.SetActive(true);
        AudioManager.Instance?.PlaySfxWin();
        Time.timeScale = 0f;
    }

    private void MostrarGameOver()
    {
        juegoTerminado = true;
        if (panelGameOver != null) panelGameOver.SetActive(true);
        Time.timeScale = 0f;
    }

    public void ReiniciarNivel()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    private void ActualizarTextoMonedas()
    {
        if (textoMonedas != null) textoMonedas.text = $"Monedas: {monedas}";
    }

    private void ActualizarTextoPuntos()
    {
        if (textoPuntos != null) textoPuntos.text = $"Puntos: {puntuacion}";
    }

    private void ActualizarTextoVidas()
    {
        if (textoVidas != null) textoVidas.text = $"Vidas: {vidas}";
    }
}
