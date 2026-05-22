using UnityEngine;

public sealed class PlayerProfileManager : MonoBehaviour
{
    public static PlayerProfileManager Instance { get; private set; }

    private const string PlayerNameKey = "PLAYER_NAME";

    [Header("Default Names")]
    [SerializeField]
    private string[] defaultNames =
    {
        "SilentEcho27",
        "NovaGhost12",
        "CrimsonWolf84",
        "ShadowByte31",
        "PhantomPulse77",
        "ZeroHunter19",
        "IronViper62",
        "NightSignal44",
        "StaticRogue28",
        "FrostCipher93"
    };

    public string PlayerName { get; private set; }

    #region Unity Lifecycle

    private void Awake()
    {
        InitializeSingleton();
        LoadOrGeneratePlayerName();
    }

    #endregion

    #region Initialization

    private void InitializeSingleton()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    private void LoadOrGeneratePlayerName()
    {
        if (PlayerPrefs.HasKey(PlayerNameKey))
        {
            PlayerName = PlayerPrefs.GetString(PlayerNameKey);
            return;
        }

        GenerateRandomName();
    }

    #endregion

    #region Public API

    public void SavePlayerName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            Debug.LogWarning("Attempted to save an invalid player name.");
            return;
        }

        PlayerName = newName.Trim();

        PlayerPrefs.SetString(PlayerNameKey, PlayerName);
        PlayerPrefs.Save();
    }

    #endregion

    #region Private Methods

    private void GenerateRandomName()
    {
        if (defaultNames == null || defaultNames.Length == 0)
        {
            PlayerName = "UnknownWizard";
            return;
        }

        int randomIndex = Random.Range(0, defaultNames.Length);

        PlayerName = defaultNames[randomIndex];

        SavePlayerName(PlayerName);
    }

    #endregion
}