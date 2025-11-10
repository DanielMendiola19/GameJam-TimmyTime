using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

[RequireComponent(typeof(AudioSource))]
public class BeatMapGenerator : MonoBehaviour
{
    public AudioSource song;
    public AudioSource tapAudio;
    public BeatMap generatedBeatMap;
    public NoteSpawner spawner;

    [Header("Análisis de audio")]
    public int fftSize = 1024;
    public int hopSize = 512;
    public float minNoteSpacing = 0.08f;
    public float peakThresholdMultiplier = 1.4f;
    public float sustainedSuppressFactor = 0.6f;
    public float sustainedEnergyWindow = 0.25f;

    [Header("Cuantización")]
    public float bpm = 120f;
    public float quantizeDivisions = 4f;

    [Header("Count-in")]
    public float countInTime = 2f;
    public int tapCount = 3;

    [Header("Notas Dobles")]
    public bool enableDoubleNotes = true;
    [Range(0f, 1f)]
    public float doubleNoteProbability = 0.8f;
    public float minEnergyForDoubleNotes = 0.1f;

    [Header("Filtro Final")]
    public bool enableEndFilter = true;
    [Range(0f, 30f)]
    public float secondsBeforeEndToStop = 6f;

    [Header("Guardado Automático")]
    public bool enableAutoSave = true;
    public string beatmapsFolder = "Assets/Beatmaps/Levels";

    void Start()
    {
        if (song == null || generatedBeatMap == null || spawner == null || tapAudio == null)
        {
            Debug.LogWarning("Asignar Song, BeatMap, Spawner y TapAudio en el inspector.");
            return;
        }

        // Verificar si ya existe un beatmap para esta canción
        if (enableAutoSave && TryLoadExistingBeatMap())
        {
            Debug.Log($"Beatmap cargado desde archivo para: {GetSongKey()}");
            spawner.beatMap = generatedBeatMap;
        }
        else
        {
            // Generar nuevo beatmap
            GenerateBeatMap();
            spawner.beatMap = generatedBeatMap;

            // Guardar automáticamente si está habilitado
            if (enableAutoSave)
            {
                SaveBeatMap();
            }
        }

        StartCoroutine(StartWithCountIn());
    }

    IEnumerator StartWithCountIn()
    {
        float tapInterval = countInTime / tapCount;
        for (int i = 0; i < tapCount; i++)
        {
            tapAudio.Play();
            yield return new WaitForSeconds(tapInterval);
        }

        song.Play();
    }

    private string GetSongKey()
    {
        if (song.clip == null) return "unknown";

        // Incluir el BPM en la clave para diferenciar beatmaps con diferente BPM
        return $"{song.clip.name}_{bpm}_{song.clip.length:F2}_{song.clip.frequency}";

    }

    private string GetBeatMapFilePath()
    {
        string songKey = GetSongKey();
        string fileName = $"{songKey.Replace(" ", "_").Replace(".", "_")}.asset";
        return Path.Combine(beatmapsFolder, fileName);
    }

    private bool TryLoadExistingBeatMap()
    {
        if (song.clip == null) return false;

        string songKey = GetSongKey();

#if UNITY_EDITOR
        // En el editor, cargar el asset directamente
        string filePath = GetBeatMapFilePath();
        BeatMap existingBeatMap = UnityEditor.AssetDatabase.LoadAssetAtPath<BeatMap>(filePath);
        if (existingBeatMap != null)
        {
            generatedBeatMap.notes.Clear();
            foreach (var note in existingBeatMap.notes)
                generatedBeatMap.notes.Add(new NoteData { time = note.time, lane = note.lane });
            return true;
        }
#else
    // En build, cargar desde JSON en carpeta persistente
    string folderPath = Path.Combine(Application.persistentDataPath, "Beatmaps");
    string filePath = Path.Combine(folderPath, $"{songKey.Replace(" ", "_").Replace(".", "_")}.json");

    if (File.Exists(filePath))
    {
        string json = File.ReadAllText(filePath);
        BeatMapDataWrapper wrapper = JsonUtility.FromJson<BeatMapDataWrapper>(json);
        generatedBeatMap.notes.Clear();

        foreach (var note in wrapper.notes)
            generatedBeatMap.notes.Add(new NoteData { time = note.time, lane = note.lane });

        Debug.Log($"[Build] Beatmap cargado desde: {filePath}");
        return true;
    }
#endif

        return false;
    }


    private void SaveBeatMap()
    {
        if (song.clip == null || generatedBeatMap.notes.Count == 0) return;

#if UNITY_EDITOR
        // --- Editor: guardar como asset ---
        if (!Directory.Exists(beatmapsFolder))
            Directory.CreateDirectory(beatmapsFolder);

        string filePath = GetBeatMapFilePath();

        if (generatedBeatMap.name == "EmptyBeatMap")
        {
            BeatMap newBeatMap = ScriptableObject.CreateInstance<BeatMap>();
            newBeatMap.notes = new List<NoteData>();
            foreach (var note in generatedBeatMap.notes)
                newBeatMap.notes.Add(new NoteData { time = note.time, lane = note.lane });

            UnityEditor.AssetDatabase.CreateAsset(newBeatMap, filePath);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();

            Debug.Log($"BeatMap guardado (Editor): {filePath}");
            spawner.beatMap = newBeatMap;
        }
        else
        {
            UnityEditor.EditorUtility.SetDirty(generatedBeatMap);
            UnityEditor.AssetDatabase.SaveAssets();
        }

#else
    // --- Build: guardar como JSON en persistentDataPath ---
    string folderPath = Path.Combine(Application.persistentDataPath, "Beatmaps");
    if (!Directory.Exists(folderPath))
        Directory.CreateDirectory(folderPath);

    string songKey = GetSongKey();
    string filePath = Path.Combine(folderPath, $"{songKey.Replace(" ", "_").Replace(".", "_")}.json");

    BeatMapDataWrapper wrapper = new BeatMapDataWrapper();
    wrapper.notes = generatedBeatMap.notes;

    string json = JsonUtility.ToJson(wrapper, true);
    File.WriteAllText(filePath, json);

    Debug.Log($"[Build] BeatMap guardado en: {filePath}");
#endif
    }

    public void GenerateBeatMap()
    {
        if (song.clip == null) { Debug.LogError("No clip asignado."); return; }

        generatedBeatMap.notes.Clear();
        System.Random prng = new System.Random(song.clip.name.GetHashCode());

        // --- Extraer audio mono ---
        float[] mono = new float[song.clip.samples];
        float[] stereo = new float[song.clip.samples * song.clip.channels];
        song.clip.GetData(stereo, 0);
        if (song.clip.channels == 1) Array.Copy(stereo, mono, mono.Length);
        else
        {
            for (int i = 0; i < mono.Length; i++)
            {
                float sum = 0f;
                for (int c = 0; c < song.clip.channels; c++) sum += stereo[i * song.clip.channels + c];
                mono[i] = sum / song.clip.channels;
            }
        }

        // --- Calcular duración real de la canción ---
        float songDuration = song.clip.length;
        float endCutoffTime = songDuration - secondsBeforeEndToStop;

        // --- Preparar ventanas y FFT ---
        List<float[]> spectra = new List<float[]>();
        List<float> times = new List<float>();
        List<float> energies = new List<float>();
        int pos = 0;
        while (pos + fftSize < mono.Length)
        {
            float[] window = new float[fftSize];
            Array.Copy(mono, pos, window, 0, fftSize);
            AudioUtils.ApplyHann(window);
            float[] spectrum = AudioUtils.RealFFTMag(window);
            spectra.Add(spectrum);

            float energy = 0f;
            foreach (float mag in spectrum) energy += mag;
            energies.Add(energy);

            times.Add((float)pos / song.clip.frequency);
            pos += hopSize;
        }

        // --- Spectral Flux ---
        float[] flux = AudioUtils.SpectralFlux(spectra);
        float[] fluxSmooth = AudioUtils.MedianFilter(flux, 3);
        float maxFlux = 1e-6f;
        for (int i = 0; i < fluxSmooth.Length; i++) if (fluxSmooth[i] > maxFlux) maxFlux = fluxSmooth[i];
        for (int i = 0; i < fluxSmooth.Length; i++) fluxSmooth[i] /= maxFlux;

        // Normalizar energías
        float maxEnergy = 1e-6f;
        foreach (float energy in energies) if (energy > maxEnergy) maxEnergy = energy;
        for (int i = 0; i < energies.Count; i++) energies[i] /= maxEnergy;

        float[] threshold = AudioUtils.AdaptiveThreshold(fluxSmooth, Mathf.RoundToInt(0.5f * song.clip.frequency / hopSize));
        List<int> peakFrames = AudioUtils.PickPeaks(fluxSmooth, threshold);

        List<float> onsetTimes = new List<float>();
        List<float> onsetEnergies = new List<float>();
        foreach (int f in peakFrames)
        {
            if (f >= 0 && f < times.Count)
            {
                float time = times[f];
                // Filtrar notas cerca del final si está habilitado
                if (enableEndFilter && time > endCutoffTime)
                    continue;

                onsetTimes.Add(time);
                onsetEnergies.Add(energies[f]);
            }
        }

        // --- Preparar notas ---
        float fallDistance = Vector3.Distance(spawner.lanePoints[0].position, spawner.targetPoints[0].position);
        float noteSpeed = spawner.notePrefab.GetComponent<Note>().speed;
        float fallTime = Mathf.Max(0.001f, fallDistance / noteSpeed);

        float lastNoteTime = -999f;

        for (int i = 0; i < onsetTimes.Count; i++)
        {
            float peakTime = onsetTimes[i];
            float energy = onsetEnergies[i];

            if (peakTime - lastNoteTime < minNoteSpacing) continue;

            float sustainScore = EstimateSustainScore(mono, song.clip.frequency, peakTime, sustainedEnergyWindow);
            if (sustainScore > 0.7f && prng.NextDouble() < sustainedSuppressFactor) continue;

            // Generar nota principal
            NoteData mainNote = new NoteData();
            mainNote.time = Mathf.Max(0f, peakTime - fallTime);
            mainNote.lane = prng.Next(0, spawner.lanePoints.Length);
            generatedBeatMap.notes.Add(mainNote);

            // Generar nota doble
            if (enableDoubleNotes && energy > minEnergyForDoubleNotes)
            {
                double probability = prng.NextDouble();
                if (probability < doubleNoteProbability)
                {
                    NoteData doubleNote = GenerateDoubleNote(mainNote, prng);
                    if (doubleNote != null)
                    {
                        generatedBeatMap.notes.Add(doubleNote);
                    }
                }
            }

            lastNoteTime = peakTime;
        }

        // --- Generar notas para count-in ---
        float preStartTime = countInTime - fallTime;
        int extraNotes = 0;
        while (extraNotes * (60f / bpm / quantizeDivisions) < preStartTime)
        {
            NoteData note = new NoteData();
            note.time = extraNotes * (60f / bpm / quantizeDivisions);
            note.lane = prng.Next(0, spawner.lanePoints.Length);
            generatedBeatMap.notes.Add(note);
            extraNotes++;
        }

        // --- Cuantizar según BPM ---
        float beatInterval = 60f / bpm / quantizeDivisions;
        for (int i = 0; i < generatedBeatMap.notes.Count; i++)
        {
            float t = generatedBeatMap.notes[i].time;
            t = Mathf.Round(t / beatInterval) * beatInterval;
            generatedBeatMap.notes[i].time = Mathf.Max(0f, t);
        }

        // Ordenar por tiempo
        generatedBeatMap.notes.Sort((a, b) =>
        {
            int timeCompare = a.time.CompareTo(b.time);
            if (timeCompare == 0) return a.lane.CompareTo(b.lane);
            return timeCompare;
        });

        // Solo eliminar notas que tengan EXACTAMENTE el mismo tiempo Y lane
        RemoveExactDuplicates(generatedBeatMap.notes);


        // --- Filtro final adicional por si acaso ---
        if (enableEndFilter)
        {
            RemoveNotesAtEnd(generatedBeatMap.notes, songDuration);
        }

        // --- REDUCIR NOTAS INICIALES ---
        ReduceEarlyNotes();

        Debug.Log($"BeatMap generado: {generatedBeatMap.notes.Count} notas. BPM usado: {bpm}. Corte final: {secondsBeforeEndToStop}s");
    }

    private void RemoveNotesAtEnd(List<NoteData> notes, float songDuration)
    {
        int removedCount = 0;
        float cutoffTime = songDuration - secondsBeforeEndToStop;

        for (int i = notes.Count - 1; i >= 0; i--)
        {
            if (notes[i].time > cutoffTime)
            {
                notes.RemoveAt(i);
                removedCount++;
            }
        }

        if (removedCount > 0)
        {
            Debug.Log($"Eliminadas {removedCount} notas del final de la canción");
        }
    }

    private NoteData GenerateDoubleNote(NoteData mainNote, System.Random prng)
    {
        int mainLane = mainNote.lane;
        List<int> availableLanes = new List<int>();

        for (int i = 0; i < spawner.lanePoints.Length; i++)
        {
            if (i != mainLane)
                availableLanes.Add(i);
        }

        if (availableLanes.Count == 0)
            return null;

        int doubleLane = availableLanes[prng.Next(0, availableLanes.Count)];

        NoteData doubleNote = new NoteData();
        doubleNote.time = mainNote.time;
        doubleNote.lane = doubleLane;

        return doubleNote;
    }

    void RemoveExactDuplicates(List<NoteData> notes)
    {
        if (notes.Count < 2) return;

        List<NoteData> uniqueNotes = new List<NoteData>();
        HashSet<string> seen = new HashSet<string>();

        foreach (var note in notes)
        {
            string key = $"{note.time:F6}_{note.lane}";
            if (!seen.Contains(key))
            {
                seen.Add(key);
                uniqueNotes.Add(note);
            }
        }

        notes.Clear();
        notes.AddRange(uniqueNotes);
    }

    float EstimateSustainScore(float[] mono, float sampleRate, float peakTime, float windowSec)
    {
        int center = Mathf.RoundToInt(peakTime * sampleRate);
        int half = Mathf.RoundToInt(windowSec * sampleRate * 0.5f);
        int start = Mathf.Clamp(center - half, 0, mono.Length - 1);
        int end = Mathf.Clamp(center + half, 0, mono.Length - 1);
        float prevEnergy = 0f;
        float energy = 0f;
        for (int i = start; i < end - 1; i++)
        {
            float e = mono[i] * mono[i];
            energy += e;
            prevEnergy += Mathf.Abs((mono[i + 1] * mono[i + 1]) - e);
        }
        if (energy <= 0.000001f) return 0f;
        return Mathf.Clamp01(1f - (prevEnergy / energy));
    }

    private void ReduceEarlyNotes()
    {
        if (generatedBeatMap.notes.Count <= 4)
        {
            // Si hay 4 notas o menos, eliminar todas
            generatedBeatMap.notes.Clear();
            return;
        }

        // Eliminar exactamente las primeras 4 notas
        generatedBeatMap.notes.RemoveRange(0, 4);
    }
}


//[System.Serializable]
//public class BeatMapDataWrapper
//{
//    public List<NoteData> notes;
//}

