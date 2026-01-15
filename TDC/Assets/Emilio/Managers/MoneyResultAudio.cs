using UnityEngine;

public class MoneyResultAudio : MonoBehaviour
{
    [SerializeField] private MoneyManager moneyManager;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip winClip;
    [SerializeField] private AudioClip loseClip;

    private void Awake()
    {
        Debug.Log("MoneyResultAudio AWAKE - SI VES ESTO, EL SCRIPT ESTA VIVO");
        // Intento pillar el MoneyManager automáticamente
        if (moneyManager == null)
            moneyManager = MoneyManager.Instance;

        // Comprobaciones útiles
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        if (moneyManager == null)
        {
            Debug.LogError("MoneyResultAudio: MoneyManager es NULL. ¿Está en la escena y se inicializa antes?");
            return;
        }

        moneyManager.OnRoundMoneyResult += HandleRoundResult;
        Debug.Log("MoneyResultAudio: Suscrito a OnRoundMoneyResult OK");
    }

    private void OnDisable()
    {
        if (moneyManager != null)
            moneyManager.OnRoundMoneyResult -= HandleRoundResult;
    }

    private void HandleRoundResult(int netResult)
    {
        Debug.Log($"MoneyResultAudio: HandleRoundResult llamado. netResult = {netResult}");

        if (audioSource == null)
        {
            Debug.LogError("MoneyResultAudio: AudioSource es NULL");
            return;
        }

        if (netResult > 0)
        {
            Debug.Log("Ganar Dinero Sonido");
            if (winClip != null) audioSource.PlayOneShot(winClip);
            else Debug.LogError("MoneyResultAudio: winClip es NULL");
        }
        else if (netResult < 0)
        {
            Debug.Log("Perder Dinero Sonido");
            if (loseClip != null) audioSource.PlayOneShot(loseClip);
            else Debug.LogError("MoneyResultAudio: loseClip es NULL");
        }
        else
        {
            Debug.Log("MoneyResultAudio: netResult es 0, no suena nada (ni ganar ni perder).");
        }
    }
}
