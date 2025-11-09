using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public Image[] lifeImages; // 5 imágenes que representan las vidas
    public Text scoreText;
    public int maxGoodHits = 5;
    public int targetScore = 20;

    private int goodHits = 0;
    private int score = 0;

    public void HitGoodObject(GameObject obj)
    {
        goodHits++;

        // Quita la vida visual
        if (goodHits - 1 < lifeImages.Length)
            lifeImages[goodHits - 1].enabled = false;

        Destroy(obj);

        // Si supera el máximo permitido → pierde
        if (goodHits >= maxGoodHits)
        {
            Debug.Log("¡Perdiste!");
            // opcional: Time.timeScale = 0f; para pausar
        }
    }

    public void HitBadObject(GameObject obj)
    {
        score++;
        scoreText.text = "Score: " + score;

        Destroy(obj);

        // Si alcanza el puntaje mínimo → gana
        if (score >= targetScore)
        {
            Debug.Log("¡Ganaste!");
            // opcional: Time.timeScale = 0f; para pausar
        }
    }
}
