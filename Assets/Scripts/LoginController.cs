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
public class ErrorResponse
{
    public string message;
}

[System.Serializable]
public class LoginSuccessResponse
{
    public string username;
}

public class LoginController : MonoBehaviour
{
    [Header("API")]
    [SerializeField] private string baseUrl = "http://localhost:5165"; 
    private string RegisterUrl => baseUrl.TrimEnd('/') + "/auth/register";
    private string LoginUrl    => baseUrl.TrimEnd('/') + "/auth/login";

    [Header("Panels")]
    [SerializeField] private GameObject welcomePanel;    
    [SerializeField] private GameObject loginScreenPanel;  
    [SerializeField] private GameObject registrationPanel;   
    [SerializeField] private GameObject userHubPanel;        

    [Header("Login Inputs")]
    [SerializeField] private TMP_InputField loginUsernameInput;
    [SerializeField] private TMP_InputField loginPasswordInput;

    [Header("Registration Inputs")]
    [SerializeField] private TMP_InputField registrationUsernameInput;
    [SerializeField] private TMP_InputField registrationPasswordInput;

    [Header("Status Texts")]
    [SerializeField] private TMP_Text loginStatusText;
    [SerializeField] private TMP_Text registrationStatusText;

[Header("Hub")]
[SerializeField] private UserHubController userHubController;
    private void Start()
    {
        ShowWelcomeOnly();
    }

    private void ShowWelcomeOnly()
    {
        if (welcomePanel)        welcomePanel.SetActive(true);
        if (loginScreenPanel)    loginScreenPanel.SetActive(false);
        if (registrationPanel)   registrationPanel.SetActive(false);
        if (userHubPanel)        userHubPanel.SetActive(false);

        if (loginStatusText)         loginStatusText.text = "";
        if (registrationStatusText)  registrationStatusText.text = "";

        if (loginUsernameInput)        loginUsernameInput.text = "";
        if (loginPasswordInput)        loginPasswordInput.text = "";
        if (registrationUsernameInput) registrationUsernameInput.text = "";
        if (registrationPasswordInput) registrationPasswordInput.text = "";
    }

    public void OnShowLoginClicked()
    {
        Debug.Log("[LoginController] Show Login clicked");

        if (welcomePanel)        welcomePanel.SetActive(false);
        if (loginScreenPanel)    loginScreenPanel.SetActive(true);
        if (registrationPanel)   registrationPanel.SetActive(false);
        if (userHubPanel)        userHubPanel.SetActive(false);

        if (loginStatusText)     loginStatusText.text = "";
    }

    public void OnShowRegisterClicked()
    {
        Debug.Log("[LoginController] Show Register clicked");

        if (welcomePanel)        welcomePanel.SetActive(false);
        if (loginScreenPanel)    loginScreenPanel.SetActive(false);
        if (registrationPanel)   registrationPanel.SetActive(true);
        if (userHubPanel)        userHubPanel.SetActive(false);

        if (registrationStatusText) registrationStatusText.text = "";
    }

    public void OnBackToWelcomeFromLogin()
    {
        Debug.Log("[LoginController] Back from Login");
        ShowWelcomeOnly();
    }

    public void OnBackToWelcomeFromRegister()
    {
        Debug.Log("[LoginController] Back from Register");
        ShowWelcomeOnly();
    }

    public void OnLoginButtonClicked()
    {
        Debug.Log("[LoginController] Login button pressed");

        string username = loginUsernameInput ? loginUsernameInput.text.Trim() : "";
        string password = loginPasswordInput ? loginPasswordInput.text : "";

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
        {
            if (loginStatusText) loginStatusText.text = "Please enter username and password.";
            return;
        }

        StartCoroutine(LoginRoutine(username, password));
    }

    public void OnRegisterButtonClicked()
    {
        Debug.Log("[LoginController] Register button pressed");

        string username = registrationUsernameInput ? registrationUsernameInput.text.Trim() : "";
        string password = registrationPasswordInput ? registrationPasswordInput.text : "";

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
        {
            if (registrationStatusText) registrationStatusText.text = "Please enter username and password.";
            return;
        }

        StartCoroutine(RegisterRoutine(username, password));
    }

    // register

    private IEnumerator RegisterRoutine(string username, string password)
    {
        if (registrationStatusText) registrationStatusText.text = "Registering...";

        var dto = new RegisterRequest { username = username, password = password };
        string json = JsonUtility.ToJson(dto);
        string url  = RegisterUrl;

        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
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

            string body = req.downloadHandler != null ? req.downloadHandler.text : "";

            if (bad)
            {
                Debug.LogError($"[Register] {req.responseCode} {req.error} | {body}");

                ErrorResponse err = null;
                try { err = JsonUtility.FromJson<ErrorResponse>(body); } catch { }

                if (registrationStatusText)
                    registrationStatusText.text = err != null && !string.IsNullOrEmpty(err.message)
                        ? err.message
                        : "Registration failed.";
            }
            else
            {
                Debug.Log("[Register] success: " + body);

                if (registrationStatusText)
                    registrationStatusText.text = "User registered. You can now log in.";
            }
        }
    }

    // login

    private IEnumerator LoginRoutine(string username, string password)
    {
        if (loginStatusText) loginStatusText.text = "Logging in...";

        var dto = new LoginRequest { username = username, password = password };
        string json = JsonUtility.ToJson(dto);
        string url  = LoginUrl;

        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
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

            string body = req.downloadHandler != null ? req.downloadHandler.text : "";

            if (bad)
            {
                Debug.LogError($"[Login] {req.responseCode} {req.error} | {body}");

                ErrorResponse err = null;
                try { err = JsonUtility.FromJson<ErrorResponse>(body); } catch { }

                if (loginStatusText)
                    loginStatusText.text = err != null && !string.IsNullOrEmpty(err.message)
                        ? err.message
                        : "Login failed.";
            }
            else
            {
                Debug.Log("[Login] success: " + body);

                var resp = JsonUtility.FromJson<LoginSuccessResponse>(body);
                if (resp == null || string.IsNullOrEmpty(resp.username))
                {
                    if (loginStatusText) loginStatusText.text = "Invalid server response.";
                    yield break;
                }

                AuthManager.Username = resp.username;
                Debug.Log("[Login] AuthManager.Username set to: " + AuthManager.Username);

                if (userHubController != null)
                {
                  userHubController.RefreshWelcome();
                }


                if (loginStatusText) loginStatusText.text = "";
                if (loginUsernameInput) loginUsernameInput.text = "";
                if (loginPasswordInput) loginPasswordInput.text = "";

                if (welcomePanel)      welcomePanel.SetActive(false);
                if (loginScreenPanel)  loginScreenPanel.SetActive(false);
                if (registrationPanel) registrationPanel.SetActive(false);
                if (userHubPanel)      userHubPanel.SetActive(true);
            }
        }
    }
}
