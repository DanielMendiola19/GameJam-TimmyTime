using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections;
using UnityEngine.Networking;
using TMPro;
using System.Collections.Generic;
using System.Linq;



#if UNITY_STANDALONE_WIN
using WinForms = System.Windows.Forms;
#endif


[System.Serializable]
public class CustomLevelInfo
{
    public string levelID;      // ID único del nivel
    public string levelName;    // Nombre para mostrar en lista
    public string songName;     // Nombre de la canción
    public float bpm;
    public float duration;      // Duración de la canción
}


public class SongUploadUI : MonoBehaviour
{
    [Header("Referencias UI")]
    public TMP_Text songNameText;
    public TMP_InputField bpmInput;
    public Button uploadButton;
    public Button generateButton;
    public Button playPauseButton;
    public Button clearButton;
    public Slider timelineSlider;
    public TMP_Text statusText;
    public GameObject loadingSpinner;
    public TMP_Text timeText;

    [Header("UI Mensajes")]
    public TMP_Text bpmNoticeText;

    [Header("Sprites Play/Pause")]
    public Sprite playSprite;
    public Sprite pauseSprite;

    [Header("Límites de Validación")]
    public float maxFileSizeMB = 100f;
    public float minSongDuration = 10f;
    public float maxSongDuration = 900f; // 15 minutos
    public int minBPM = 30;
    public int maxBPM = 500;
    public float loadTimeoutSeconds = 30f;

    [Header("Referencias de Scripts")]
    public BeatMapGeneratorUI generator;
    public AudioSource previewAudio;

    private string savedSongPath;
    private bool isPlaying = false;
    private bool isLoading = false;
    private bool isGenerating = false;
    private bool isDraggingTimeline = false;
    private Coroutine currentLoadCoroutine;
    private Image playPauseButtonImage;

    void Start()
    {
        playPauseButtonImage = playPauseButton.GetComponent<Image>();

        songNameText.text = "Ninguna canción seleccionada";
        statusText.text = "Listo para cargar canción";
        statusText.color = Color.white;
        timelineSlider.minValue = 0f;
        timelineSlider.maxValue = 1f;
        timelineSlider.value = 0f;
        loadingSpinner.SetActive(false);
        timeText.text = "00:00 - 00:00";

        uploadButton.onClick.AddListener(OnUploadSong);
        generateButton.onClick.AddListener(OnGenerateMap);
        playPauseButton.onClick.AddListener(TogglePlayPause);
        clearButton.onClick.AddListener(ResetUI);

        // Cambio importante: Usar eventos separados para drag
        timelineSlider.onValueChanged.AddListener(OnTimelineChanging);

        // Agregar eventos para detectar cuando se suelta el slider
        var eventTrigger = timelineSlider.GetComponent<UnityEngine.EventSystems.EventTrigger>();
        if (eventTrigger == null)
            eventTrigger = timelineSlider.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();

        // Evento cuando empieza a arrastrar
        var pointerDown = new UnityEngine.EventSystems.EventTrigger.Entry();
        pointerDown.eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown;
        pointerDown.callback.AddListener((data) => { OnTimelineDragStart(); });
        eventTrigger.triggers.Add(pointerDown);

        // Evento cuando suelta el arrastre
        var pointerUp = new UnityEngine.EventSystems.EventTrigger.Entry();
        pointerUp.eventID = UnityEngine.EventSystems.EventTriggerType.PointerUp;
        pointerUp.callback.AddListener((data) => { OnTimelineDragEnd(); });
        eventTrigger.triggers.Add(pointerUp);

        bpmInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        bpmInput.characterLimit = 3;

        generateButton.interactable = false;
        playPauseButton.interactable = false;
        timelineSlider.interactable = false;

        if (playPauseButtonImage != null && playSprite != null)
            playPauseButtonImage.sprite = playSprite;
    }

    void Update()
    {
        if (previewAudio.clip != null)
        {
            // Actualiza slider y tiempo mientras se reproduce Y no se está arrastrando
            if (isPlaying && !isDraggingTimeline)
            {
                float currentTime = previewAudio.time;
                float totalTime = previewAudio.clip.length;

                timelineSlider.SetValueWithoutNotify(currentTime / totalTime);
                UpdateTimeText(currentTime, totalTime);

                // Cuando termina la canción
                if (currentTime >= totalTime - 0.1f) // Pequeño margen para evitar loops
                {
                    previewAudio.Stop();
                    previewAudio.time = 0f;
                    isPlaying = false;
                    timelineSlider.SetValueWithoutNotify(0f);
                    if (playPauseButtonImage != null && playSprite != null)
                        playPauseButtonImage.sprite = playSprite;

                    UpdateTimeText(0f, totalTime);
                }
            }
        }
    }

    void TogglePlayPause()
    {
        if (previewAudio.clip == null) return;

        if (isPlaying)
        {
            previewAudio.Pause();
            isPlaying = false;
            if (playPauseButtonImage != null && playSprite != null)
                playPauseButtonImage.sprite = playSprite;
        }
        else
        {
            previewAudio.Play();
            isPlaying = true;
            if (playPauseButtonImage != null && pauseSprite != null)
                playPauseButtonImage.sprite = pauseSprite;
        }
    }

    // Se llama mientras se arrastra el slider (solo actualiza visualmente)
    void OnTimelineChanging(float value)
    {
        if (previewAudio.clip == null) return;

        float newTime = value * previewAudio.clip.length;

        // Solo actualizar el texto del tiempo, NO cambiar la posición del audio
        UpdateTimeText(newTime, previewAudio.clip.length);
    }

    // Se llama cuando empieza a arrastrar el slider
    void OnTimelineDragStart()
    {
        isDraggingTimeline = true;

        // Pausar temporalmente si estaba reproduciéndose
        if (isPlaying)
        {
            previewAudio.Pause();
        }
    }

    // Se llama cuando suelta el slider
    void OnTimelineDragEnd()
    {
        if (previewAudio.clip == null) return;

        isDraggingTimeline = false;

        // Asegurarse de que newTime esté dentro del rango válido
        float newTime = Mathf.Clamp(timelineSlider.value * previewAudio.clip.length, 0f, previewAudio.clip.length - 0.01f);
        previewAudio.time = newTime;

        UpdateTimeText(newTime, previewAudio.clip.length);

        // Reanudar reproducción si estaba reproduciéndose antes del drag
        if (isPlaying)
        {
            previewAudio.Play();
        }
    }


    void OnUploadSong()
    {
        if (isLoading)
        {
            ShowStatusMessage("Ya se está cargando una canción...", Color.yellow);
            return;
        }

#if UNITY_STANDALONE_WIN
        var ofd = new WinForms.OpenFileDialog
        {
            Filter = "Archivos de Audio|*.mp3;*.wav;*.ogg",
            Title = "Selecciona una canción"
        };

        if (ofd.ShowDialog() == WinForms.DialogResult.OK)
        {
            string selectedPath = ofd.FileName;

            if (!ValidateFile(selectedPath))
                return;

            savedSongPath = selectedPath;

            currentLoadCoroutine = StartCoroutine(LoadAudioClipWithTimeout(savedSongPath));
        }
        else
        {
            ShowStatusMessage("No se seleccionó ninguna canción.", Color.yellow);
        }
#else
        ShowStatusMessage("Selector de archivos solo disponible en Windows.", Color.red);
#endif
    }

    private bool ValidateFile(string filePath)
    {
        try
        {
            FileInfo fileInfo = new FileInfo(filePath);
            float fileSizeMB = fileInfo.Length / (1024f * 1024f);

            if (fileSizeMB > maxFileSizeMB)
            {
                ShowStatusMessage($"Archivo demasiado grande ({fileSizeMB:F1}MB). Máximo: {maxFileSizeMB}MB", Color.red);
                return false;
            }

            string extension = Path.GetExtension(filePath).ToLower();
            if (extension != ".mp3" && extension != ".wav" && extension != ".ogg")
            {
                ShowStatusMessage("Formato no soportado. Usa .mp3, .wav o .ogg", Color.red);
                return false;
            }

            return true;
        }
        catch (System.Exception ex)
        {
            ShowStatusMessage("Error al validar el archivo", Color.red);
            Debug.LogError($"Error validando archivo: {ex.Message}");
            return false;
        }
    }

    private bool ValidateAudioClip(AudioClip clip)
    {
        if (clip == null)
        {
            ShowStatusMessage("Error: Archivo de audio corrupto", Color.red);
            return false;
        }

        if (clip.length < minSongDuration)
        {
            ShowStatusMessage($"Canción demasiado corta ({clip.length:F1}s). Mínimo: {minSongDuration}s", Color.red);
            return false;
        }

        if (clip.length > maxSongDuration)
        {
            ShowStatusMessage($"Canción demasiado larga ({clip.length:F1}s). Máximo: {maxSongDuration}s", Color.red);
            return false;
        }

        if (clip.frequency < 8000 || clip.frequency > 192000)
            ShowStatusMessage($"Sample rate no compatible: {clip.frequency}Hz", Color.yellow);

        return true;
    }

    private bool ValidateBPM(string bpmText)
    {
        if (string.IsNullOrWhiteSpace(bpmText))
        {
            ShowStatusMessage("Ingresa el BPM de la canción", Color.yellow);
            return false;
        }

        if (!int.TryParse(bpmText, out int bpm))
        {
            ShowStatusMessage("BPM no válido", Color.red);
            return false;
        }

        if (bpm < minBPM || bpm > maxBPM)
        {
            ShowStatusMessage($"BPM fuera de rango ({minBPM}-{maxBPM})", Color.red);
            return false;
        }

        return true;
    }

    IEnumerator LoadAudioClipWithTimeout(string path)
    {
        isLoading = true;
        uploadButton.interactable = false;
        loadingSpinner.SetActive(true);
        ShowStatusMessage("Cargando canción...", Color.blue);

        bool loadCompleted = false;
        float startTime = Time.time;

        Coroutine loadCoroutine = StartCoroutine(LoadAudioClip(path, () => loadCompleted = true));

        while (!loadCompleted && Time.time - startTime < loadTimeoutSeconds)
            yield return new WaitForSeconds(0.1f);

        if (!loadCompleted)
        {
            StopCoroutine(loadCoroutine);
            ShowStatusMessage("Timeout: La carga tardó demasiado", Color.red);
        }

        isLoading = false;
        uploadButton.interactable = true;
        loadingSpinner.SetActive(false);
    }

    IEnumerator LoadAudioClip(string path, System.Action onComplete)
    {
        string url = "file://" + path;
        using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.UNKNOWN))
        {
            uwr.SendWebRequest();

            while (!uwr.isDone)
            {
                ShowStatusMessage($"Cargando... {uwr.downloadProgress * 100:F0}%", Color.blue);
                yield return new WaitForSeconds(0.1f);
            }

#if UNITY_2020_1_OR_NEWER
            if (uwr.result != UnityWebRequest.Result.Success)
#else
            if (uwr.isNetworkError || uwr.isHttpError)
#endif
            {
                ShowStatusMessage("Error al cargar el archivo de audio", Color.red);
                Debug.LogError($"Error cargando audio: {uwr.error}");
                onComplete?.Invoke();
                yield break;
            }

            AudioClip clip = DownloadHandlerAudioClip.GetContent(uwr);

            if (!ValidateAudioClip(clip))
            {
                // Borrar todo como reset
                ResetUI();
                onComplete?.Invoke();
                yield break;
            }

            previewAudio.clip = clip;
            StartCoroutine(AutoEstimateBPM(clip));
            previewAudio.clip.name = Path.GetFileNameWithoutExtension(path);
            previewAudio.time = 0f;
            isPlaying = false;

            songNameText.text = Path.GetFileName(path);

            generator.SetSongClip(clip);

            generateButton.interactable = true;
            playPauseButton.interactable = true;
            timelineSlider.interactable = true;

            if (playPauseButtonImage != null && playSprite != null)
                playPauseButtonImage.sprite = playSprite;

            // MOSTRAR TIEMPO INMEDIATAMENTE AL CARGAR LA CANCIÓN
            UpdateTimeText(0f, clip.length);

            ShowStatusMessage("Canción cargada correctamente", Color.green);
            onComplete?.Invoke();
        }
    }

    void OnGenerateMap()
    {
        if (isGenerating)
        {
            ShowStatusMessage("Ya se está generando un beatmap...", Color.yellow);
            return;
        }

        if (previewAudio.clip == null)
        {
            ShowStatusMessage("No hay canción cargada", Color.red);
            return;
        }

        if (!ValidateBPM(bpmInput.text))
            return;

        int bpm = int.Parse(bpmInput.text);

        StartCoroutine(GenerateBeatMapWithProgress(bpm));
    }

    IEnumerator GenerateBeatMapWithProgress(int bpm)
    {
        isGenerating = true;
        generateButton.interactable = false;
        ShowStatusMessage("Generando beatmap...", Color.blue);

        generator.SetBPM(bpm);
        generator.GenerateBeatMap();
#if UNITY_EDITOR
        // Aquí llamamos a SaveBeatMapAsSO en Editor
        generator.SaveBeatMapAsSO();
#endif


        // Aquí opcionalmente puedes mostrar progreso visual
        for (int i = 0; i < 5; i++)
        {
            ShowStatusMessage($"Generando beatmap... {i * 20}%", Color.blue);
            yield return new WaitForSeconds(0.3f);
        }

        if (!SaveCustomLevel(bpm))
        {
            ShowStatusMessage("Error guardando nivel custom", Color.red);
            isGenerating = false;
            generateButton.interactable = true;
            yield break;
        }

        ShowStatusMessage("¡Nivel Custom generado y guardado!", Color.green);
        isGenerating = false;
        generateButton.interactable = true;
    }

    private bool SaveCustomLevel(int bpm)
    {
        if (previewAudio.clip == null)
        {
            ShowStatusMessage("No hay canción cargada para guardar", Color.red);
            return false;
        }

        string levelID = generator.GetSongKey(); // Exactamente igual que BeatMapGeneratorUI
        string customLevelsFolder = Path.Combine(Application.persistentDataPath, "CustomLevels");
        string levelFolder = Path.Combine(customLevelsFolder, levelID);

        if (!Directory.Exists(levelFolder))
            Directory.CreateDirectory(levelFolder);

        // Guardar info.json
        CustomLevelInfo info = new CustomLevelInfo
        {
            levelID = levelID,
            levelName = Path.GetFileNameWithoutExtension(previewAudio.clip.name),
            songName = previewAudio.clip.name,
            bpm = bpm,
            duration = previewAudio.clip.length
        };

        string infoPath = Path.Combine(levelFolder, "info.json");
        File.WriteAllText(infoPath, JsonUtility.ToJson(info, true));

        // Guardar beatmap.json usando BeatMapGenerator
        generator.SaveBeatMap(levelFolder);

        // Guardar canción en mp3
        string songPath = Path.Combine(levelFolder, "song.mp3");
        if (!File.Exists(songPath))
        {
            try
            {
                File.Copy(savedSongPath, songPath);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error guardando canción: {ex.Message}");
                ShowStatusMessage("Error guardando canción", Color.red);
                return false;
            }
        }

        Debug.Log($"Custom Level guardado: {levelFolder}");
        return true;
    }

    IEnumerator AutoEstimateBPM(AudioClip clip)
    {
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        List<float> onsetTimes;
        List<float> onsetEnergies;

        AudioOnsetDetector.DetectOnsets(samples, clip.frequency, out onsetTimes, out onsetEnergies);

        float bpmEst = EstimateBPMFromOnsets(onsetTimes, onsetEnergies);

        bpmInput.text = Mathf.RoundToInt(bpmEst).ToString();

        // Mostrar mensaje temporal
        if (bpmNoticeText != null)
        {
            StartCoroutine(ShowBPMNoticeTemporary("BPM generado automáticamente: estimado. Se recomienda ajustar manualmente.", 6f));
        }

        yield break;
    }

    // Coroutine para mostrar el mensaje por un tiempo determinado
    private IEnumerator ShowBPMNoticeTemporary(string message, float duration)
    {
        bpmNoticeText.text = message;
        bpmNoticeText.color = Color.yellow; // Opcional: que resalte
        yield return new WaitForSeconds(duration);
        bpmNoticeText.text = ""; // Limpiar mensaje
    }


    // Archivo: SongUploadUI.cs
    public float EstimateBPMFromOnsets(List<float> onsetTimes, List<float> onsetEnergies, float energyPercentile = 0.3f)
    {
        if (onsetTimes.Count < 4) return 120f; // fallback

        // 1) Filtrar onsets por percentil de energía
        List<float> energiesSorted = new List<float>(onsetEnergies);
        energiesSorted.Sort();
        float energyThreshold = energiesSorted[Mathf.Max(0, (int)(energiesSorted.Count * energyPercentile))];

        List<float> strongOnsets = new List<float>();
        for (int i = 0; i < onsetTimes.Count; i++)
        {
            if (onsetEnergies[i] >= energyThreshold)
                strongOnsets.Add(onsetTimes[i]);
        }

        if (strongOnsets.Count < 4) return 120f;

        // 2) Calcular intervalos entre onsets
        List<float> intervals = new List<float>();
        for (int i = 1; i < strongOnsets.Count; i++)
        {
            float interval = strongOnsets[i] - strongOnsets[i - 1];
            if (interval > 0.2f && interval < 2.5f)
                intervals.Add(interval);
        }

        if (intervals.Count == 0) return 120f;

        // 3) Mediana y promedio de intervalos
        intervals.Sort();
        float medianInterval = intervals[intervals.Count / 2];

        float avgInterval = 0f;
        foreach (float interval in intervals) avgInterval += interval;
        avgInterval /= intervals.Count;

        // 4) Calcular BPM base
        float bpm = 60f / medianInterval;

        // 5) Ajustar a múltiplos/submúltiplos sensatos
        while (bpm > 160f) bpm /= 2f;
        while (bpm < 60f) bpm *= 2f;

        // 6) Ajuste especial para evitar repetición de 108 BPM
        if (Mathf.Round(bpm) == 108f)
        {
            float diff = medianInterval - avgInterval; // signo indica dirección
            float factor = Mathf.Clamp(Mathf.Abs(diff) * 120f, 5f, 10f); // magnitud entre 5 y 10 BPM

            // Variación entre 5-10 según diferencia, signo decide subir o bajar
            bpm += (diff < 0f ? factor : -factor);
        }

        return Mathf.Round(bpm);
    }








    private void ShowStatusMessage(string message, Color color)
    {
        statusText.text = message;
        statusText.color = color;
        Debug.Log($"[SongUploadUI] {message}");
    }

    public void ResetUI()
    {
        // Detener cualquier carga en curso
        if (currentLoadCoroutine != null)
        {
            StopCoroutine(currentLoadCoroutine);
            currentLoadCoroutine = null;
        }

        // Resetear audio
        previewAudio.Stop();
        previewAudio.clip = null;
        isPlaying = false;
        isDraggingTimeline = false;

        if (playPauseButtonImage != null && playSprite != null)
            playPauseButtonImage.sprite = playSprite;

        // Limpiar referencia al archivo cargado
        savedSongPath = null;

        // Resetear estados
        isLoading = false;
        isGenerating = false;

        // Resetear UI
        songNameText.text = "Ninguna canción seleccionada";
        statusText.text = "Listo para cargar canción";
        statusText.color = Color.white;
        bpmInput.text = "";
        timelineSlider.value = 0f;
        loadingSpinner.SetActive(false);
        timeText.text = "00:00 - 00:00";
        bpmNoticeText.text = "";

        generateButton.interactable = false;
        playPauseButton.interactable = false;
        timelineSlider.interactable = false;
        uploadButton.interactable = true;
    }


    public void SetGeneratorReference(BeatMapGeneratorUI gen)
    {
        generator = gen;
    }

    private void UpdateTimeText(float currentTime, float totalTime)
    {
        timeText.text = $"{FormatTime(currentTime)} - {FormatTime(totalTime)}";
    }

    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        return $"{minutes:D2}:{seconds:D2}";
    }


}