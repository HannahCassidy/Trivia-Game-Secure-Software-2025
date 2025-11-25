using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

[System.Serializable]
public class RegisterRequest
{
    public string username;
    public string password;
}

[System.Serializable]
public class LoginRequest
{
    public string username;
    public string password;
}

[System.Serializable]
public class LoginSuccessResponse
{
    public string username;
}

[System.Serializable]
public class ErrorResponse
{
    public string message;
}

public class LoginController : MonoBehaviour
{
    [Header("API")]
    [SerializeField] private string baseUrl      = "http://localhost:5165";
    [SerializeField] private string registerPath = "/auth/register";
    [SerializeField] private string loginPath    = "/auth/login";

    [Header("Panels")]
    [SerializeField] private GameObject welcomePanel;
    [SerializeField] private GameObject loginScreenPanel;
    [SerializeField] private GameObject registrationPanel;

    [Header("Login UI")]
    [SerializeField] private TMP_InputField loginUsernameInput;
    [SerializeField] private TMP_InputField loginPasswordInput;
    [SerializeField] private Button         loginButton;

    [Header("New Account UI")]
    [SerializeField] private TMP_InputField newUsernameInput;
    [SerializeField] private TMP_InputField newPasswordInput;
    [SerializeField] private Button         registerButton;

    [Header("Status Texts")]
    [SerializeField] private TMP_Text loginStatusText;
    [SerializeField] private TMP_Text registrationStatusText;

    [Header("Game")]
    [SerializeField] private GameController gameController;

    private void Awake()
    {
        if (loginButton)    loginButton.onClick.AddListener(onLoginClicked);
        if (registerButton) registerButton.onClick.AddListener(onRegisterClicked);
    }

    private void Start()
    {
        showWelcomeOnly();
    }

    // ============ PANEL SWITCHING ============

    public void onShowLoginPanel()
    {
        welcomePanel.SetActive(true);
        loginScreenPanel.SetActive(true);
        registrationPanel.SetActive(false);

        loginStatusText.text = "";
        registrationStatusText.text = "";
    }

    public void onShowRegistrationPanel()
    {
        welcomePanel.SetActive(true);
        loginScreenPanel.SetActive(false);
        registrationPanel.SetActive(true);

        loginStatusText.text = "";
        registrationStatusText.text = "";
    }

    public void onBackToWelcome()
    {
        showWelcomeOnly();
    }

    private void showWelcomeOnly()
    {
        welcomePanel.SetActive(true);
        loginScreenPanel.SetActive(false);
        registrationPanel.SetActive(false);

        loginStatusText.text = "";
        registrationStatusText.text = "";
    }

    // ============ REGISTER ============

    private void onRegisterClicked()
    {
        string username = newUsernameInput.text.Trim();
        string password = newPasswordInput.text;

        StartCoroutine(registerRoutine(username, password));
    }

    private IEnumerator registerRoutine(string username, string password)
    {
        registrationStatusText.text = "Creating account...";

        var payload = new RegisterRequest { username = username, password = password };
        string json = JsonUtility.ToJson(payload);

        using (var req = new UnityWebRequest(baseUrl + registerPath, "POST"))
        {
            req.timeout         = 10;
            req.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

#if UNITY_2020_3_OR_NEWER
            bool bad = req.result == UnityWebRequest.Result.ConnectionError ||
                       req.result == UnityWebRequest.Result.ProtocolError;
#else
            bool bad = req.isNetworkError || req.isHttpError;
#endif
            string body = req.downloadHandler.text;

            if (bad)
            {
                ErrorResponse err = null;
                try { err = JsonUtility.FromJson<ErrorResponse>(body); } catch {}

                registrationStatusText.text = err != null ? err.message : "Registration failed.";
            }
            else
            {
                registrationStatusText.text = "Account created. You can now log in.";
                onShowLoginPanel(); // auto-switch to login
            }
        }
    }

    // ============ LOGIN ============

    private void onLoginClicked()
    {
        string username = loginUsernameInput.text.Trim();
        string password = loginPasswordInput.text;

        StartCoroutine(loginRoutine(username, password));
    }

    private IEnumerator loginRoutine(string username, string password)
    {
        loginStatusText.text = "Logging in...";

        var payload = new LoginRequest { username = username, password = password };
        string json = JsonUtility.ToJson(payload);

        using (var req = new UnityWebRequest(baseUrl + loginPath, "POST"))
        {
            req.timeout         = 10;
            req.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

#if UNITY_2020_3_OR_NEWER
            bool bad = req.result == UnityWebRequest.Result.ConnectionError ||
                       req.result == UnityWebRequest.Result.ProtocolError;
#else
            bool bad = req.isNetworkError || req.isHttpError;
#endif
            string body = req.downloadHandler.text;

            if (bad)
            {
                ErrorResponse err = null;
                try { err = JsonUtility.FromJson<ErrorResponse>(body); } catch {}

                if (err != null && err.message.ToLower().Contains("locked"))
                {
                    loginStatusText.text = err.message;
                }
                else
                {
                    loginStatusText.text = err != null ? err.message : "Login failed.";
                }
            }
            else
            {
                var resp = JsonUtility.FromJson<LoginSuccessResponse>(body);
                loginStatusText.text = $"Welcome, {resp.username}!";

                gameController.showTrivia();
            }
        }
    }
}
