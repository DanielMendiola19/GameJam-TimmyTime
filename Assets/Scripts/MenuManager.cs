using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;
using System.Collections.Generic;

[System.Serializable]
public class LevelData
{
    public string levelID;        // Ej: StoryLevel_001
    public string levelName;      // Nombre manual que quieres mostrar
    public string sceneName;      // Nombre de la escena correspondiente
    public string minGradeToUnlock = "A"; // Para desbloquear siguiente nivel
    public bool needsCinematic = true; // requiere ver video antes de jugar
}

public class MenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject levelSelectPanel;
    public GameObject configPanel; // NUEVO PANEL DE CONFIGURACIONES

    [Header("Level Prefab & Container")]
    public GameObject levelPanelPrefab; // Prefab de nivel
    public Transform levelContainer;    // Contenedor de paneles

    [Header("Levels")]
    public List<LevelData> levels;      // Lista de niveles

    [Header("Stats")]
    public string statsFolder = "Stats/Story";

    public ScrollRect scrollRect;

    void Start()
    {

        // Verificar si debemos mostrar el selector de niveles directamente
        if (PlayerPrefs.GetInt("ShowLevelSelect", 0) == 1)
        {
            PlayerPrefs.SetInt("ShowLevelSelect", 0);
            PlayerPrefs.Save();
            ShowLevelSelect();
        }
        else
        {
            ShowMainMenu();
        }
    }

    public void ResetAllCinematicsSeen()
    {
        foreach (var level in levels)
        {
            PlayerPrefs.DeleteKey("CinematicSeen_" + level.levelID);
        }
        PlayerPrefs.Save();
    }

    // MÉTODOS PARA ACTIVAR/DESACTIVAR PANELES
    public void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        levelSelectPanel.SetActive(false);
        configPanel.SetActive(false); // Asegurar que se cierre
    }

    public void ShowLevelSelect()
    {
        mainMenuPanel.SetActive(false);
        levelSelectPanel.SetActive(true);
        configPanel.SetActive(false); // Asegurar que se cierre

        PopulateLevelPanels();
    }

    // MÉTODO PARA MOSTRAR CONFIGURACIONES
    public void ShowConfigPanel()
    {
        mainMenuPanel.SetActive(false);
        levelSelectPanel.SetActive(false);
        configPanel.SetActive(true); // Activar panel de configuraciones
    }

    // MÉTODO PARA CERRAR CONFIGURACIONES (volver al menú principal)
    public void CloseConfigPanel()
    {
        configPanel.SetActive(false);
        ShowMainMenu();
    }

    private void PopulateLevelPanels()
    {
        // Limpiar contenedor
        foreach (Transform child in levelContainer)
            Destroy(child.gameObject);

        for (int i = 0; i < levels.Count; i++)
        {
            LevelData data = levels[i];
            GameObject panel = Instantiate(levelPanelPrefab, levelContainer);
            LevelPanelUI ui = panel.GetComponent<LevelPanelUI>();

            if (ui == null)
            {
                Debug.LogError("LevelPanelPrefab necesita LevelPanelUI script");
                continue;
            }

            // Recuperar stats del nivel
            LevelStatsWrapper wrapper = LoadStatsWrapper(data.levelID);

            bool unlocked = false;
            if (i == 0)
            {
                unlocked = true; // primer nivel siempre desbloqueado
            }
            else
            {
                LevelStatsWrapper prevWrapper = LoadStatsWrapper(levels[i - 1].levelID);
                if (prevWrapper != null && prevWrapper.stats.Count > 0)
                {
                    string grade = prevWrapper.stats[prevWrapper.stats.Count - 1].grade;
                    unlocked = IsGradeSufficient(grade, levels[i - 1].minGradeToUnlock);
                }
            }

            // ======= NUEVO: Detectar nivel “sin stats” =======
            bool hasStats = wrapper != null && wrapper.stats.Count > 0;
            bool isSpecialLevel = !hasStats && data.levelID.StartsWith("FinalLevel"); // tu criterio

            if (hasStats)
            {
                // Mostrar stats normal
                LevelStats best = wrapper.stats[0];
                foreach (var s in wrapper.stats)
                {
                    if (CompareStats(s, best) > 0)
                        best = s;
                }

                ui.SetLevelInfo(data.levelName, best.grade, best.score, best.accuracy, best.dateTime, unlocked, "");
            }
            else if (isSpecialLevel)
            {
                // Nivel especial: desbloqueado y sin grade
                ui.SetLevelInfo(data.levelName, "-", 0, 0, "-", unlocked, "¡Último nivel!", false);
            }
            else if (unlocked)
            {
                // Nivel desbloqueado pero sin stats
                ui.SetLevelInfo(data.levelName, "-", 0, 0, "-", unlocked, $"Requiere {data.minGradeToUnlock} para avanzar");
            }
            else
            {
                // Nivel bloqueado
                ui.SetLevelInfo("???", "-", 0, 0, "-", unlocked, "Nivel bloqueado");
            }

            int indexCopy = i;
            ui.button.onClick.AddListener(() => PlayLevel(indexCopy));

            // CINEMÁTICA
            bool cinematicSeen = PlayerPrefs.GetInt("CinematicSeen_" + data.levelID, 0) == 1;
            ui.SetCinematicState(!data.needsCinematic || cinematicSeen, unlocked);

            ui.cinematicButton.onClick.AddListener(() =>
            {
                if (!unlocked) return;
                MenuManager.CinematicPass.levelID = data.levelID;
                PlayerPrefs.SetString("PendingLevelAfterCinematic", data.sceneName);
                PlayerPrefs.Save();
                SceneManager.LoadScene("CinematicTemplate");
            });
        }


        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 1f;
        Canvas.ForceUpdateCanvases();
    }

    private bool IsGradeSufficient(string grade, string minGrade)
    {
        string[] order = { "F", "D", "C", "B", "A", "S", "S+" };
        return System.Array.IndexOf(order, grade) >= System.Array.IndexOf(order, minGrade);
    }

    private LevelStatsWrapper LoadStatsWrapper(string levelID)
    {
        string path = Path.Combine(Application.persistentDataPath, statsFolder, levelID + "_stats.json");
        if (!File.Exists(path)) return null;

        try
        {
            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<LevelStatsWrapper>(json);
        }
        catch
        {
            Debug.LogWarning("No se pudo leer stats JSON para " + levelID);
            return null;
        }
    }

    public static class CinematicPass
    {
        public static string levelID;
    }


    private int CompareStats(LevelStats a, LevelStats b)
    {
        string[] order = { "F", "D", "C", "B", "A", "S", "S+" };
        int gradeA = System.Array.IndexOf(order, a.grade);
        int gradeB = System.Array.IndexOf(order, b.grade);

        if (gradeA != gradeB) return gradeA - gradeB;
        return a.score - b.score; // si mismo grade, mayor score gana
    }

    public void PlayLevel(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= levels.Count)
            return;

        SceneManager.LoadScene(levels[levelIndex].sceneName);
    }

    public void BackToMainMenu() => ShowMainMenu();

    public void ChangeScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("ChangeScene: nombre de escena vacío");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Quit Game");
    }
}