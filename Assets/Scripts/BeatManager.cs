using UnityEngine;

public class BeatManager : MonoBehaviour
{
    public AudioSource musicSource;
    public float bpm = 128f;

    private float secondsPerBeat;
    private float songStartTime;
    private float songPosition;

    void Start()
    {
        secondsPerBeat = 60f / bpm;
        songStartTime = (float)AudioSettings.dspTime;
        musicSource.Play();
    }

    void Update()
    {
        songPosition = (float)(AudioSettings.dspTime - songStartTime);
    }

    public bool IsOnBeat(float tolerance = 0.05f)
    {
        float beatPosition = songPosition / secondsPerBeat;
        return Mathf.Abs(beatPosition - Mathf.Round(beatPosition)) < tolerance;
    }

    public float GetSongTime()
    {
        return songPosition;
    }
}
