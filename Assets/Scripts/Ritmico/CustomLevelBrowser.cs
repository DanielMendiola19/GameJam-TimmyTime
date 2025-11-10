using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class CustomLevelBrowser : MonoBehaviour
{
    [Header("UI References")]
    public GameObject levelPanelPrefab;
    public Transform levelContainer;
    public ScrollRect scrollRect;
    public TMP_Text noLevelsText;

    [Header("Confirmation Panel")]
    public GameObject confirmPanel;
    public TMP_Text confirmText;
    public Button confirmYesButton;
    public Button confirmNoButton;

    [Header("Settings")]
    public string gameplaySceneName = "Nivel_Custom";

    List<CustomLevelInfo> levels = new List<CustomLevelInfo>();
    string levelToDelete = "";

    void Start()
    {
        confirmPanel.SetActive(false);
        confirmYesButton.onClick.AddListener(OnConfirmDelete);
        confirmNoButton.onClick.AddListener(() => confirmPanel.SetActive(false));

        LoadCustomLevels();
        PopulateLevelList();
    }

    void LoadCustomLevels()
    {
        levels.Clear();

        string mainFolder = Path.Combine(Application.persistentDataPath, "CustomLevels");
        if (!Directory.Exists(mainFolder))
        {
            Directory.CreateDirectory(mainFolder);
            return;
        }

        foreach (var folder in Directory.GetDirectories(mainFolder))
        {
            string infoPath = Path.Combine(folder, "info.json");
            if (!File.Exists(infoPath)) continue;

            string json = File.ReadAllText(infoPath);
            CustomLevelInfo info = JsonUtility.FromJson<CustomLevelInfo>(json);

            if (info != null)
                levels.Add(info);
        }
    }

    void PopulateLevelList()
    {
        foreach (Transform child in levelContainer)
            Destroy(child.gameObject);

        if (levels.Count == 0)
        {
            noLevelsText.gameObject.SetActive(true);
            return;
        }

        noLevelsText.gameObject.SetActive(false);

        foreach (var info in levels)
        {
            GameObject panel = Instantiate(levelPanelPrefab, levelContainer);
            LevelPanelUI ui = panel.GetComponent<LevelPanelUI>();

            LevelStats best = LoadBestCustomStats(info.levelID);
            string grade = best != null ? best.grade : "-";
            int score = best != null ? best.score : 0;
            float acc = best != null ? best.accuracy : 0f;
            string fecha = best != null ? best.dateTime : "-";

            ui.SetLevelInfo(
                info.levelName,
                grade,
                score,
                acc,
                fecha,
                true,
                $"{info.bpm} - {FormatTime(info.duration)}"
            );

            ui.button.onClick.AddListener(() => SelectLevel(info.levelID));

            // NUEVO: Botón de eliminar
            ui.deleteButton.gameObject.SetActive(true); // asegúrate que LevelPanelUI tenga deleteButton
            ui.deleteButton.onClick.AddListener(() => AskDeleteLevel(info.levelID, info.levelName));
        }

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 1f;
    }

    LevelStats LoadBestCustomStats(string levelID)
    {
        string folder = Path.Combine(Application.persistentDataPath, "CustomLevels", levelID);
        string statsPath = Path.Combine(folder, "stats.json");

        if (!File.Exists(statsPath))
            return null;

        string json = File.ReadAllText(statsPath);
        LevelStatsWrapper wrap = JsonUtility.FromJson<LevelStatsWrapper>(json);

        if (wrap == null || wrap.stats == null || wrap.stats.Count == 0)
            return null;

        LevelStats best = wrap.stats[0];
        foreach (var s in wrap.stats)
        {
            if (s.score > best.score)
                best = s;
        }

        return best;
    }

    void SelectLevel(string levelID)
    {
        PlayerPrefs.SetString("CurrentCustomLevel", levelID);
        PlayerPrefs.Save();
        SceneManager.LoadScene(gameplaySceneName);
    }

    string FormatTime(float seconds)
    {
        int m = Mathf.FloorToInt(seconds / 60f);
        int s = Mathf.FloorToInt(seconds % 60f);
        return $"{m:D2}:{s:D2}";
    }

    // NUEVO: Mostrar panel de confirmación
    void AskDeleteLevel(string levelID, string levelName)
    {
        levelToDelete = levelID;
        confirmText.text = $"¿Deseas eliminar el nivel \"{levelName}\"? Esta acción no se puede deshacer.";
        confirmPanel.SetActive(true);
    }

    // NUEVO: Confirmar eliminación
    void OnConfirmDelete()
    {
        if (string.IsNullOrEmpty(levelToDelete)) return;

        string folderPath = Path.Combine(Application.persistentDataPath, "CustomLevels", levelToDelete);
        if (Directory.Exists(folderPath))
        {
            Directory.Delete(folderPath, true); // elimina todo el nivel
            Debug.Log($"Nivel {levelToDelete} eliminado.");
        }

        confirmPanel.SetActive(false);
        levelToDelete = "";

        // Recargar lista en tiempo real
        LoadCustomLevels();
        PopulateLevelList();
    }
}
