using UnityEngine;
using UnityEngine.SceneManagement;

public class CreditsScroller : MonoBehaviour
{
    public RectTransform creditsText;
    public float speed = 50f;
    public AudioSource bgMusic;
    public string sceneAfterCredits = "MainMenu";
    public float duration = 25f; // Duración antes de ir al MainMenu

    bool finished = false;

    void Start()
    {
        if (bgMusic != null)
            bgMusic.Play();

        // Llamar EndCredits después de "duration" segundos
        Invoke("EndCredits", duration);
    }

    void Update()
    {
        if (finished) return;

        creditsText.anchoredPosition += Vector2.up * speed * Time.deltaTime;
    }

    public void SkipCredits()
    {
        EndCredits();
    }

    private void EndCredits()
    {
        if (finished) return; // prevenir múltiples llamadas
        finished = true;

        if (bgMusic != null)
            bgMusic.Stop();

        SceneManager.LoadScene(sceneAfterCredits);
    }
}
