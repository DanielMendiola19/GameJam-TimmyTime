using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class KeyBindingsUI : MonoBehaviour
{
    [Header("Prefab para cada acción")]
    public GameObject keyButtonPrefab; // Prefab con Button + 2 TMP_Text (Descripción + Tecla)
    public Transform container;         // Contenedor vertical u horizontal

    [Header("Panel para presionar tecla")]
    public GameObject waitingPanel;     // Panel principal
    public TMP_Text waitingText;

    [Header("Panel de conflicto")]
    public Button btnConfirmar;         // Botón Sí
    public Button btnCancelar;          // Botón No

    private Dictionary<string, TMP_Text> actionToText = new Dictionary<string, TMP_Text>();
    private string waitingAction = null;
    private bool waitingForKey = false;

    private bool conflictActive = false;

    void Start()
    {
        SetupButtons();

        // Inicialmente ocultar botones de conflicto
        btnConfirmar.gameObject.SetActive(false);
        btnCancelar.gameObject.SetActive(false);
        waitingPanel.SetActive(false);
    }

    void SetupButtons()
    {
        // Diccionario de nombres amigables
        Dictionary<string, string> nombresAmigables = new Dictionary<string, string>()
        {
            { "Carril 1", "Carril 1" },
            { "Carril 2", "Carril 2" },
            { "Carril 3", "Carril 3" },
            { "Carril 4", "Carril 4" },
            { "Reiniciar", "Reiniciar nivel" },
            { "Pausa", "Pausar juego" }
        };

        foreach (var pair in nombresAmigables)
        {
            string action = pair.Key;
            string displayName = pair.Value;

            GameObject btnObj = Instantiate(keyButtonPrefab, container);

            TMP_Text[] textos = btnObj.GetComponentsInChildren<TMP_Text>();
            TMP_Text labelDescripcion = textos[0]; // Descripción
            TMP_Text btnText = textos[1];          // Tecla asignada

            labelDescripcion.text = displayName;
            btnText.text = SettingsManager.Instance.GetKey(action).ToString();

            actionToText[action] = btnText;

            Button btn = btnObj.GetComponentInChildren<Button>();
            btn.onClick.AddListener(() => StartRebind(action));
        }
    }

    void StartRebind(string action)
    {
        waitingAction = action;
        waitingForKey = true;
        waitingPanel.SetActive(true);
        waitingText.text = $"Presiona una tecla para asignar a '{action}'";
    }

    void Update()
    {
        if (waitingForKey && waitingAction != null && !conflictActive)
        {
            foreach (KeyCode k in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(k))
                {
                    string mensaje;
                    if (!SettingsManager.Instance.SetKey(waitingAction, k, out mensaje))
                    {
                        // Hay conflicto → mostrar botones de confirmación
                        conflictActive = true; // bloqueamos la captura de teclas
                        StartCoroutine(ShowConflictDialog(k, mensaje));
                    }
                    else
                    {
                        // Asignación exitosa
                        UpdateButtonText(waitingAction);
                        waitingForKey = false;
                        waitingPanel.SetActive(false);
                        waitingAction = null;
                    }
                    break;
                }
            }
        }
    }

    IEnumerator ShowConflictDialog(KeyCode key, string mensaje)
    {
        waitingText.text = mensaje;
        btnConfirmar.gameObject.SetActive(true);
        btnCancelar.gameObject.SetActive(true);

        bool decisionTomada = false;
        bool asignar = false;

        btnConfirmar.onClick.RemoveAllListeners();
        btnCancelar.onClick.RemoveAllListeners();

        btnConfirmar.onClick.AddListener(() => { asignar = true; decisionTomada = true; });
        btnCancelar.onClick.AddListener(() => { asignar = false; decisionTomada = true; });

        while (!decisionTomada) yield return null;

        if (asignar)
        {
            // ahora recibimos la acción que quedó vacía
            string vaciada = SettingsManager.Instance.ForceSetKey(waitingAction, key);

            // actualizar UI de ambas acciones
            UpdateButtonText(waitingAction);
            if (!string.IsNullOrEmpty(vaciada))
                UpdateButtonText(vaciada);
        }

        btnConfirmar.gameObject.SetActive(false);
        btnCancelar.gameObject.SetActive(false);
        waitingPanel.SetActive(false);

        waitingAction = null;
        waitingForKey = false;
        conflictActive = false;
    }


    void UpdateButtonText(string action)
    {
        if (actionToText.ContainsKey(action))
        {
            KeyCode key = SettingsManager.Instance.GetKey(action);
            actionToText[action].text = key == KeyCode.None ? "Sin asignación" : key.ToString();
        }
    }
}
