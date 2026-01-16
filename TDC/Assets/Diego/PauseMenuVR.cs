using UnityEngine;
using UnityEngine.XR;

public class PauseMenuVR : MonoBehaviour
{
    [Header("Panels (Same Scene)")]
    [SerializeField] private GameObject pauseMenuPanel; // 你的 PauseMenu 面板
    [SerializeField] private GameObject mainMenuPanel;  // 主菜单面板
    [SerializeField] private GameObject configPanel;    // 配置面板（可选）

    [Header("XR Pause Button")]
    [Tooltip("通常“下面那个按钮”= Secondary。若无效，改成 Primary 或 Menu。")]
    [SerializeField] private ButtonType pauseButton = ButtonType.Secondary;
    [SerializeField] private bool listenRightHand = true;
    [SerializeField] private bool listenLeftHand = true;

    public enum ButtonType { Primary, Secondary, Menu }

    private InputDevice leftDevice;
    private InputDevice rightDevice;
    private bool lastPressed;

    private void Awake()
    {
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        AcquireDevices();
    }

    private void OnEnable()
    {
        InputDevices.deviceConnected += OnDeviceChanged;
        InputDevices.deviceDisconnected += OnDeviceChanged;
        AcquireDevices();
    }

    private void OnDisable()
    {
        InputDevices.deviceConnected -= OnDeviceChanged;
        InputDevices.deviceDisconnected -= OnDeviceChanged;
    }

    private void OnDeviceChanged(InputDevice _) => AcquireDevices();

    private void AcquireDevices()
    {
        leftDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        rightDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;

        if (listenLeftHand && !leftDevice.isValid)
            leftDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);

        if (listenRightHand && !rightDevice.isValid)
            rightDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

        bool pressed = IsPausePressed();

        // Rising edge：按下瞬间触发一次
        if (pressed && !lastPressed)
        {
            GameManager.Instance.TogglePause();
            SyncPanel();
        }

        lastPressed = pressed;

        // 状态变化同步面板
        SyncPanel();
    }

    private bool IsPausePressed()
    {
        bool pressed = false;
        if (listenLeftHand) pressed |= GetButton(leftDevice);
        if (listenRightHand) pressed |= GetButton(rightDevice);
        return pressed;
    }

    private bool GetButton(InputDevice device)
    {
        if (!device.isValid) return false;

        InputFeatureUsage<bool> usage = CommonUsages.secondaryButton;
        switch (pauseButton)
        {
            case ButtonType.Primary: usage = CommonUsages.primaryButton; break;
            case ButtonType.Secondary: usage = CommonUsages.secondaryButton; break;
            case ButtonType.Menu: usage = CommonUsages.menuButton; break;
        }

        bool value;
        return device.TryGetFeatureValue(usage, out value) && value;
    }

    private void SyncPanel()
    {
        if (pauseMenuPanel == null) return;

        bool shouldShow = (GameManager.Instance.CurrentState == GameState.Pause);
        if (pauseMenuPanel.activeSelf != shouldShow)
            pauseMenuPanel.SetActive(shouldShow);
    }

    // ======================
    // UI Buttons (OnClick)
    // ======================

    // 继续游戏
    public void Resume()
    {
        if (GameManager.Instance == null) return;

        if (GameManager.Instance.CurrentState == GameState.Pause)
            GameManager.Instance.TogglePause();

        SyncPanel();
    }

    // 返回主菜单（之后点击 Jugar 会重新开始一局）
    public void BackToMainMenu()
    {
        if (GameManager.Instance == null) return;

        // 先退出暂停，保证 timeScale 回到 1
        if (GameManager.Instance.CurrentState == GameState.Pause)
            GameManager.Instance.TogglePause();

        // 切回主菜单状态
        GameManager.Instance.SetGameState(GameState.MainMenu);

        // UI：显示主菜单，关闭暂停/设置
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (configPanel != null) configPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }
}
