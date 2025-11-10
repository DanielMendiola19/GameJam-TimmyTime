using UnityEngine;

public class PanelManager : MonoBehaviour
{
    /// <summary>
    /// Muestra un panel y oculta otro.
    /// </summary>
    /// <param name="panelToShow">Panel que quieres mostrar</param>
    /// <param name="panelToHide">Panel que quieres ocultar</param>
    public void ShowPanel(GameObject panelToShow, GameObject panelToHide)
    {
        if (panelToShow != null)
            panelToShow.SetActive(true);

        if (panelToHide != null)
            panelToHide.SetActive(false);
    }

    /// <summary>
    /// Solo muestra un panel sin ocultar nada más.
    /// </summary>
    public void ShowPanel(GameObject panelToShow)
    {
        if (panelToShow != null)
            panelToShow.SetActive(true);
    }

    /// <summary>
    /// Solo oculta un panel sin mostrar nada más.
    /// </summary>
    public void HidePanel(GameObject panelToHide)
    {
        if (panelToHide != null)
            panelToHide.SetActive(false);
    }
}
