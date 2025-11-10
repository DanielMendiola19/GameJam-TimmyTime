using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;

public class StoryLevelButton : MonoBehaviour
{
    public string levelID;
    public Button button;
    public GameObject lockIcon;
    public string statsFolder = "Stats/Story";

    void Start()
    {
        UpdateButtonState();
    }

    public void UpdateButtonState()
    {
        bool unlocked = false;

        if (levelID == "StoryLevel_001")
        {
            unlocked = true; // Primer nivel desbloqueado
        }
        else
        {
            string prevLevelID = GetPreviousLevelID(levelID);
            LevelStatsWrapper wrapper = LoadStatsWrapper(prevLevelID);
            if (wrapper != null && wrapper.stats.Count > 0)
            {
                string grade = wrapper.stats[wrapper.stats.Count - 1].grade;
                unlocked = IsGradeSufficient(grade, "A");
            }
        }

        button.interactable = unlocked;
        if (lockIcon != null)
            lockIcon.SetActive(!unlocked);
    }

    private string GetPreviousLevelID(string currentID)
    {
        int num = int.Parse(currentID.Substring(11, 3));
        return "StoryLevel_" + (num - 1).ToString("D3");
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
            return JsonUtility.FromJson<LevelStatsWrapper>(json); // usa la de StatsManager
        }
        catch
        {
            Debug.LogWarning("No se pudo leer stats JSON para " + levelID);
            return null;
        }
    }
}
