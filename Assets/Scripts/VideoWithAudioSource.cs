using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class VideoWithAudioSource : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public AudioSource audioSource;

    [Header("Next Scene Config")]
    public string sceneToLoadAfter = "MainMenu";

    void Start()
    {
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.SetTargetAudioSource(0, audioSource);

        videoPlayer.Play();
        audioSource.Play();

        videoPlayer.loopPointReached += OnVideoEnded;
    }

    void OnVideoEnded(VideoPlayer vp)
    {
        GoToMenuWithSelector();
    }

    private void GoToMenuWithSelector()
    {
        if (string.IsNullOrEmpty(sceneToLoadAfter))
            sceneToLoadAfter = "MainMenu";

        SceneManager.LoadScene(sceneToLoadAfter);
    }
}
