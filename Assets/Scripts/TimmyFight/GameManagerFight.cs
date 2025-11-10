using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class GameManagerFight : MonoBehaviour
{
    [Header("Game State")]
    public bool isPaused = false;
    public bool gameOver = false;
    public bool levelCompleted = false;

    [Header("UI Panels")]
    public GameObject pausePanel;
    public GameObject gameOverPanel;
    public GameObject victoryPanel;
    public TMP_Text resultsText;

    [Header("Fight Settings")]
    public Image[] lifeImages; // 5 vidas
    public TMP_Text scoreText;
    public int maxGoodHits = 5;
    public int targetScore = 30;

    [Header("Audio")]
    public AudioSource song; // <--- aquí tu canción

    private int goodHits = 0;
    private int score = 0;

    private float originalTimeScale;

    void Start()
    {
        originalTimeScale = Time.timeScale;

        if (pausePanel != null) pausePanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);

        UpdateScoreText();
    }

    void Update()
    {
        // Pausa con ESC
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();

        // Detecta fin del nivel según score SOLO si no está pausado
        if (!gameOver && !levelCompleted && !isPaused)
        {
            if (score >= targetScore)
                ShowVictory();

            // Si la canción termina sin alcanzar objetivo → Game Over
            if (song != null && !song.isPlaying && score < targetScore)
                ShowGameOver("No alcanzaste el objetivo");
        }
    }


    // =================== HIT SYSTEM ===================
    public void HitGoodObject(GameObject obj)
    {
        goodHits++;

        // Quita la vida visual
        if (goodHits - 1 < lifeImages.Length)
            lifeImages[goodHits - 1].enabled = false;

        Destroy(obj);

        // Si supera el máximo permitido → pierde
        if (goodHits >= maxGoodHits)
        {
            ShowGameOver("Has sido desconectado por errores");
        }
    }

    public void HitBadObject(GameObject obj)
    {
        score++;
        UpdateScoreText();
        Destroy(obj);

        if (score >= targetScore)
        {
            ShowVictory();
        }
    }

    private void UpdateScoreText()
    {
        if (scoreText != null)
            scoreText.text = $"Score: {score}/{targetScore}";
    }

    // =================== PAUSA ===================
    public void TogglePause()
    {
        if (gameOver || levelCompleted) return;

        isPaused = !isPaused;
        if (isPaused) PauseGame();
        else ResumeGame();
    }

    private void PauseGame()
    {
        Time.timeScale = 0f;
        if (pausePanel != null) pausePanel.SetActive(true);
        if (song != null) song.Pause(); // detiene la canción
    }

    public void ResumeGame()
    {
        Time.timeScale = originalTimeScale;
        if (pausePanel != null) pausePanel.SetActive(false);
        isPaused = false;
        if (song != null) song.UnPause(); // reanuda la canción
    }

    // =================== GAME OVER ===================
    public void ShowGameOver(string reason = "Perdiste")
    {
        gameOver = true;
        Time.timeScale = 0f;

        if (song != null) song.Pause(); // detiene la canción

        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (resultsText != null)
            resultsText.text = reason;
    }

    // =================== VICTORIA ===================
    public void ShowVictory()
    {
        levelCompleted = true;
        Time.timeScale = 0f;

        if (song != null) song.Pause(); // detiene la canción

        if (victoryPanel != null) victoryPanel.SetActive(true);
        if (resultsText != null)
            resultsText.text = $"¡Nivel completado!\nScore final: {score}/{targetScore}";
    }

    // =================== BOTONES UI ===================
    public void OnRestartButton()
    {
        Time.timeScale = originalTimeScale;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnMainMenuButton()
    {
        Time.timeScale = originalTimeScale;
        PlayerPrefs.SetInt("ShowLevelSelect", 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene("MainMenu");
    }

    public void GoToFinalCinematic()
    {
        // Esto te permite llamar directo desde un botón del UI
        // o desde donde quieras para cargar la última cinemática

        SceneManager.LoadScene("FinalCinematica");
    }

}
