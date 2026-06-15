using UnityEngine;

/// <summary>
/// 全局音效/音乐管理器。
/// 提供 BGM 循环播放以及 SFX 单次播放接口，供 TurnStateMachine 等调用。
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip bgmClip;
    [SerializeField] private AudioClip moveSfxClip;
    [SerializeField] private AudioClip diceRollSfxClip;
    [SerializeField] private AudioClip coinGainSfxClip;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        PlayBGM();
    }

    // ==================== BGM ====================

    /// <summary>
    /// 开始循环播放背景音乐。
    /// </summary>
    public void PlayBGM()
    {
        if (bgmSource == null || bgmClip == null)
        {
            return;
        }

        bgmSource.clip = bgmClip;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    /// <summary>
    /// 停止背景音乐。
    /// </summary>
    public void StopBGM()
    {
        if (bgmSource != null)
        {
            bgmSource.Stop();
        }
    }

    // ==================== SFX ====================

    /// <summary>
    /// 播放移动音效。
    /// </summary>
    public void PlayMoveSfx()
    {
        PlayOneShot(moveSfxClip);
    }

    /// <summary>
    /// 播放掷骰子音效。
    /// </summary>
    public void PlayDiceRollSfx()
    {
        PlayOneShot(diceRollSfxClip);
    }

    /// <summary>
    /// 播放获得金币音效。
    /// </summary>
    public void PlayCoinGainSfx()
    {
        PlayOneShot(coinGainSfxClip);
    }

    // ==================== Internal ====================

    private void PlayOneShot(AudioClip clip)
    {
        if (sfxSource == null || clip == null)
        {
            return;
        }

        sfxSource.PlayOneShot(clip);
    }
}