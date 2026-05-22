using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerProfileUI : MonoBehaviour
{
    [Header("Profile UI")]
    [Space(5)]
    [SerializeField] private TMP_Text playerNameText;

    [SerializeField] private TMP_InputField playerNameInputField;

    [SerializeField] private Button editButton;

    [SerializeField] private Button saveButton;

    private void Start()
    {
        print("Script working");
        LoadProfileUI();

        if (editButton != null)
            editButton.onClick.AddListener(BeginEditingName);

        if (saveButton != null)
            saveButton.onClick.AddListener(SavePlayerName);
    }

    private void LoadProfileUI()
    {
        if (PlayerProfileManager.Instance == null)
            return;

        string currentName =
            PlayerProfileManager.Instance.PlayerName;

        playerNameText.text = currentName;

        playerNameInputField.text = currentName;

        playerNameText.gameObject.SetActive(true);

        playerNameInputField.gameObject.SetActive(false);

        saveButton.gameObject.SetActive(false);
    }

    public void BeginEditingName()
    {
        if (PlayerProfileManager.Instance == null)
            return;

        playerNameText.gameObject.SetActive(false);

        playerNameInputField.gameObject.SetActive(true);

        saveButton.gameObject.SetActive(true);

        playerNameInputField.text =
            PlayerProfileManager.Instance.PlayerName;

        playerNameInputField.ActivateInputField();

        playerNameInputField.Select();
    }

    public void SavePlayerName()
    {
        if (PlayerProfileManager.Instance == null)
            return;

        string enteredName =
            playerNameInputField.text.Trim();

        if (string.IsNullOrWhiteSpace(enteredName))
            return;

        PlayerProfileManager.Instance
            .SavePlayerName(enteredName);

        playerNameText.text =
            PlayerProfileManager.Instance.PlayerName;

        playerNameText.gameObject.SetActive(true);

        playerNameInputField.gameObject.SetActive(false);

        saveButton.gameObject.SetActive(false);
    }
}