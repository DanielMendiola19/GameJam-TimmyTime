using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelPanelUI : MonoBehaviour
{
    public Button button;            // Botón de jugar nivel
    public Button cinematicButton;   // Botón de ver cinemática
    public Button deleteButton;      // NUEVO: Botón de borrar nivel custom
    public GameObject lockIcon;

    public TMP_Text levelNameText;
    public TMP_Text gradeText;
    public TMP_Text scoreText;
    public TMP_Text accuracyText;
    public TMP_Text dateText;
    public TMP_Text unlockRequirementText;

    public void SetLevelInfo(string name, string grade, int score, float accuracy, string date, bool unlocked, string requirement, bool isCustomLevel = false)
    {
        levelNameText.text = name;
        gradeText.text = grade;
        scoreText.text = score > 0 ? score.ToString() : "-";
        accuracyText.text = accuracy > 0 ? accuracy.ToString("F2") + "%" : "-";
        dateText.text = date;

        // requirement NUNCA vacío
        if (string.IsNullOrEmpty(requirement))
        {
            if (unlocked)
            {
                if (score > 0)
                    requirement = "Completado";
                else
                    requirement = "Disponible";
            }
            else
            {
                requirement = "Nivel bloqueado";
            }
        }

        unlockRequirementText.text = requirement;

        button.interactable = unlocked;
        if (lockIcon != null)
            lockIcon.SetActive(!unlocked);

        // Mostrar botón de borrar solo si es nivel custom
        if (deleteButton != null)
            deleteButton.gameObject.SetActive(isCustomLevel);
    }

    public void SetCinematicState(bool canPlayDirect, bool unlocked)
    {
        // Solo desbloquear botón de jugar si puede jugar directamente
        button.interactable = canPlayDirect;

        // Mostrar botón de cinemática solo si el nivel está desbloqueado
        if (cinematicButton != null)
            cinematicButton.gameObject.SetActive(unlocked);
    }
}
