using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioSettingsManager : MonoBehaviour
{
    [Header("Audio Mixer")]
    public AudioMixer mainMixer;

    [Header("Sliders")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;
    public Slider ambientSlider;

    // Parámetros expuestos en el AudioMixer
    private const string MASTER_PARAM = "MasterVolume";
    private const string MUSIC_PARAM  = "MusicVolume";
    private const string SFX_PARAM    = "SFXVolume";
    private const string AMBIENT_PARAM = "AmbientVolume";

    // Claves PlayerPrefs
    private const string MASTER_KEY  = "MasterVolumeValue";
    private const string MUSIC_KEY   = "MusicVolumeValue";
    private const string SFX_KEY     = "SFXVolumeValue";
    private const string AMBIENT_KEY = "AmbientVolumeValue";

    private void Start()
    {
        // 1. Cargar valores guardados (0–1)
        float master  = PlayerPrefs.GetFloat(MASTER_KEY, 1f);
        float music   = PlayerPrefs.GetFloat(MUSIC_KEY, 1f);
        float sfx     = PlayerPrefs.GetFloat(SFX_KEY, 1f);
        float ambient = PlayerPrefs.GetFloat(AMBIENT_KEY, 1f);

        // 2. Asignar sliders
        if (masterSlider  != null) masterSlider.value  = master;
        if (musicSlider   != null) musicSlider.value   = music;
        if (sfxSlider     != null) sfxSlider.value     = sfx;
        if (ambientSlider != null) ambientSlider.value = ambient;

        // 3. Aplicar volúmenes al mixer
        SetMasterVolume(master);
        SetMusicVolume(music);
        SetSFXVolume(sfx);
        SetAmbientVolume(ambient);

        // 4. Listeners
        if (masterSlider  != null) masterSlider.onValueChanged.AddListener(SetMasterVolume);
        if (musicSlider   != null) musicSlider.onValueChanged.AddListener(SetMusicVolume);
        if (sfxSlider     != null) sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        if (ambientSlider != null) ambientSlider.onValueChanged.AddListener(SetAmbientVolume);
    }

    // Convierte 0–1 a dB (-80 a 0)
    private float LinearToDecibel(float value)
    {
        if (value <= 0.0001f)
            return -80f;

        return Mathf.Log10(value) * 20f;
    }

    public void SetMasterVolume(float value)
    {
        mainMixer.SetFloat(MASTER_PARAM, LinearToDecibel(value));
        PlayerPrefs.SetFloat(MASTER_KEY, value);
    }

    public void SetMusicVolume(float value)
    {
        mainMixer.SetFloat(MUSIC_PARAM, LinearToDecibel(value));
        PlayerPrefs.SetFloat(MUSIC_KEY, value);
    }

    public void SetSFXVolume(float value)
    {
        mainMixer.SetFloat(SFX_PARAM, LinearToDecibel(value));
        PlayerPrefs.SetFloat(SFX_KEY, value);
    }

    public void SetAmbientVolume(float value)
    {
        mainMixer.SetFloat(AMBIENT_PARAM, LinearToDecibel(value));
        PlayerPrefs.SetFloat(AMBIENT_KEY, value);
    }
}
