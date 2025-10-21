using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;

public class GameController : MonoBehaviour
{
    [Header("API")]
    [SerializeField] private string triviaBaseUrl    = "http://localhost:5165";
    [SerializeField] private string triviaNextPath   = "/trivia/next";
    [SerializeField] private string triviaAnswerPath = "/trivia/answer";
    [SerializeField] private string triviaEndPath    = "/trivia/end";

    [Header("Trivia UI")]
    [SerializeField] private GameObject triviaPanel;
    [SerializeField] private TMP_Text   triviaQuestion;
    [SerializeField] private TMP_Text   scoreTracker;
    [SerializeField] private TMP_Text   statusText;
    [SerializeField] private Button     ans1;
    [SerializeField] private Button     ans2;
    [SerializeField] private Button     ans3;
    [SerializeField] private Button     ans4;
    [SerializeField] private Button     nextQuesBtn;
    [SerializeField] private Button     endGameBtn;

    [Header("Welcome UI (optional)")]
    [SerializeField] private GameObject welcomePanel;
    [SerializeField] private Button     playBtn;

    private int      currentQuestionId = -1;
    private string[] currentChoices    = Array.Empty<string>();
    private int      currentScore      = 0;

    private void Awake()
    {
        if (ans1) ans1.onClick.AddListener(() => onAnswer(0));
        if (ans2) ans2.onClick.AddListener(() => onAnswer(1));
        if (ans3) ans3.onClick.AddListener(() => onAnswer(2));
        if (ans4) ans4.onClick.AddListener(() => onAnswer(3));
        if (nextQuesBtn) nextQuesBtn.onClick.AddListener(onNextQuestion);
        if (endGameBtn)  endGameBtn.onClick.AddListener(onExitToWelcome);
        if (playBtn)     playBtn.onClick.AddListener(showTrivia);
    }

    private void Start() => showWelcome();

    private void showWelcome()
    {
        if (welcomePanel) welcomePanel.SetActive(true);
        if (triviaPanel)  triviaPanel.SetActive(false);
        statusText?.SetText("");
        scoreTracker?.SetText("Score: 0");
        currentScore = 0;
        currentQuestionId = -1;
        currentChoices = Array.Empty<string>();
    }

    private void showTrivia()
    {
        if (welcomePanel) welcomePanel.SetActive(false);
        if (triviaPanel)  triviaPanel.SetActive(true);
        statusText?.SetText("");
        scoreTracker?.SetText($"Score: {currentScore}");
        StartCoroutine(fetchNextQuestion());
    }

    private void onNextQuestion()
    {
        setButtonText(nextQuesBtn, "Loading...");
        nextQuesBtn.interactable = false;
        StartCoroutine(fetchNextQuestion());
    }

    private IEnumerator fetchNextQuestion()
    {
        if (statusText) statusText.text = "Loading question...";

        string url = combineUrl(triviaBaseUrl, triviaNextPath);
        using (var req = UnityWebRequest.Get(url))
        {
            req.timeout = 10;
            req.downloadHandler = new DownloadHandlerBuffer();
            Debug.Log($"[Trivia] GET {url}");
            yield return req.SendWebRequest();

#if UNITY_2020_3_OR_NEWER
            bool bad = req.result == UnityWebRequest.Result.ConnectionError || req.result == UnityWebRequest.Result.ProtocolError;
#else
            bool bad = req.isNetworkError || req.isHttpError;
#endif
            Debug.Log($"[Trivia] result={req.result} code={req.responseCode} err={req.error}");
            if (bad)
            {
                if (statusText) statusText.text = $"Failed to load: {req.responseCode}\n{req.error}\n{req.downloadHandler.text}";
                setButtonText(nextQuesBtn, "Next");
                nextQuesBtn.interactable = true;
                yield break;
            }

            var res = JsonUtility.FromJson<QuestionRes>(req.downloadHandler.text);
            if (res == null)
            {
                statusText?.SetText("No question received.");
                setButtonText(nextQuesBtn, "Next");
                nextQuesBtn.interactable = true;
                yield break;
            }

            currentQuestionId = res.questionId;
            currentChoices    = res.choices ?? Array.Empty<string>();

            triviaQuestion?.SetText(res.question ?? "No question");
            statusText?.SetText("");

            if (ans1) setButtonText(ans1, currentChoices.Length > 0 ? currentChoices[0] : "—");
            if (ans2) setButtonText(ans2, currentChoices.Length > 1 ? currentChoices[1] : "—");
            if (ans3) setButtonText(ans3, currentChoices.Length > 2 ? currentChoices[2] : "—");
            if (ans4) setButtonText(ans4, currentChoices.Length > 3 ? currentChoices[3] : "—");

            setButtonText(nextQuesBtn, "Next");
            nextQuesBtn.interactable = true;
        }
    }

    private void onAnswer(int choiceIndex)
    {
        if (currentQuestionId < 0 || currentChoices == null || choiceIndex < 0 || choiceIndex >= currentChoices.Length)
        {
            statusText?.SetText("No active question.");
            return;
        }
        StartCoroutine(submitAnswerRoutine(currentQuestionId, choiceIndex));
    }

    private IEnumerator submitAnswerRoutine(int questionId, int choiceIndex)
    {
        string url = combineUrl(triviaBaseUrl, triviaAnswerPath);
        Debug.Log($"[Trivia] POST {url}");

        var payload = JsonUtility.ToJson(new AnswerReq { questionId = questionId, choiceIndex = choiceIndex });

        using (var req = new UnityWebRequest(url, "POST"))
        {
            req.timeout = 10;
            req.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
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
                statusText?.SetText($"Submit failed: {req.responseCode}\n{req.error}\n{req.downloadHandler.text}");
                yield break;
            }

            var res = JsonUtility.FromJson<AnswerRes>(req.downloadHandler.text);
            if (res == null)
            {
                statusText?.SetText("No response from server.");
                yield break;
            }

            if (res.correct)
            {
                currentScore++;
                scoreTracker?.SetText($"Score: {currentScore}");
                statusText?.SetText("Correct!");
            }
            else
            {
                statusText?.SetText($"Incorrect. Correct answer: {res.correctAnswer}");
            }

            nextQuesBtn.interactable = true;
        }
    }

    private void onExitToWelcome()
    {
        StartCoroutine(endGameRoutine());
        showWelcome();
    }

    private IEnumerator endGameRoutine()
    {
        string url = combineUrl(triviaBaseUrl, triviaEndPath);
        using (var req = new UnityWebRequest(url, "POST"))
        {
            req.timeout = 10;
            req.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes("{}"));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();
        }
    }

    private static void setButtonText(Button btn, string text)
    {
        if (!btn) return;
        var label = btn.GetComponentInChildren<TMP_Text>();
        if (label) label.text = text ?? "";
    }

    private static string combineUrl(string root, string path)
    {
        if (string.IsNullOrEmpty(root)) return path ?? string.Empty;
        if (string.IsNullOrEmpty(path)) return root;
        if (root.EndsWith("/")) root = root.TrimEnd('/');
        return path.StartsWith("/") ? root + path : root + "/" + path;
    }

    // DTOs
    [Serializable] private class QuestionRes
    {
        public int      questionId;
        public string   question;
        public string[] choices;
    }

    [Serializable] private class AnswerReq
    {
        public int questionId;
        public int choiceIndex;
    }

    [Serializable] private class AnswerRes
    {
        public bool   correct;
        public int    correctIndex;
        public string correctAnswer;
    }
}
