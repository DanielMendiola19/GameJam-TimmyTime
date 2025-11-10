using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

[Serializable]
public class LevelStats
{
    public string levelID;
    public string levelName;
    public int perfect;
    public int great;
    public int fail;
    public int miss;
    public int score;
    public int maxCombo;
    public float accuracy;
    public string grade;
    public string dateTime;
}

[Serializable]
public class LevelStatsWrapper
{
    public List<LevelStats> stats = new List<LevelStats>();
    public string hash;
}

public class StatsManager : MonoBehaviour
{
    [Header("Configuración de Guardado")]
    public string statsFolder = "Stats";

    private Dictionary<string, List<LevelStats>> allStats = new Dictionary<string, List<LevelStats>>();

    void Awake()
    {
        LoadAllStats();
    }

    private string GetStatsFilePath(string levelID, bool isStory)
    {
        if (isStory)
        {
            string subFolder = "Story";
#if UNITY_EDITOR
            string folderPath = Path.Combine(statsFolder, subFolder);
#else
            string folderPath = Path.Combine(Application.persistentDataPath, statsFolder, subFolder);
#endif
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            return Path.Combine(folderPath, $"{levelID}_stats.json");
        }
        else
        {
            // Ruta custom dentro de la carpeta del nivel
            string levelFolder = Path.Combine(Application.persistentDataPath, "CustomLevels", levelID);
            if (!Directory.Exists(levelFolder))
                Directory.CreateDirectory(levelFolder);

            return Path.Combine(levelFolder, "stats.json");
        }
    }

    public void SaveLevelStats(LevelStats stats, bool isStory)
    {
        if (string.IsNullOrEmpty(stats.levelID))
        {
            Debug.LogError("StatsManager: levelID vacío, no se puede guardar");
            return;
        }

        if (!allStats.ContainsKey(stats.levelID))
            allStats[stats.levelID] = new List<LevelStats>();

        stats.dateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        allStats[stats.levelID].Add(stats);

        SaveStatsToFile(stats.levelID, isStory);
    }

    public List<LevelStats> GetLevelStats(string levelID)
    {
        if (allStats.ContainsKey(levelID))
            return allStats[levelID];
        return new List<LevelStats>();
    }

    private void SaveStatsToFile(string levelID, bool isStory)
    {
        string filePath = GetStatsFilePath(levelID, isStory);

        LevelStatsWrapper wrapper = new LevelStatsWrapper();
        wrapper.stats = allStats[levelID];

        string jsonNoHash = JsonUtility.ToJson(wrapper, true);
        wrapper.hash = ComputeHash(jsonNoHash);

        string finalJson = JsonUtility.ToJson(wrapper, true);
        File.WriteAllText(filePath, finalJson);

        Debug.Log($"[StatsManager] Estadísticas guardadas en: {filePath}");
    }

    private void LoadAllStats()
    {
        allStats.Clear();

        // Story levels
        LoadStatsFromFolder("Story");

        // Custom levels
        string customLevelsPath = Path.Combine(Application.persistentDataPath, "CustomLevels");
        if (!Directory.Exists(customLevelsPath)) return;

        string[] levelFolders = Directory.GetDirectories(customLevelsPath);
        foreach (string folder in levelFolders)
        {
            string levelID = Path.GetFileName(folder);
            string statsFile = Path.Combine(folder, "stats.json");

            if (!File.Exists(statsFile))
            {
                // Si no existe, crear uno vacío
                File.WriteAllText(statsFile, JsonUtility.ToJson(new LevelStatsWrapper(), true));
            }

            try
            {
                string json = File.ReadAllText(statsFile);
                LevelStatsWrapper wrapper = JsonUtility.FromJson<LevelStatsWrapper>(json);

                // Verificación de hash
                string hashOriginal = wrapper.hash;
                wrapper.hash = null;
                string jsonSinHash = JsonUtility.ToJson(wrapper, true);

                if (!string.IsNullOrEmpty(hashOriginal) && hashOriginal != ComputeHash(jsonSinHash))
                {
                    Debug.LogWarning($"[StatsManager] Archivo corrupto o modificado externamente: {statsFile}. Se crea uno nuevo limpio.");
                    wrapper = new LevelStatsWrapper();
                    File.WriteAllText(statsFile, JsonUtility.ToJson(wrapper, true));
                }

                allStats[levelID] = wrapper.stats;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[StatsManager] Error al cargar estadísticas: {statsFile}\n{e}");
                File.WriteAllText(statsFile, JsonUtility.ToJson(new LevelStatsWrapper(), true));
                allStats[levelID] = new List<LevelStats>();
            }
        }

        Debug.Log($"[StatsManager] Estadísticas cargadas: {allStats.Count} niveles");
    }

    private void LoadStatsFromFolder(string subFolder)
    {
#if UNITY_EDITOR
        string folderPath = Path.Combine(statsFolder, subFolder);
#else
        string folderPath = Path.Combine(Application.persistentDataPath, statsFolder, subFolder);
#endif
        if (!Directory.Exists(folderPath)) return;

        string[] files = Directory.GetFiles(folderPath, "*_stats.json");

        foreach (string file in files)
        {
            try
            {
                string json = File.ReadAllText(file);
                LevelStatsWrapper wrapper = JsonUtility.FromJson<LevelStatsWrapper>(json);

                string hashOriginal = wrapper.hash;
                wrapper.hash = null;
                string jsonSinHash = JsonUtility.ToJson(wrapper, true);

                if (!string.IsNullOrEmpty(hashOriginal) && hashOriginal != ComputeHash(jsonSinHash))
                {
                    Debug.LogWarning($"[StatsManager] Archivo corrupto o modificado externamente: {file}. Se crea uno nuevo limpio.");
                    string idFromFile = Path.GetFileName(file).Replace("_stats.json", "");
                    allStats[idFromFile] = new List<LevelStats>();
                    File.WriteAllText(file, JsonUtility.ToJson(new LevelStatsWrapper(), true));
                    continue;
                }

                string levelID = Path.GetFileName(file).Replace("_stats.json", "");
                allStats[levelID] = wrapper.stats;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[StatsManager] Error al cargar estadísticas: {file}\n{e}");
                File.WriteAllText(file, JsonUtility.ToJson(new LevelStatsWrapper(), true));
            }
        }
    }

    public void DeleteLevelStats(string levelID, bool isStory)
    {
        if (allStats.ContainsKey(levelID))
            allStats.Remove(levelID);

        string filePath = GetStatsFilePath(levelID, isStory);
        if (File.Exists(filePath))
            File.Delete(filePath);

        Debug.Log($"[StatsManager] Estadísticas eliminadas de: {levelID}");
    }

    private string ComputeHash(string text)
    {
        using (SHA256 sha = SHA256.Create())
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            byte[] hash = sha.ComputeHash(bytes);
            StringBuilder sb = new StringBuilder();
            foreach (byte b in hash)
                sb.Append(b.ToString("X2"));
            return sb.ToString();
        }
    }
}
