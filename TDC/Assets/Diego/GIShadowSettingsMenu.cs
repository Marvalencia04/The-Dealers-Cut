using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 图形设置菜单：启用 / 禁用 “GI 阴影” 的视觉效果
/// 适用于：Baked GI + Mixed Lighting（Shadowmask）
/// 说明：无法真正关闭烘焙 GI，只是关闭实时阴影并调整环境光。
/// </summary>
public class GIShadowSettingsMenu : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("控制 GI 阴影的 Toggle")]
    public Toggle giShadowToggle;

    [Header("需要被控制的光源（不填则自动查找场景所有 Light）")]
    public Light[] targetLights;

    private const string PREF_KEY = "GI_SHADOW_ENABLED";

    // 记录默认值，方便恢复
    private ShadowQuality defaultShadowQuality;
    private float defaultAmbientIntensity;
    private LightShadows[] defaultLightShadows;

    private void Awake()
    {
        // 如果没手动指定光源，就自动抓场景里所有 Light
        if (targetLights == null || targetLights.Length == 0)
        {
            targetLights = FindObjectsOfType<Light>();
        }

        // 记录默认值
        defaultShadowQuality = QualitySettings.shadows;
        defaultAmbientIntensity = RenderSettings.ambientIntensity;

        defaultLightShadows = new LightShadows[targetLights.Length];
        for (int i = 0; i < targetLights.Length; i++)
        {
            if (targetLights[i] != null)
                defaultLightShadows[i] = targetLights[i].shadows;
        }

        // 从本地读取上次设置（默认开启 GI 阴影）
        bool enabled = PlayerPrefs.GetInt(PREF_KEY, 1) == 1;

        // 初始化 UI & 绑定事件
        if (giShadowToggle != null)
        {
            giShadowToggle.isOn = enabled;
            giShadowToggle.onValueChanged.AddListener(OnToggleChanged);
        }

        // 应用一次当前状态
        ApplyGIShadow(enabled);
    }

    private void OnDestroy()
    {
        if (giShadowToggle != null)
        {
            giShadowToggle.onValueChanged.RemoveListener(OnToggleChanged);
        }
    }

    private void OnToggleChanged(bool value)
    {
        PlayerPrefs.SetInt(PREF_KEY, value ? 1 : 0);
        PlayerPrefs.Save();

        ApplyGIShadow(value);
    }

    /// <summary>
    /// 打开/关闭“GI 阴影效果”（模拟）
    /// </summary>
    private void ApplyGIShadow(bool enable)
    {
        if (enable)
        {
            // ========== GI 阴影：开启（高画质） ==========

            // 恢复全局阴影
            QualitySettings.shadows = defaultShadowQuality;

            // 恢复环境光强度
            RenderSettings.ambientIntensity = defaultAmbientIntensity;

            // 恢复每个光的阴影
            for (int i = 0; i < targetLights.Length; i++)
            {
                Light light = targetLights[i];
                if (light == null) continue;

                light.shadows = defaultLightShadows[i];
            }
        }
        else
        {
            // ========== GI 阴影：关闭（模拟、低画质） ==========

            // 1）关闭全局阴影
            QualitySettings.shadows = ShadowQuality.Disable;

            // 2）关闭所有控制光源的阴影
            for (int i = 0; i < targetLights.Length; i++)
            {
                Light light = targetLights[i];
                if (light == null) continue;

                light.shadows = LightShadows.None;
            }

            // 3）稍微提高环境光，让场景不至于太暗
            //    如果你想“更像关掉 GI”，可以把系数改小一点，例如 0.7f、0.5f
            RenderSettings.ambientIntensity = defaultAmbientIntensity * 1.2f;
        }
    }
}
