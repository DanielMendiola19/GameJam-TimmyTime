using UnityEngine;
using UnityEngine.UI;

public class HealthSystem : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float healthPerPerfect = 5f;
    public float healthPerGreat = 2f;
    public float healthPerFail = -10f;
    public float healthPerMiss = -15f;
    

    [Header("UI References")]
    public Slider healthBar;
    public Image healthFill;
    public Color highHealthColor = Color.green;
    public Color mediumHealthColor = Color.yellow;
    public Color lowHealthColor = Color.red;

    [Header("Handle Sprites")]
    public Image sliderHandleImage;   // este es el component Image del handle
    public Sprite timmyLow;
    public Sprite timmyMid;
    public Sprite timmyHigh;


    private float currentHealth;
    private bool isGameOver = false;

    void Start()
    {

        currentHealth = maxHealth * 0.25f;
        // forzar que la primera cara sea la desconectada baja
        UpdateHealthBar();
    }

    void Update()
    {
        if (!isGameOver)
        {
            
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
            UpdateHealthBar();

            // Verificar game over
            if (currentHealth <= 0)
            {
                GameOver();
            }
        }
    }

    public void AddHealth(float amount)
    {
        if (!isGameOver)
        {
            currentHealth += amount;
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
            UpdateHealthBar();
        }
    }

    public void OnNoteHit(string hitType)
    {
        switch (hitType)
        {
            case "PERFECT":
                AddHealth(healthPerPerfect);
                break;
            case "GREAT":
                AddHealth(healthPerGreat);
                break;
        }
    }

    public void OnNoteFail(string failType)
    {
        switch (failType)
        {
            case "FAIL":
                AddHealth(healthPerFail);
                break;
            case "MISS":
                AddHealth(healthPerMiss);
                break;
        }
    }

    private void UpdateHealthBar()
    {
        float healthPercent = currentHealth / maxHealth;

        if (healthBar != null)
            healthBar.value = healthPercent;

        // Fill color
        if (healthFill != null)
        {
            if (healthPercent > 0.9f)
                healthFill.color = highHealthColor;
            else if (healthPercent > 0.4f)
                healthFill.color = mediumHealthColor;
            else
                healthFill.color = lowHealthColor;
        }

        // ---- CAMBIO SEGUN VIDA ----
        if (sliderHandleImage != null)
        {
            if (healthPercent > 0.9f)
            {
                sliderHandleImage.sprite = timmyHigh;
            }
            else if (healthPercent > 0.4f)
            {
                sliderHandleImage.sprite = timmyMid;
            }
            else
            {
                sliderHandleImage.sprite = timmyLow;
            }
        }
    }


    private void GameOver()
    {
        isGameOver = true;

        // primero intenta el CustomGameManager
        var customGM = FindObjectOfType<CustomGameManager>();
        if (customGM != null)
        {
            customGM.ShowGameOver();
            return;
        }

        // si no hay custom, usa el normal (story)
        var storyGM = FindObjectOfType<GameManager>();
        if (storyGM != null)
        {
            storyGM.ShowGameOver();
            return;
        }

        Debug.LogWarning("No se encontró ningún GameManager para ejecutar GameOver");
    }


    public void ResetHealth()
    {
        currentHealth = maxHealth;
        isGameOver = false;
        UpdateHealthBar();
    }
}