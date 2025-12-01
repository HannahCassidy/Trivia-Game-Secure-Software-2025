using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class UserStatsDto
{
    public int   totalGamesPlayed;
    public int   totalQuestionsAnswered;
    public int   totalCorrectAnswers;
    public float accuracy;  
}

public class StatsPanelController : MonoBehaviour
{
    [Header("Text Fields")]
    [SerializeField] private TMP_Text totalGames;
    [SerializeField] private TMP_Text totalQuestions;
    [SerializeField] private TMP_Text totalCorrect;
    [SerializeField] private TMP_Text accuracyText;

    [Header("Panels")]
    [SerializeField] private GameObject statsPanel;
    [SerializeField] private UserHubController userHubController;

    [Header("API")]
    [SerializeField] private string baseUrl = "http://localhost:5165";

    private void OnEnable()
    {
        StartCoroutine(LoadStatsRoutine());
    }

    private IEnumerator LoadStatsRoutine()
    {
        if (string.IsNullOrEmpty(AuthManager.Username))
        {
            if (totalGames)      totalGames.text = "0";
            if (totalQuestions)  totalQuestions.text = "0";
            if (totalCorrect)    totalCorrect.text = "0";
            if (accuracyText)    accuracyText.text = "0%";
            yield break;
        }

        string url = baseUrl.TrimEnd('/') + "/stats/" + UnityWebRequest.EscapeURL(AuthManager.Username);
        Debug.Log("[Stats] Requesting: " + url);

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
                Debug.LogError($"Error loading stats: {req.responseCode} {req.error} | {req.downloadHandler.text}");
                if (totalGames)      totalGames.text = "-";
                if (totalQuestions)  totalQuestions.text = "-";
                if (totalCorrect)    totalCorrect.text = "-";
                if (accuracyText)    accuracyText.text = "-";
                yield break;
            }

            string json = req.downloadHandler.text;
            Debug.Log("[Stats] Response JSON: " + json);

            UserStatsDto stats = null;
            try
            {
                stats = JsonUtility.FromJson<UserStatsDto>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to parse stats JSON: " + ex.Message + " | " + json);
            }

            if (stats == null)
            {
                if (totalGames)      totalGames.text = "0";
                if (totalQuestions)  totalQuestions.text = "0";
                if (totalCorrect)    totalCorrect.text = "0";
                if (accuracyText)    accuracyText.text = "0%";
                yield break;
            }

            if (totalGames)      totalGames.text = stats.totalGamesPlayed.ToString();
            if (totalQuestions)  totalQuestions.text = stats.totalQuestionsAnswered.ToString();
            if (totalCorrect)    totalCorrect.text = stats.totalCorrectAnswers.ToString();
            if (accuracyText)    accuracyText.text  = (stats.accuracy * 100f).ToString("F1") + "%";
        }
    }

    public void RefreshStats()
    {
    StopAllCoroutines();
    StartCoroutine(LoadStatsRoutine());
    }

    public void OnBackButtonClicked()
    {
        if (userHubController != null)
        {
            userHubController.ShowHubFromStats();
        }
        else if (statsPanel)
        {
            statsPanel.SetActive(false);
        }
    }
}
