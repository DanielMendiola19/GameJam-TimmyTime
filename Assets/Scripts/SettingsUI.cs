using UnityEngine;
using UnityEngine.UI;
using TMPro; // <-- Esto es necesario para TextMeshPro

public class SettingsUI : MonoBehaviour
{
    public Slider volumeSlider;
    public Slider brightnessSlider;

    public TMP_Text volumeText;      // TextMeshPro para mostrar valor de volumen
    public TMP_Text brightnessText;  // TextMeshPro para mostrar valor de brillo

    private void Start()
    {
        // Configuración de sliders
        volumeSlider.minValue = 0;
        volumeSlider.maxValue = 50;
        volumeSlider.wholeNumbers = true;

        brightnessSlider.minValue = 0;
        brightnessSlider.maxValue = 50;
        brightnessSlider.wholeNumbers = true;

        // Inicializar sliders con valores guardados
        volumeSlider.value = SettingsManager.Instance.GetVolume();
        brightnessSlider.value = SettingsManager.Instance.GetBrightness();

        // Actualizar textos iniciales
        UpdateVolumeText((int)volumeSlider.value);
        UpdateBrightnessText((int)brightnessSlider.value);

        // Suscribirse a los cambios
        volumeSlider.onValueChanged.AddListener(val =>
        {
            int intVal = (int)val;
            SettingsManager.Instance.SetVolume(intVal);
            UpdateVolumeText(intVal);
        });

        brightnessSlider.onValueChanged.AddListener(val =>
        {
            int intVal = (int)val;
            SettingsManager.Instance.SetBrightness(intVal);
            UpdateBrightnessText(intVal);
        });
    }

    private void UpdateVolumeText(int value)
    {
        if (volumeText != null)
            volumeText.text = value.ToString();
    }

    private void UpdateBrightnessText(int value)
    {
        if (brightnessText != null)
            brightnessText.text = value.ToString();
    }
}
