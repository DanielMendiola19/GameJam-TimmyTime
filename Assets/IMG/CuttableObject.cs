using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CuttableObject : MonoBehaviour, IPointerDownHandler
{
    public bool isGood; // true = good, false = bad
    private Image img;
    private GameManager gm;

    private void Awake()
    {
        img = GetComponent<Image>();
        gm = FindObjectOfType<GameManager>(); // encuentra el GameManager
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isGood)
        {
            img.color = Color.blue;
            gm.HitGoodObject(gameObject);
        }
        else
        {
            img.color = Color.red;
            gm.HitBadObject(gameObject);
        }
    }
}
