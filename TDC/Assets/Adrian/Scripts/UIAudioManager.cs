using UnityEngine;

public enum UIUISound
{
    Hover,
    Click,
    Back,
    Confirm,
    Cancel,
    Error
}

public class UIAudioManager : MonoBehaviour
{
    public static UIAudioManager Instance { get; private set; }

    [Header("AudioSource (2D)")]
    [SerializeField] private AudioSource uiSource;

    [Header("Clips")]
    [SerializeField] private AudioClip hoverClip;
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private AudioClip backClip;
    [SerializeField] private AudioClip confirmClip;
    [SerializeField] private AudioClip cancelClip;
    [SerializeField] private AudioClip errorClip;

    [Header("Volúmenes")]
    [Range(0f, 1f)][SerializeField] private float hoverVolume = 0.1f;
    [Range(0f, 1f)][SerializeField] private float clickVolume = 0.5f;
    [Range(0f, 1f)][SerializeField] private float backVolume = 0.2f;
    [Range(0f, 1f)][SerializeField] private float confirmVolume = 0.5f;
    [Range(0f, 1f)][SerializeField] private float cancelVolume = 0.3f;
    [Range(0f, 1f)][SerializeField] private float errorVolume = 0.6f;

    [Header("Variación (opcional)")]
    [SerializeField] private Vector2 pitchRange = new Vector2(0.98f, 1.02f);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (!uiSource) uiSource = GetComponent<AudioSource>();
    }

    public void Play(UIUISound sound)
    {
        if (!uiSource) return;

        var (clip, vol) = GetClipAndVolume(sound);
        if (!clip || vol <= 0f) return;

        float oldPitch = uiSource.pitch;
        uiSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        uiSource.PlayOneShot(clip, vol);
        uiSource.pitch = oldPitch;
    }

    private (AudioClip clip, float vol) GetClipAndVolume(UIUISound sound)
    {
        switch (sound)
        {
            case UIUISound.Hover: return (hoverClip, hoverVolume);
            case UIUISound.Click: return (clickClip, clickVolume);
            case UIUISound.Back: return (backClip, backVolume);
            case UIUISound.Confirm: return (confirmClip, confirmVolume);
            case UIUISound.Cancel: return (cancelClip, cancelVolume);
            case UIUISound.Error: return (errorClip, errorVolume);
            default: return (clickClip, clickVolume);
        }
    }
}
