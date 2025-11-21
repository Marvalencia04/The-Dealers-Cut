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

    // 暴露参数名，要与 AudioMixer 中暴露的名字一致
    private const string MASTER_PARAM = "MasterVolume";
    private const string MUSIC_PARAM = "MusicVolume";
    private const string SFX_PARAM = "SFXVolume";

    // 用于保存到 PlayerPrefs 的键
    private const string MASTER_KEY = "MasterVolumeValue";
    private const string MUSIC_KEY = "MusicVolumeValue";
    private const string SFX_KEY = "SFXVolumeValue";

    private void Start()
    {
        // 1. 读取本地保存的音量（如果有）
        float master = PlayerPrefs.GetFloat(MASTER_KEY, 1f);
        float music = PlayerPrefs.GetFloat(MUSIC_KEY, 1f);
        float sfx = PlayerPrefs.GetFloat(SFX_KEY, 1f);

        // 2. 设置 Slider 初始值（0~1）
        if (masterSlider != null) masterSlider.value = master;
        if (musicSlider != null) musicSlider.value = music;
        if (sfxSlider != null) sfxSlider.value = sfx;

        // 3. 把 Slider 当前值应用到 AudioMixer
        SetMasterVolume(master);
        SetMusicVolume(music);
        SetSFXVolume(sfx);

        // 4. 注册 Slider 改变事件（也可以在 Inspector 的 OnValueChanged 手动拖）
        if (masterSlider != null) masterSlider.onValueChanged.AddListener(SetMasterVolume);
        if (musicSlider != null) musicSlider.onValueChanged.AddListener(SetMusicVolume);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(SetSFXVolume);
    }

    // 把 0~1 的值转换为 AudioMixer 的 dB（一般 -80dB ~ 0dB）
    private float LinearToDecibel(float value)
    {
        if (value <= 0.0001f)
            return -80f; // 静音

        return Mathf.Log10(value) * 20f;  // 标准公式
    }

    public void SetMasterVolume(float value)
    {
        float dB = LinearToDecibel(value);
        mainMixer.SetFloat(MASTER_PARAM, dB);
        PlayerPrefs.SetFloat(MASTER_KEY, value);
    }

    public void SetMusicVolume(float value)
    {
        float dB = LinearToDecibel(value);
        mainMixer.SetFloat(MUSIC_PARAM, dB);
        PlayerPrefs.SetFloat(MUSIC_KEY, value);
    }

    public void SetSFXVolume(float value)
    {
        float dB = LinearToDecibel(value);
        mainMixer.SetFloat(SFX_PARAM, dB);
        PlayerPrefs.SetFloat(SFX_KEY, value);
    }
}
