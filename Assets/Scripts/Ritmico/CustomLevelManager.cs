using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class CustomLevelManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject levelPanelPrefab;
    public Transform levelContainer;
    public ScrollRect scrollRect;
    public TMP_Text noLevelsText;

    [Header("Settings")]
    public string gameplaySceneName = "Nivel_Custom";

    private List<CustomLevelData> customLevels = new List<CustomLevelData>();

    void Start()
    {
        LoadCustomLevels();
        PopulateLevelList();
    }

    private void LoadCustomLevels()
    {
        customLevels.Clear();

        string beatmapsFolder = Path.Combine(Application.persistentDataPath, "Beatmaps");

        if (!Directory.Exists(beatmapsFolder))
        {
            Debug.LogWarning($"No existe la carpeta de beatmaps: {beatmapsFolder}");
            return;
        }

        string[] jsonFiles = Directory.GetFiles(beatmapsFolder, "*.json");

        foreach (string filePath in jsonFiles)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                BeatMapDataWrapper wrapper = JsonUtility.FromJson<BeatMapDataWrapper>(json);

                if (wrapper != null && wrapper.notes != null && wrapper.notes.Count > 0)
                {
                    CustomLevelData levelData = new CustomLevelData();
                    levelData.fileName = Path.GetFileName(filePath);
                    levelData.levelName = GetLevelNameFromFileName(levelData.fileName);
                    levelData.noteCount = wrapper.notes.Count;
                    levelData.duration = GetLevelDuration(wrapper.notes);

                    customLevels.Add(levelData);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error cargando nivel {filePath}: {e.Message}");
            }
        }

        Debug.Log($"📁 Cargados {customLevels.Count} niveles custom");
    }

    private string GetLevelNameFromFileName(string fileName)
    {
        // Remover extensión .json y formatear nombre
        string nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);

        // Reemplazar underscores con espacios y formatear
        string formattedName = nameWithoutExt.Replace("_", " ");

        // Intentar extraer BPM si está en el formato "Cancion_120bpm"
        if (formattedName.ToLower().Contains("bpm"))
        {
            // Formatear mejor el nombre
            formattedName = formattedName.Replace("bpm", " BPM");
        }

        return formattedName;
    }

    private string GetLevelDuration(List<NoteData> notes)
    {
        if (notes == null || notes.Count == 0) return "0:00";

        // Encontrar el tiempo de la última nota
        float lastNoteTime = 0f;
        foreach (var note in notes)
        {
            if (note.time > lastNoteTime)
                lastNoteTime = note.time;
        }

        // Agregar margen para la caída de la última nota (aprox 2 segundos)
        float totalDuration = lastNoteTime + 2f;

        // Formatear como mm:ss
        int minutes = Mathf.FloorToInt(totalDuration / 60f);
        int seconds = Mathf.FloorToInt(totalDuration % 60f);

        return $"{minutes}:{seconds:00}";
    }

    private void PopulateLevelList()
    {
        // Limpiar contenedor
        foreach (Transform child in levelContainer)
            Destroy(child.gameObject);

        if (customLevels.Count == 0)
        {
            if (noLevelsText != null)
                noLevelsText.gameObject.SetActive(true);
            return;
        }

        if (noLevelsText != null)
            noLevelsText.gameObject.SetActive(false);

        foreach (CustomLevelData levelData in customLevels)
        {
            GameObject panel = Instantiate(levelPanelPrefab, levelContainer);
            LevelPanelUI ui = panel.GetComponent<LevelPanelUI>();

            if (ui != null)
            {
                // Cargar stats si existen
                LevelStats bestStats = LoadBestStats(levelData.fileName);

                string grade = bestStats != null ? bestStats.grade : "-";
                int score = bestStats != null ? bestStats.score : 0;
                float accuracy = bestStats != null ? bestStats.accuracy : 0f;
                string dateTime = bestStats != null ? bestStats.dateTime : "Nuevo";

                ui.SetLevelInfo(
                    levelData.levelName,
                    grade,
                    score,
                    accuracy,
                    dateTime,
                    true, // Siempre desbloqueado
                    $"{levelData.noteCount} notas • {levelData.duration}"
                );

                // Asignar evento de clic
                ui.button.onClick.AddListener(() => PlayCustomLevel(levelData.fileName));
            }
        }

        // Scroll al inicio
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 1f;
    }

    private LevelStats LoadBestStats(string fileName)
    {
        try
        {
            string statsFolder = Path.Combine(Application.persistentDataPath, "Stats", "Custom");
            string statsPath = Path.Combine(statsFolder, Path.GetFileNameWithoutExtension(fileName) + "_stats.json");

            if (!File.Exists(statsPath)) return null;

            string json = File.ReadAllText(statsPath);
            LevelStatsWrapper wrapper = JsonUtility.FromJson<LevelStatsWrapper>(json);

            if (wrapper == null || wrapper.stats == null || wrapper.stats.Count == 0)
                return null;

            // Encontrar el mejor score
            LevelStats best = wrapper.stats[0];
            foreach (var stat in wrapper.stats)
            {
                if (stat.score > best.score)
                    best = stat;
            }

            return best;
        }
        catch
        {
            return null;
        }
    }

    public void PlayCustomLevel(string fileName)
    {
        // Guardar el fileName para que la escena de gameplay lo cargue
        PlayerPrefs.SetString("CurrentCustomLevel", fileName);
        PlayerPrefs.Save();

        // Cargar escena de gameplay plantilla
        SceneManager.LoadScene(gameplaySceneName);
    }

    public void RefreshLevelList()
    {
        LoadCustomLevels();
        PopulateLevelList();
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}

[System.Serializable]
public class CustomLevelData
{
    public string fileName;    // Nombre completo del archivo .json
    public string levelName;   // Nombre formateado para mostrar
    public int noteCount;
    public string duration;    // Duración formateada mm:ss
}