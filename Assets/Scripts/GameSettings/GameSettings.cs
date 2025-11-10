using UnityEngine;

[CreateAssetMenu(fileName = "GameSettings", menuName = "Settings/GameSettings")]
public class GameSettings : ScriptableObject
{
    [Range(0, 50)]
    public int volume = 25;

    [Range(0, 50)]
    public int brightness = 25;

    // Agrega más configuraciones aquí en el futuro
}
