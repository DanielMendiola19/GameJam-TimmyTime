using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Collections;

public class CustomLevelLoader : MonoBehaviour
{
    public AudioSource songSource; // SOLO para custom
    public BeatMap runtimeBeatMap;

    void Awake()
    {
        string levelID = PlayerPrefs.GetString("CurrentCustomLevel", null);

        if (string.IsNullOrEmpty(levelID))
        {
            Debug.LogError("No hay levelID seleccionado!!");
            return;
        }

        string levelFolder = Path.Combine(Application.persistentDataPath, "CustomLevels", levelID);

        // cargar info
        string infoPath = Path.Combine(levelFolder, "info.json");
        CustomLevelInfo info = JsonUtility.FromJson<CustomLevelInfo>(File.ReadAllText(infoPath));

        // cargar beatmap
        string beatPath = Path.Combine(levelFolder, "beatmap.json");
        BeatMapDataWrapper wrapper = JsonUtility.FromJson<BeatMapDataWrapper>(File.ReadAllText(beatPath));
        runtimeBeatMap = new BeatMap();
        runtimeBeatMap.notes = wrapper.notes;

        // cargar song usando UnityWebRequest
        string songPath = Path.Combine(levelFolder, "song.mp3");
        StartCoroutine(LoadSong(songPath, info.levelName));
    }

    private IEnumerator LoadSong(string path, string levelName)
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + path, AudioType.MPEG))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error cargando canción del nivel {levelName}: {www.error}");
            }
            else
            {
                songSource.clip = DownloadHandlerAudioClip.GetContent(www);
                Debug.Log("CUSTOM LEVEL LOADED OK: " + levelName);
            }
        }
    }
}
