using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;

public class GameController : MonoBehaviour
{
    // =========================
    // API CONFIG
    // =========================
    [Header("API")]
    [SerializeField] private string baseUrl = "http://localhost:5164"; // e.g., http://localhost:5164

    // =========================
    // PANELS
    // =========================
    [Header("Panels")]
    [SerializeField] private GameObject welcomePanel;
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject registerPanel;
    [SerializeField] private GameObject triviaPanel;

    // =========================
    // WELCOME UI
    // =========================
    [Header("Welcome UI")]
    [SerializeField] private Button welcomeLoginBtn;
    [SerializeField] private Button welcomeRegisterBtn;

    // =========================
    // LOGIN UI
    // =========================
    [Header("Login UI")]
    [SerializeField] private TMP_InputField loginUserOrEmailField; // username or email
    [SerializeField] private TMP_InputField loginPasswordField;
    [SerializeField] private Button loginSubmitBtn;
    [SerializeField] private Button loginBackBtn;
    [SerializeField] private TMP_Text loginStatusText;

    // =========================
    // REGISTER UI
    // =========================
    [Header("Register UI")]
    [SerializeField] private TMP_InputField regUsernameField;
    [SerializeField] private TMP_InputField regEmailField;
    [SerializeField] private TMP_InputField regPasswordField;
    [SerializeField] private Button registerSubmitBtn;
    [SerializeField] private Button registerBackBtn;
    [SerializeField] private TMP_Text registerStatusText;

    // =========================
    // TRIVIA UI
    // =========================
    [Header("Trivia UI")]
    [SerializeField] private TMP_Text triviaQuestion;
    [SerializeField] private TMP_Text scoreTracker;
    [SerializeField] private TMP_Text statusText;

    [SerializeField] private Button ans1;
    [SerializeField] private Button ans2;
    [SerializeField] private Button ans3;
    [SerializeField] private Button ans4;

    [SerializeField] private Button nextQuesBtn;
    [SerializeField] private Button endGameBtn;

    // =========================
    // STATE
    // =========================
    private string authToken = null;               // JWT returned by /auth/*
    private int currentQuestionId = -1;
    private string[] currentChoices = Array.Empty<string>();
    private int currentScore = 0;

    // =========================
    // LIFECYCLE
    // =========================
    private void Awake()
    {
        // Welcome
        if (welcomeLoginBtn) welcomeLoginBtn.onClick.AddListener(ShowLogin);
        if (welcomeRegisterBtn) welcomeRegisterBtn.onClick.AddListener(ShowRegister);

        // Login
        if (loginSubmitBtn) loginSubmitBtn.onClick.AddListener(OnLoginClicked);
        if (loginBackBtn) loginBackBtn.onClick.AddListener(ShowWelcome);

        // Register
        if (registerSubmitBtn) registerSubmitBtn.onClick.AddListener(OnRegisterClicked);
        if (registerBackBtn) registerBackBtn.onClick.AddListener(ShowWelcome);

        // Trivia actions
        if (ans1) ans1.onClick.AddListener(() => OnAnswer(0));
        if (ans2) ans2.onClick.AddListener(() => OnAnswer(1));
        if (ans3) ans3.onClick.AddListener(() => OnAnswer(2));
        if (ans4) ans4.onClick.AddListener(() => OnAnswer(3));

        if (nextQuesBtn) nextQuesBtn.onClick.AddListener(OnNextQuestion);
        if (endGameBtn) endGameBtn.onClick.AddListener(OnEndGame);
    }

    private void Start()
    {
        ShowWelcome();
    }

    // =========================
    // PANEL NAV
    // =========================
    private void ShowWelcome()
    {
        SetPanels(welcome: true, login: false, register: false, trivia: false);
        ClearLoginStatus();
        ClearRegisterStatus();
    }

    private void ShowLogin()
    {
        SetPanels(welcome: false, login: true, register: false, trivia: false);
        ClearLoginStatus();
        if (loginUserOrEmailField) loginUserOrEmailField.ActivateInputField();
    }

    private void ShowRegister()
    {
        SetPanels(welcome: false, login: false, register: true, trivia: false);
        ClearRegisterStatus();
        if (regUsernameField) regUsernameField.ActivateInputField();
    }

    private void ShowTrivia()
    {
        SetPanels(welcome: false, login: false, register: false, trivia: true);
        statusText?.SetText("");
        // Optionally auto-load first question:
        StartCoroutine(FetchNextQuestion());
    }

    private void SetPanels(bool welcome, bool login, bool register, bool trivia)
    {
        if (welcomePanel)  welcomePanel.SetActive(welcome);
        if (loginPanel)    loginPanel.SetActive(login);
        if (registerPanel) registerPanel.SetActive(register);
        if (triviaPanel)   triviaPanel.SetActive(trivia);
    }

    // =========================
    // AUTH FLOWS
    // =========================
    public void OnLoginClicked()
    {
        string uoe = loginUserOrEmailField ? loginUserOrEmailField.text.Trim() : "";
        string pw  = loginPasswordField ? loginPasswordField.text : "";

        if (string.IsNullOrEmpty(uoe) || string.IsNullOrEmpty(pw))
        {
            SetLoginStatus("Enter username/email and password.");
            return;
        }

        SetLoginStatus("Signing in...");
        StartCoroutine(LoginRoutine(uoe, pw));
    }

    public void OnRegisterClicked()
    {
        string u = regUsernameField ? regUsernameField.text.Trim() : "";
        string e = regEmailField ? regEmailField.text.Trim() : "";
        string p = regPasswordField ? regPasswordField.text : "";

        if (string.IsNullOrEmpty(u) || string.IsNullOrEmpty(p))
        {
            SetRegisterStatus("Username and password are required.");
            return;
        }

        SetRegisterStatus("Creating account...");
        StartCoroutine(RegisterRoutine(u, p, e));
    }

    private IEnumerator LoginRoutine(string usernameOrEmail, string password)
    {
        string url = $"{baseUrl}/auth/login";
        var payload = JsonUtility.ToJson(new LoginReq { usernameOrEmail = usernameOrEmail, password = password });

        using (var req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

#if UNITY_2020_3_OR_NEWER
            bool bad = req.result == UnityWebRequest.Result.ConnectionError || req.result == UnityWebRequest.Result.ProtocolError;
#else
            bool bad = req.isNetworkError || req.isHttpError;
#endif
            if (bad)
            {
                SetLoginStatus($"Login failed: {req.responseCode} {req.error}");
                yield break;
            }

            TokenRes res = null;
            try { res = JsonUtility.FromJson<TokenRes>(req.downloadHandler.text); }
            catch { SetLoginStatus("Login failed: bad response."); yield break; }

            if (res == null || string.IsNullOrEmpty(res.token))
            {
                SetLoginStatus("Login failed: no token.");
                yield break;
            }

            authToken = res.token;
            SetLoginStatus("Success!");
            ShowTrivia();
        }
    }

    private IEnumerator RegisterRoutine(string username, string password, string email)
    {
        string url = $"{baseUrl}/auth/register";
        var payload = JsonUtility.ToJson(new RegisterReq { username = username, password = password, email = email });

        using (var req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

#if UNITY_2020_3_OR_NEWER
            bool bad = req.result == UnityWebRequest.Result.ConnectionError || req.result == UnityWebRequest.Result.ProtocolError;
#else
            bool bad = req.isNetworkError || req.isHttpError;
#endif
            if (bad)
            {
                SetRegisterStatus($"Error: {req.responseCode} {req.error}");
                yield break;
            }

            TokenRes res = null;
            try { res = JsonUtility.FromJson<TokenRes>(req.downloadHandler.text); }
            catch { SetRegisterStatus("Bad response."); yield break; }

            if (res == null || string.IsNullOrEmpty(res.token))
            {
                // Your API returns a token on register; if not, prompt user to log in.
                SetRegisterStatus("Account created. Please log in.");
                ShowLogin();
                yield break;
            }

            // If your API returns a token on register, you can auto-login:
            authToken = res.token;
            SetRegisterStatus("Account created!");
            ShowTrivia();
        }
    }

    // =========================
    // TRIVIA FLOWS
    // =========================
    private void OnNextQuestion()
    {
        if (string.IsNullOrEmpty(authToken))
        {
            statusText?.SetText("Please log in first.");
            return;
        }
        StartCoroutine(FetchNextQuestion());
    }

    private IEnumerator FetchNextQuestion()
    {
        statusText?.SetText("Loading question...");
        string url = $"{baseUrl}/trivia/next"; // adjust if your route differs

        using (var req = UnityWebRequest.Get(url))
        {
            AddAuthHeader(req);
            req.downloadHandler = new DownloadHandlerBuffer();

            yield return req.SendWebRequest();

#if UNITY_2020_3_OR_NEWER
            bool bad = req.result == UnityWebRequest.Result.ConnectionError || req.result == UnityWebRequest.Result.ProtocolError;
#else
            bool bad = req.isNetworkError || req.isHttpError;
#endif
            if (bad)
            {
                statusText?.SetText($"Error: {req.responseCode} {req.error}");
                yield break;
            }

            QuestionRes res = null;
            try { res = JsonUtility.FromJson<QuestionRes>(req.downloadHandler.text); }
            catch { statusText?.SetText("Invalid question payload."); yield break; }

            if (res == null || res.questionId <= 0 || res.choices == null || res.choices.Length < 4)
            {
                statusText?.SetText("Invalid question data.");
                yield break;
            }

            currentQuestionId = res.questionId;
            currentChoices = res.choices ?? Array.Empty<string>();
            currentScore = res.score;

            triviaQuestion?.SetText(res.question);
            SetButtonText(ans1, res.choices[0]);
            SetButtonText(ans2, res.choices[1]);
            SetButtonText(ans3, res.choices[2]);
            SetButtonText(ans4, res.choices[3]);

            scoreTracker?.SetText($"Score: {currentScore}");
            statusText?.SetText("");
        }
    }

    private void OnAnswer(int choiceIndex)
    {
        if (string.IsNullOrEmpty(authToken) || currentQuestionId <= 0) return;
        if (choiceIndex < 0 || choiceIndex >= currentChoices.Length) return;

        StartCoroutine(SubmitAnswerRoutine(choiceIndex));
    }

    private IEnumerator SubmitAnswerRoutine(int choiceIndex)
    {
        string url = $"{baseUrl}/trivia/answer"; // adjust if your route differs
        var payload = JsonUtility.ToJson(new AnswerReq { questionId = currentQuestionId, choiceIndex = choiceIndex });

        using (var req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            AddAuthHeader(req);

            yield return req.SendWebRequest();

#if UNITY_2020_3_OR_NEWER
            bool bad = req.result == UnityWebRequest.Result.ConnectionError || req.result == UnityWebRequest.Result.ProtocolError;
#else
            bool bad = req.isNetworkError || req.isHttpError;
#endif
            if (bad)
            {
                statusText?.SetText($"Submit failed: {req.responseCode} {req.error}");
                yield break;
            }

            AnswerRes res = null;
            try { res = JsonUtility.FromJson<AnswerRes>(req.downloadHandler.text); }
            catch { statusText?.SetText("Bad answer response."); yield break; }

            if (res == null)
            {
                statusText?.SetText("Bad answer response.");
                yield break;
            }

            currentScore = res.score;
            scoreTracker?.SetText($"Score: {currentScore}");

            statusText?.SetText(res.correct
                ? "✅ Correct!"
                : (string.IsNullOrEmpty(res.correctAnswer) ? "Incorrect." : $"Incorrect. Answer: {res.correctAnswer}"));
        }
    }

    private void OnEndGame()
    {
        authToken = null;
        currentQuestionId = -1;
        currentChoices = Array.Empty<string>();
        currentScore = 0;

        triviaQuestion?.SetText("");
        scoreTracker?.SetText("Score: 0");
        statusText?.SetText("");

        ShowWelcome();
    }

    // =========================
    // HELPERS & DTOs
    // =========================
    private void AddAuthHeader(UnityWebRequest req)
    {
        if (!string.IsNullOrEmpty(authToken))
            req.SetRequestHeader("Authorization", $"Bearer {authToken}");
    }

    private void SetButtonText(Button b, string txt)
    {
        if (!b) return;
        var label = b.GetComponentInChildren<TMP_Text>();
        if (label) label.SetText(txt);
    }

    // ---- DTOs must be camelCase to match ASP.NET Core defaults ----
    [Serializable] private class RegisterReq { public string username; public string password; public string email; }
    [Serializable] private class LoginReq    { public string usernameOrEmail; public string password; }
    [Serializable] private class TokenRes    { public string token; }

    [Serializable] private class QuestionRes
    {
        public int questionId;
        public string question;
        public string[] choices; // expected length 4
        public int score;        // optional: include if API returns score with question
    }

    [Serializable] private class AnswerReq
    {
        public int questionId;
        public int choiceIndex;
    }

    [Serializable] private class AnswerRes
    {
        public bool correct;
        public string correctAnswer; // optional
        public int score;
    }
}
