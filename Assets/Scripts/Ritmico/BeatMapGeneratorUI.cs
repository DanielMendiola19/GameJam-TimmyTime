using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif



[RequireComponent(typeof(AudioSource))]
public class BeatMapGeneratorUI : MonoBehaviour
{
    public AudioSource song;
    public AudioSource tapAudio;
    public BeatMap generatedBeatMap;
    public LaneReferenceSpawner laneSpawner;

    [Header("Análisis de audio")]
    public int fftSize = 1024;
    public int hopSize = 512;
    public float minNoteSpacing = 0.08f;
    public float peakThresholdMultiplier = 1.4f;
    public float sustainedSuppressFactor = 0.6f;
    public float sustainedEnergyWindow = 0.25f;

    private float bpm = 0f;

    [Header("Cuantización")]
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

    [Header("Configuración de Notas")]
    public float fallDistance = 10f;
    public float noteSpeed = 5f;

    [SerializeField] private bool playTapSound = false;

    void Start()
    {

    }

    IEnumerator StartWithCountIn()
    {
        float tapInterval = countInTime / tapCount;
        for (int i = 0; i < tapCount; i++)
        {
            if (playTapSound) tapAudio.Play();
            yield return new WaitForSeconds(tapInterval);
        }
        song.Play();
    }

    public string GetSongKey()
    {
        if (song.clip == null) return "unknown";
        return $"{song.clip.name}_{bpm}_{song.clip.length:F2}_{song.clip.frequency}";
    }

    public bool TryLoadExistingBeatMap(string customLevelFolder)
    {
        if (song.clip == null) return false;

        string filePath = Path.Combine(customLevelFolder, "beatmap.json");
        if (!File.Exists(filePath)) return false;

        string json = File.ReadAllText(filePath);
        BeatMapDataWrapper wrapper = JsonUtility.FromJson<BeatMapDataWrapper>(json);
        generatedBeatMap.notes.Clear();
        foreach (var note in wrapper.notes)
            generatedBeatMap.notes.Add(new NoteData { time = note.time, lane = note.lane });

        Debug.Log($"[Build] Beatmap cargado desde: {filePath}");
        return true;
    }


    // Nuevo método preparado para el nuevo sistema
    public void SaveBeatMap(string customLevelFolder)
    {
        if (song.clip == null || generatedBeatMap.notes.Count == 0)
        {
            Debug.LogWarning("No hay beatmap para guardar.");
            return;
        }

        // Asegurarse de que la carpeta exista
        if (!Directory.Exists(customLevelFolder))
            Directory.CreateDirectory(customLevelFolder);

        string filePath = Path.Combine(customLevelFolder, "beatmap.json");

        BeatMapDataWrapper wrapper = new BeatMapDataWrapper();
        wrapper.notes = generatedBeatMap.notes;

        string json = JsonUtility.ToJson(wrapper, true);
        File.WriteAllText(filePath, json);

        Debug.Log($"[Build] BeatMap guardado en: {filePath}");
    }

#if UNITY_EDITOR
    public void SaveBeatMapAsSO()
    {
        if (song.clip == null || generatedBeatMap.notes.Count == 0)
        {
            Debug.LogWarning("No hay beatmap para guardar como ScriptableObject.");
            return;
        }

        string songKey = GetSongKey();
        string assetFolder = "Assets/Beatmaps/Levels";

        if (!Directory.Exists(assetFolder))
            Directory.CreateDirectory(assetFolder);

        BeatMap newAsset = ScriptableObject.CreateInstance<BeatMap>();

        // copiar notas
        newAsset.notes = new List<NoteData>(generatedBeatMap.notes);

        string assetPath = $"{assetFolder}/{songKey}.asset";

        AssetDatabase.CreateAsset(newAsset, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[StoryEditor] BeatMap generado como ScriptableObject: {assetPath}");
    }
#endif



    public void GenerateBeatMap()
    {
        if (song.clip == null) { Debug.LogError("No clip asignado."); return; }

        generatedBeatMap.notes.Clear();
        System.Random prng = new System.Random(song.clip.name.GetHashCode());

        // --- Calcular fallTime igual que en el original ---
        float fallTime = Mathf.Max(0.001f, fallDistance / noteSpeed);

        // --- Extraer audio mono (igual que original) ---
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

        float songDuration = song.clip.length;
        float endCutoffTime = songDuration - secondsBeforeEndToStop;

        // --- Preparar ventanas y FFT (igual que original) ---
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

        // --- Spectral Flux (igual que original) ---
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
                if (enableEndFilter && time > endCutoffTime) continue;
                onsetTimes.Add(time);
                onsetEnergies.Add(energies[f]);
            }
        }

        // --- GENERACIÓN DE NOTAS (IGUAL QUE ORIGINAL) ---
        float lastNoteTime = -999f;

        for (int i = 0; i < onsetTimes.Count; i++)
        {
            float peakTime = onsetTimes[i];
            float energy = onsetEnergies[i];

            // Verificar spacing mínimo (igual que original)
            if (peakTime - lastNoteTime < minNoteSpacing) continue;

            // Verificar sustain score (igual que original)
            float sustainScore = EstimateSustainScore(mono, song.clip.frequency, peakTime, sustainedEnergyWindow);
            if (sustainScore > 0.7f && prng.NextDouble() < sustainedSuppressFactor) continue;

            // Generar nota principal (con fallTime como original)
            NoteData mainNote = new NoteData();
            mainNote.time = Mathf.Max(0f, peakTime - fallTime);
            mainNote.lane = prng.Next(0, laneSpawner.lanePoints.Length);
            generatedBeatMap.notes.Add(mainNote);

            // Generar nota doble (igual que original)
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

        // --- Generar notas para count-in (igual que original) ---
        float preStartTime = countInTime - fallTime;
        int extraNotes = 0;
        while (extraNotes * (60f / bpm / quantizeDivisions) < preStartTime)
        {
            NoteData note = new NoteData();
            note.time = extraNotes * (60f / bpm / quantizeDivisions);
            note.lane = prng.Next(0, laneSpawner.lanePoints.Length);
            generatedBeatMap.notes.Add(note);
            extraNotes++;
        }

        // --- Cuantizar según BPM (igual que original) ---
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

        RemoveExactDuplicates(generatedBeatMap.notes);
        if (enableEndFilter) RemoveNotesAtEnd(generatedBeatMap.notes, songDuration);
        ReduceEarlyNotes();

        Debug.Log($"BeatMap generado: {generatedBeatMap.notes.Count} notas. BPM usado: {bpm}. Corte final: {secondsBeforeEndToStop}s");
    }

    private NoteData GenerateDoubleNote(NoteData mainNote, System.Random prng)
    {
        int mainLane = mainNote.lane;
        List<int> availableLanes = new List<int>();

        for (int i = 0; i < laneSpawner.lanePoints.Length; i++)
        {
            if (i != mainLane)
                availableLanes.Add(i);
        }

        if (availableLanes.Count == 0) return null;

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
        if (removedCount > 0) Debug.Log($"Eliminadas {removedCount} notas del final de la canción");
    }

    private void ReduceEarlyNotes()
    {
        if (generatedBeatMap.notes.Count <= 4)
        {
            generatedBeatMap.notes.Clear();
            return;
        }
        generatedBeatMap.notes.RemoveRange(0, 4);
    }

    public void SetBPM(float newBPM)
    {
        if (newBPM <= 0f)
        {
            Debug.LogWarning("BPM inválido en BeatMapGeneratorUI");
            return;
        }
        bpm = newBPM;
    }

    public void SetSongClip(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogError("Clip nulo al asignar canción en BeatMapGeneratorUI");
            return;
        }
        song.clip = clip;
        song.time = 0f;
    }
}

[System.Serializable]
public class BeatMapDataWrapper
{
    public List<NoteData> notes;
}


