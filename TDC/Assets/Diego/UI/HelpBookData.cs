using UnityEngine;

[CreateAssetMenu(fileName = "HelpBook", menuName = "UI/Help Book")]
public class HelpBookData : ScriptableObject
{
    public HelpPageData[] pages;
}
