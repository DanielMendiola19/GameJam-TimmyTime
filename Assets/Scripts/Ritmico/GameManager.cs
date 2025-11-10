using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [Header("Game State")]
    public bool isPaused = false;
    public bool gameCompleted = false;
    public bool gameOver = false;

    [Header("UI References")]
    public GameObject pausePanel;
    public GameObject gameOverPanel;
    public GameObject levelCompletePanel;
    public TextMeshProUGUI resultsText;
    public HealthSystem healthSystem;

    [Header("Audio")]
    public AudioSource song;

    public StoryNoteSpawner storySpawner;

    private NoteHitDetector hitDetector;
    private NoteResultManager resultManager;
    private float originalTimeScale;

    void Start()
    {
        hitDetector = FindObjectOfType<NoteHitDetector>();
        resultManager = FindObjectOfType<NoteResultManager>();
        originalTimeScale = Time.timeScale;

        if (pausePanel != null) pausePanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (levelCompletePanel != null) levelCompletePanel.SetActive(false);

        SetCursorForGameplay();
    }

    void Update()
    {
        if (Input.GetKeyDown(SettingsManager.Instance.GetKey("Reiniciar")))
            RestartLevel();

        if (Input.GetKeyDown(SettingsManager.Instance.GetKey("Pausa")))
            TogglePause();
        if (isPaused && Input.GetKeyDown(KeyCode.Backspace)) ResumeGame();

        if (!gameCompleted && !gameOver && song != null && !song.isPlaying && song.time >= song.clip.length - 0.1f)
            LevelComplete();
    }

    // ===================== CURSOR =====================
    private void SetCursorForGameplay()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void SetCursorForUI()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    // ===================== PAUSA =====================
    public void TogglePause()
    {
        if (gameCompleted || gameOver) return;

        isPaused = !isPaused;

        if (isPaused) PauseGame();
        else ResumeGame();
    }

    private void PauseGame()
    {
        Time.timeScale = 0f;
        if (pausePanel != null) pausePanel.SetActive(true);

        if (song != null) song.Pause();
        if (hitDetector != null) hitDetector.canProcessInput = false;

        if (storySpawner != null)
            storySpawner.PauseEverything();

        SetCursorForUI();
        Debug.Log("JUEGO EN PAUSA");
    }

    private void ResumeGame()
    {
        Time.timeScale = originalTimeScale;
        if (pausePanel != null) pausePanel.SetActive(false);

        if (song != null) song.UnPause();
        if (hitDetector != null) hitDetector.canProcessInput = true;

        if (storySpawner != null)
            storySpawner.ResumeEverything();

        isPaused = false;
        SetCursorForGameplay();
        Debug.Log("JUEGO REANUDADO");
    }

    // ===================== GAME OVER =====================
    public void ShowGameOver()
    {
        gameOver = true;
        Time.timeScale = 0f;
        if (gameOverPanel != null) gameOverPanel.SetActive(true);

        if (hitDetector != null) hitDetector.canProcessInput = false;

        if (storySpawner != null)
            storySpawner.StopEverything();

        SetCursorForUI();
        Debug.Log("GAME OVER - TODO DETENIDO");
    }

    // ===================== LEVEL COMPLETE =====================
    private void LevelComplete()
    {
        gameCompleted = true;
        StartCoroutine(ShowLevelComplete());
    }

    private IEnumerator ShowLevelComplete()
    {
        yield return new WaitForEndOfFrame();

        Time.timeScale = 0f;
        if (levelCompletePanel != null) levelCompletePanel.SetActive(true);

        if (hitDetector != null) hitDetector.canProcessInput = false;

        if (storySpawner != null)
            storySpawner.StopEverything();

        if (resultsText != null)
            resultsText.text = CalculateResults();

        StatsManager statsManager = FindObjectOfType<StatsManager>();
        LevelIdentity levelIdentity = FindObjectOfType<LevelIdentity>();
        bool isStoryLevel = levelIdentity != null && levelIdentity.isStoryLevel;

        if (statsManager != null && hitDetector != null)
        {
            LevelStats stats = new LevelStats
            {
                // Asignar levelID correctamente
                levelID = levelIdentity != null ? levelIdentity.levelID : "Custom_" + (song.clip != null ? song.clip.name : "UnknownLevel"),
                levelName = song.clip != null ? song.clip.name : "UnknownLevel",
                perfect = hitDetector.PerfectHits,
                great = hitDetector.GreatHits,
                fail = hitDetector.FailHits,
                miss = hitDetector.MissHits,
                score = hitDetector.CurrentScore,
                maxCombo = hitDetector.MaxCombo,
                accuracy = hitDetector.Accuracy,
                grade = CalculateGrade(hitDetector.Accuracy)
            };

            statsManager.SaveLevelStats(stats, isStoryLevel);
            Debug.Log($"[StatsManager] Estadísticas guardadas para: {stats.levelName} (Story: {isStoryLevel})");
        }

        SetCursorForUI();
    }


    // ===================== RESULTADOS =====================
    private string CalculateResults()
    {
        if (hitDetector == null) return "Results not available";

        int perfect = hitDetector.PerfectHits;
        int great = hitDetector.GreatHits;
        int fail = hitDetector.FailHits;
        int miss = hitDetector.MissHits;
        int score = hitDetector.CurrentScore;
        int maxCombo = hitDetector.MaxCombo;
        float accuracy = hitDetector.Accuracy;
        string grade = CalculateGrade(accuracy);

        return $"NIVEL COMPLETADO!!\n\n" +
               $"Calificacion: {grade}\n" +
               $"Puntuacion: {score}\n" +
               $"Precision: {accuracy:F2}%\n" +
               $"Max Combo: {maxCombo}\n\n" +
               $"Notas: \n" +
               $"Perfect: {perfect}\n" +
               $"Great: {great}\n" +
               $"Fail: {fail}\n" +
               $"Miss: {miss}";
    }

    private string CalculateGrade(float accuracy)
    {
        int totalFails = hitDetector.FailHits + hitDetector.MissHits;
        int totalNotes = hitDetector.PerfectHits + hitDetector.GreatHits + hitDetector.FailHits + hitDetector.MissHits;

        if (accuracy >= 99f && totalFails == 0) return "S+";
        if (accuracy >= 96f && totalFails <= 2) return "S";
        if (accuracy >= 90f && totalFails <= Mathf.CeilToInt(totalNotes * 0.02f)) return "A";
        if (accuracy >= 85f && totalFails <= Mathf.CeilToInt(totalNotes * 0.04f)) return "B";
        if (accuracy >= 75f && totalFails <= Mathf.CeilToInt(totalNotes * 0.06f)) return "C";
        if (accuracy >= 60f && totalFails <= Mathf.CeilToInt(totalNotes * 0.08f)) return "D";
        return "F";
    }

    // ===================== BOTONES DE UI =====================
    public void OnResumeButton() => ResumeGame();
    public void OnRestartButton() => RestartLevel();
    public void OnMainMenuButton() => MainMenu();

    // ===================== LEVEL CONTROL =====================
    public void RestartLevel()
    {
        Time.timeScale = originalTimeScale;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void MainMenu()
    {
        Time.timeScale = originalTimeScale;

        // Guardar en PlayerPrefs que queremos ir directamente al selector de niveles
        PlayerPrefs.SetInt("ShowLevelSelect", 1);
        PlayerPrefs.Save();

        SceneManager.LoadScene("MainMenu");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}