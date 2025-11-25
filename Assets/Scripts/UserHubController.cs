using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UserHubController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject userHubPanel;
    [SerializeField] private GameObject statsPanel;
    [SerializeField] private GameObject triviaPanel;

    [Header("UI Text")]
    [SerializeField] private TMP_Text welcomeUserText;

    [Header("References")]
    [SerializeField] private GameController gameController;
    [SerializeField] private StatsPanelController statsPanelController;

    private void OnEnable()
    {
        RefreshWelcome();
    }

    public void RefreshWelcome()
    {
        string nameToShow = string.IsNullOrEmpty(AuthManager.Username)
            ? "Player"
            : AuthManager.Username;

        if (welcomeUserText != null)
        {
            welcomeUserText.text = "Welcome, " + nameToShow + "!";
            Debug.Log("[Hub] Welcome text set to: " + welcomeUserText.text);
        }
        else
        {
            Debug.LogWarning("[Hub] welcomeUserText is NOT assigned!");
        }
    }

    public void OnPlayClicked()
    {
        if (userHubPanel) userHubPanel.SetActive(false);

        if (gameController != null)
            gameController.showTrivia();
        else if (triviaPanel)
            triviaPanel.SetActive(true);
    }

    public void OnStatsClicked()
{
    if (userHubPanel) userHubPanel.SetActive(false);
    if (statsPanel)   statsPanel.SetActive(true);

    if (statsPanelController != null)
    {
        statsPanelController.RefreshStats();
    }
}

    public void OnLogoutClicked()
    {
        AuthManager.Username    = null;
        AuthManager.AccessToken = null;

        Scene scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex);
    }

    public void ShowHubFromStats()
    {
        if (statsPanel)   statsPanel.SetActive(false);
        if (userHubPanel) userHubPanel.SetActive(true);
        RefreshWelcome();
    }
}
