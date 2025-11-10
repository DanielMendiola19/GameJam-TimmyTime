using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CinematicController : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public Button skipButton;
    public AudioSource videoAudioSource;


    private string levelID;

    void Start()
    {
        levelID = MenuManager.CinematicPass.levelID;

        string path = System.IO.Path.Combine(Application.streamingAssetsPath, "Cinematics", $"Cinematic_{levelID}.mp4");
        videoPlayer.url = path;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.SetTargetAudioSource(0, videoAudioSource);

        videoPlayer.Prepare(); // preparar el video antes de reproducir

        videoPlayer.prepareCompleted += OnVideoPrepared; // callback cuando termina de preparar

        skipButton.onClick.AddListener(OnSkip);
        skipButton.interactable = true;

        videoPlayer.loopPointReached += OnVideoEnded;
    }

    private void OnVideoPrepared(VideoPlayer vp)
    {
        videoPlayer.Play();
    }


    void OnVideoEnded(VideoPlayer vp)
    {
        GoToMenuWithSelector();
    }

    void OnSkip()
    {
        GoToMenuWithSelector();
    }

    private void GoToMenuWithSelector()
    {
        // Marcar cinemática como vista
        PlayerPrefs.SetInt("CinematicSeen_" + levelID, 1);
        PlayerPrefs.SetInt("ShowLevelSelect", 1); // mostrar selector de niveles al volver
        PlayerPrefs.Save();

        // Cargar menú principal
        SceneManager.LoadScene("MainMenu");
    }

}
