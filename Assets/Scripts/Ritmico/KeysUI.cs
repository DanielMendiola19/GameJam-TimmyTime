using UnityEngine;
using TMPro;

public class KeysUI : MonoBehaviour
{
    [Header("Teclas Keys UI")]
    public TMP_Text Key_Carril_1;
    public TMP_Text Key_Carril_2;
    public TMP_Text Key_Carril_3;
    public TMP_Text Key_Carril_4;

    void Start()
    {
        // Obtener las teclas desde SettingsManager al iniciar la escena
        UpdateKeysDisplay();
    }

    void UpdateKeysDisplay()
    {
        if (SettingsManager.Instance == null)
        {
            Debug.LogWarning("SettingsManager no encontrado.");
            return;
        }

        Key_Carril_1.text = FormatKey(SettingsManager.Instance.GetKey("Carril 1"));
        Key_Carril_2.text = FormatKey(SettingsManager.Instance.GetKey("Carril 2"));
        Key_Carril_3.text = FormatKey(SettingsManager.Instance.GetKey("Carril 3"));
        Key_Carril_4.text = FormatKey(SettingsManager.Instance.GetKey("Carril 4"));
    }

    private string FormatKey(KeyCode key)
    {
        return key == KeyCode.None ? "Sin asignación" : key.ToString();
    }
}
