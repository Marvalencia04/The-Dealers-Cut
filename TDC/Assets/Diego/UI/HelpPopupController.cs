using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HelpPopupController : MonoBehaviour
{
    [Header("Data")]
    public HelpBookData helpBook;

    [Header("UI")]
    public GameObject helpPanel;
    
    public Image contentImage;
    public TMP_Text contentText;
    public TMP_Text pageIndicator;
    public Button btnPrev;
    public Button btnNext;
    public Button btnClose;

    private int index;

    private void Awake()
    {
        btnPrev.onClick.AddListener(Prev);
        btnNext.onClick.AddListener(Next);
        btnClose.onClick.AddListener(Hide);

        if (helpPanel != null) helpPanel.SetActive(false);
    }

    public void Show()
    {
        if (helpBook == null || helpBook.pages == null || helpBook.pages.Length == 0)
        {
            Debug.LogWarning("HelpBook is empty or not assigned.");
            return;
        }

        index = 0;
        helpPanel.SetActive(true);
        Refresh();
    }

    public void Hide()
    {
        helpPanel.SetActive(false);
    }

    private void Prev()
    {
        if (index <= 0) return;
        index--;
        Refresh();
    }

    private void Next()
    {
        if (index >= helpBook.pages.Length - 1) return;
        index++;
        Refresh();
    }

    private void Refresh()
    {
        var page = helpBook.pages[index];
        int total = helpBook.pages.Length;

       
        if (contentImage != null)
        {
            contentImage.sprite = page.image;
            contentImage.enabled = page.image != null;
        }
        if (contentText != null) contentText.text = page.text ?? "";
        if (pageIndicator != null) pageIndicator.text = $"{index + 1} / {total}";

        if (btnPrev != null) btnPrev.interactable = index > 0;
        if (btnNext != null) btnNext.interactable = index < total - 1;
    }
}
