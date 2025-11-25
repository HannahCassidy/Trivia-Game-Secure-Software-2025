using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    [Header("Trivia API")]
    [SerializeField] private string triviaBaseUrl    = "http://localhost:5165";
    [SerializeField] private string triviaNextPath   = "/trivia/next";
    [SerializeField] private string triviaAnswerPath = "/trivia/answer";
    [SerializeField] private string triviaEndPath    = "/trivia/end";

    [Header("Stats API")]
    [SerializeField] private string statsSubmitPath  = "/stats/submit"; 

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

    [Header("Welcome / Hub (optional)")]
    [SerializeField] private GameObject welcomePanel;  
    [SerializeField] private Button     playBtn;        

    private int      currentScore      = 0;
    private int      currentQuestionId = -1;
    private bool     awaitingAnswer    = false;
    private string[] currentChoices    = Array.Empty<string>();

    private int totalQuestionsThisGame = 0;
    private int correctAnswersThisGame = 0;

    private void Awake()
    {
        if (ans1) ans1.onClick.AddListener(() => OnAnswerClicked(0));
        if (ans2) ans2.onClick.AddListener(() => OnAnswerClicked(1));
        if (ans3) ans3.onClick.AddListener(() => OnAnswerClicked(2));
        if (ans4) ans4.onClick.AddListener(() => OnAnswerClicked(3));

        if (nextQuesBtn) nextQuesBtn.onClick.AddListener(OnNextQuestionClicked);
        if (endGameBtn)  endGameBtn.onClick.AddListener(OnEndGameClicked);
        if (playBtn)     playBtn.onClick.AddListener(showTrivia);
    }

    private void Start()
    {
        if (triviaPanel) triviaPanel.SetActive(false);
        if (scoreTracker) scoreTracker.text = "Score: 0";
        if (statusText)   statusText.text   = "";
    }

    public void showTrivia()
    {
        if (welcomePanel) welcomePanel.SetActive(false);
        if (triviaPanel)  triviaPanel.SetActive(true);

        currentScore            = 0;
        currentQuestionId       = -1;
        awaitingAnswer          = false;
        totalQuestionsThisGame  = 0;
        correctAnswersThisGame  = 0;

        if (scoreTracker) scoreTracker.text = "Score: 0";
        if (statusText)   statusText.text   = "Loading question...";
        if (triviaQuestion) triviaQuestion.text = "";

        setButtonText(nextQuesBtn, "Next");
        setButtonsInteractable(false);

        StartCoroutine(FetchNextQuestionRoutine());
    }

    private void OnNextQuestionClicked()
    {
        if (statusText) statusText.text = "Loading question...";
        setButtonsInteractable(false);
        setButtonText(nextQuesBtn, "Loading...");
        nextQuesBtn.interactable = false;

        StartCoroutine(FetchNextQuestionRoutine());
    }

    private void OnEndGameClicked()
    {
        StartCoroutine(EndGameAndExitRoutine());
    }

    private void OnAnswerClicked(int choiceIndex)
    {
        if (!awaitingAnswer)
        {
            if (statusText) statusText.text = "Press Next to get a question first.";
            return;
        }

        awaitingAnswer = false;
        setButtonsInteractable(false);

        totalQuestionsThisGame++;

        StartCoroutine(SendAnswerRoutine(choiceIndex));
    }

    private IEnumerator FetchNextQuestionRoutine()
    {
        string url = combineUrl(triviaBaseUrl, triviaNextPath);

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();

#if UNITY_2020_3_OR_NEWER
            bool bad = req.result == UnityWebRequest.Result.ConnectionError ||
                       req.result == UnityWebRequest.Result.ProtocolError;
#else
            bool bad = req.isNetworkError || req.isHttpError;
#endif

            if (bad)
            {
                if (statusText)
                    statusText.text = $"Failed to load: {req.responseCode}\n{req.error}\n{req.downloadHandler.text}";
                setButtonText(nextQuesBtn, "Next");
                nextQuesBtn.interactable = true;
                yield break;
            }

            var res = JsonUtility.FromJson<QuestionRes>(req.downloadHandler.text);
            if (res == null)
            {
                if (statusText) statusText.text = "Bad response from server.";
                setButtonText(nextQuesBtn, "Next");
                nextQuesBtn.interactable = true;
                yield break;
            }

            currentQuestionId = res.questionId;
            currentChoices    = res.choices ?? Array.Empty<string>();

            if (triviaQuestion) triviaQuestion.text = res.question ?? "";

            if (ans1) setButtonText(ans1, currentChoices.Length > 0 ? currentChoices[0] : "—");
            if (ans2) setButtonText(ans2, currentChoices.Length > 1 ? currentChoices[1] : "—");
            if (ans3) setButtonText(ans3, currentChoices.Length > 2 ? currentChoices[2] : "—");
            if (ans4) setButtonText(ans4, currentChoices.Length > 3 ? currentChoices[3] : "—");

            if (statusText) statusText.text = "Choose an answer.";
            awaitingAnswer = true;

            setButtonsInteractable(true);
            setButtonText(nextQuesBtn, "Next");
            nextQuesBtn.interactable = true;
        }
    }

    private IEnumerator SendAnswerRoutine(int choiceIndex)
    {
        string url = combineUrl(triviaBaseUrl, triviaAnswerPath);

        var dto = new AnswerReq
        {
            questionId  = currentQuestionId,
            choiceIndex = choiceIndex
        };

        string json = JsonUtility.ToJson(dto);

        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
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

            if (bad)
            {
                if (statusText)
                    statusText.text = $"Answer failed: {req.responseCode}\n{req.error}\n{req.downloadHandler.text}";
                yield break;
            }

            var res = JsonUtility.FromJson<AnswerRes>(req.downloadHandler.text);
            if (res == null)
            {
                if (statusText) statusText.text = "Bad answer response.";
                yield break;
            }

            if (res.correct)
            {
                correctAnswersThisGame++; 
                currentScore++;
                if (statusText) statusText.text = "Correct!";
            }
            else
            {
                if (statusText)
                    statusText.text = $"Wrong! Correct answer: {res.correctAnswer}";
            }

            if (scoreTracker) scoreTracker.text = $"Score: {currentScore}";
        }
    }

    private IEnumerator EndGameAndExitRoutine()
    {
        yield return SendStatsRoutine();

        string url = combineUrl(triviaBaseUrl, triviaEndPath);

        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler   = new UploadHandlerRaw(new byte[0]);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();
        }

        Scene scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex);
    }

    private IEnumerator SendStatsRoutine()
{
    if (string.IsNullOrEmpty(AuthManager.Username))
        yield break; 

    string url = combineUrl(triviaBaseUrl, statsSubmitPath); 

    var dto = new GameResultDto
    {
        Username       = AuthManager.Username,
        TotalQuestions = totalQuestionsThisGame,   
        CorrectAnswers = correctAnswersThisGame
    };

    Debug.Log($"[Stats/submit] Sending stats for {dto.Username}: " +
              $"{dto.CorrectAnswers}/{dto.TotalQuestions}");

    string json = JsonUtility.ToJson(dto);

    using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
    {
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

        if (bad)
        {
            Debug.LogError($"[Stats/submit] {req.responseCode} {req.error} | {req.downloadHandler.text}");
        }
        else
        {
            Debug.Log("[Stats/submit] stats saved for " + AuthManager.Username);
        }
    }
}

    private void setButtonsInteractable(bool value)
    {
        if (ans1) ans1.interactable = value;
        if (ans2) ans2.interactable = value;
        if (ans3) ans3.interactable = value;
        if (ans4) ans4.interactable = value;
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
        if (string.IsNullOrEmpty(path)) return root ?? string.Empty;

        if (root.EndsWith("/")) root = root.TrimEnd('/');
        return path.StartsWith("/") ? root + path : root + "/" + path;
    }

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

    [Serializable] private class GameResultDto
    {
        public string Username;
        public int    TotalQuestions;
        public int    CorrectAnswers;
    }
}
