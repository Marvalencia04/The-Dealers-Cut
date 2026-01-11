using UnityEngine;

[CreateAssetMenu(fileName = "HelpPage", menuName = "UI/Help Page")]
public class HelpPageData : ScriptableObject
{
  
    public Sprite image;
    [TextArea(3, 15)]
    public string text;
}
