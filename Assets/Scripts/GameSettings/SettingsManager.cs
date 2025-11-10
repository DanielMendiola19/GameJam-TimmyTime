using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance;

    public GameSettings settings;

    [Header("Opcional: Prefab de brillo global")]
    public GameObject brightnessPanelPrefab;

    private Image brightnessOverlay;

    // Keybindings
    private Dictionary<string, KeyCode> keyBindings = new Dictionary<string, KeyCode>();

    // Teclas que no se pueden usar
    private KeyCode[] teclasInvalidas = { KeyCode.LeftAlt, KeyCode.RightAlt, KeyCode.Backspace, KeyCode.Mouse0, 
        KeyCode.Mouse1, KeyCode.Mouse2, KeyCode.Mouse3, KeyCode.Mouse4, KeyCode.Mouse5 };

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSettings();
            SetupBrightnessPanel();
            ApplyVolume();
            ApplyBrightness();
            LoadKeyBindings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void SetupBrightnessPanel()
    {
        if (brightnessPanelPrefab == null) return;

        // Instanciamos panel si no existe
        GameObject existing = GameObject.Find("BrightnessOverlay");
        if (existing == null)
        {
            GameObject canvasObj = Instantiate(brightnessPanelPrefab);
            canvasObj.name = "BrightnessOverlay";
            DontDestroyOnLoad(canvasObj);

            // Aseguramos que siempre esté encima
            Canvas canvas = canvasObj.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = 9999; // número alto para que quede arriba de todo
            }

            // Tomamos el Image hijo para aplicar brillo
            brightnessOverlay = canvasObj.GetComponentInChildren<Image>();
            if (brightnessOverlay == null)
            {
                Debug.LogWarning("El prefab BrightnessPanel necesita un Image en su hijo para funcionar.");
            }
        }
        else
        {
            brightnessOverlay = existing.GetComponentInChildren<Image>();

            // También aseguramos que siga encima si ya existía
            Canvas canvas = existing.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = 9999;
            }
        }
    }

    public void SetVolume(int value)
    {
        settings.volume = Mathf.Clamp(value, 0, 50);
        PlayerPrefs.SetInt("Volume", settings.volume);
        ApplyVolume();
    }

    public void SetBrightness(int value)
    {
        settings.brightness = Mathf.Clamp(value, 0, 50);
        PlayerPrefs.SetInt("Brightness", settings.brightness);
        ApplyBrightness();
    }

    void ApplyVolume()
    {
        AudioListener.volume = settings.volume / 50f;
    }

    void ApplyBrightness()
    {
        if (brightnessOverlay == null) return;

        float midValue = 25f;  // valor normal
        float t = settings.brightness;

        Color c = brightnessOverlay.color;

        if (t < midValue)
        {
            // Oscuro → un poquito más intenso que antes
            float alpha = Mathf.Lerp(0.6f, 0f, t / midValue); // 0 = oscuro, 25 = normal
            c = Color.black;
            c.a = alpha;
        }
        else
        {
            // Luminoso → mucho más suave que antes para que no sea artificial
            float alpha = Mathf.Lerp(0f, 0.05f, (t - midValue) / (50f - midValue)); // 25 = normal, 50 = ligeramente más brillante
            c = Color.white;
            c.a = alpha;
        }

        brightnessOverlay.color = c;
    }



    public int GetVolume() => settings.volume;
    public int GetBrightness() => settings.brightness;

    void LoadSettings()
    {
        settings.volume = PlayerPrefs.GetInt("Volume", settings.volume);
        settings.brightness = PlayerPrefs.GetInt("Brightness", settings.brightness);
    }


    #region KEYBINDINGS

    private void LoadKeyBindings()
    {
        // Definimos acciones
        string[] acciones = { "Carril 1", "Carril 2", "Carril 3", "Carril 4", "Reiniciar", "Pausa" };
        KeyCode[] defaults = { KeyCode.D, KeyCode.F, KeyCode.J, KeyCode.K, KeyCode.R, KeyCode.Escape };

        for (int i = 0; i < acciones.Length; i++)
        {
            string action = acciones[i];
            string keyStr = PlayerPrefs.GetString("Key_" + action, "");

            KeyCode key;
            if (!string.IsNullOrEmpty(keyStr) && System.Enum.TryParse(keyStr, out key))
            {
                keyBindings[action] = key;
            }
            else
            {
                keyBindings[action] = defaults[i];
                PlayerPrefs.SetString("Key_" + action, defaults[i].ToString());
            }
        }

        PlayerPrefs.Save();
    }

    public bool SetKey(string action, KeyCode newKey, out string mensaje)
    {
        mensaje = "";

        // Validar tecla inválida
        foreach (var t in teclasInvalidas)
        {
            if (newKey == t)
            {
                mensaje = "Tecla no permitida.";
                return false;
            }
        }

        // Validar si ya está asignada a otra acción
        foreach (var kvp in keyBindings)
        {
            if (kvp.Value == newKey)
            {
                mensaje = $"La tecla {newKey} ya está asignada a '{kvp.Key}'. ¿Deseas reasignarla?";
                // Para reasignar debemos avisar al UI que confirme
                // Aquí no se cambia hasta que el usuario confirme
                return false;
            }
        }

        // Asignación válida
        keyBindings[action] = newKey;
        PlayerPrefs.SetString("Key_" + action, newKey.ToString());
        PlayerPrefs.Save();
        mensaje = $"Tecla '{newKey}' asignada a '{action}'.";
        return true;
    }


    public string ForceSetKey(string action, KeyCode newKey)
    {
        // Bloquear teclas no permitidas incluso en force
        foreach (var t in teclasInvalidas)
        {
            if (newKey == t)
                return null; // simplemente no hacer nada si es una tecla prohibida
        }

        string conflictedAction = null;

        // Recorrer una copia de las claves para poder modificar el diccionario
        foreach (var key in new List<string>(keyBindings.Keys))
        {
            if (keyBindings[key] == newKey && key != action)
            {
                conflictedAction = key;
                keyBindings[conflictedAction] = KeyCode.None;
                PlayerPrefs.SetString("Key_" + conflictedAction, "None");
            }
        }

        keyBindings[action] = newKey;
        PlayerPrefs.SetString("Key_" + action, newKey.ToString());
        PlayerPrefs.Save();

        return conflictedAction; // acción que quedó vacía
    }




    // Modificar GetKey para que nunca tire error aunque no haya asignación
    public KeyCode GetKey(string action)
    {
        if (keyBindings.ContainsKey(action))
            return keyBindings[action];
        return KeyCode.None;
    }


    private KeyCode GetDefaultKey(string action)
    {
        switch (action)
        {
            case "Carril 1": return KeyCode.D;
            case "Carril 2": return KeyCode.F;
            case "Carril 3": return KeyCode.J;
            case "Carril 4": return KeyCode.K;
            case "Reiniciar": return KeyCode.R;
            case "Pausa": return KeyCode.Escape;
            default: return KeyCode.None;
        }
    }

    #endregion

}
