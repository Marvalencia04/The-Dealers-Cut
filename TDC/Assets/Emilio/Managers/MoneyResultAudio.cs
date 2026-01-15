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
        if (moneyManager == null)
            moneyManager = MoneyManager.Instance;
    }

    private void OnEnable()
    {
        moneyManager.OnRoundMoneyResult += HandleRoundResult;
    }

    private void OnDisable()
    {
        moneyManager.OnRoundMoneyResult -= HandleRoundResult;
    }

    private void HandleRoundResult(int netResult)
    {
        if (netResult > 0)
            audioSource.PlayOneShot(winClip);
        else if (netResult < 0)
            audioSource.PlayOneShot(loseClip);
    }
}

