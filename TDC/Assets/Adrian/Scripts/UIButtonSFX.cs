using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIButtonSFX : MonoBehaviour, IPointerEnterHandler
{
    [Header("Sonidos del botón")]
    [SerializeField] private bool playHover = true;
    [SerializeField] private UIUISound onHoverSound = UIUISound.Hover;

    [SerializeField] private bool playClick = true;
    [SerializeField] private UIUISound onClickSound = UIUISound.Click;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.AddListener(() =>
            {
                if (!playClick) return;
                if (UIAudioManager.Instance) UIAudioManager.Instance.Play(onClickSound);
            });
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!playHover) return;
        if (UIAudioManager.Instance) UIAudioManager.Instance.Play(onHoverSound);
    }
}
