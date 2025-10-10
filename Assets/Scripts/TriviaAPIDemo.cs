using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class TriviaApiDemo : MonoBehaviour
{
    // Use the port your API prints (e.g., 5164 or 5174)
    [SerializeField] string baseUrl = "http://localhost:5164";
    int sessionId;

    void Start() { StartCoroutine(Flow()); }

    IEnumerator Flow()
    {
        // 1) Create session (POST /session)
        using (var s = UnityWebRequest.PostWwwForm($"{baseUrl}/session", "")) {
            s.downloadHandler = new DownloadHandlerBuffer();
            yield return s.SendWebRequest();
            if (s.result != UnityWebRequest.Result.Success) { Debug.LogError(s.error); yield break; }
            var sess = JsonUtility.FromJson<SessionResp>(s.downloadHandler.text);
            sessionId = sess.sessionId;
            Debug.Log("Session: " + sessionId);
        }

        // 2) Get question (GET /question)
        QuestionResp qData;
        using (var q = UnityWebRequest.Get($"{baseUrl}/question")) {
            q.downloadHandler = new DownloadHandlerBuffer();
            yield return q.SendWebRequest();
            if (q.result != UnityWebRequest.Result.Success) { Debug.LogError(q.error); yield break; }
            qData = JsonUtility.FromJson<QuestionResp>(q.downloadHandler.text);
            Debug.Log($"Q{qData.questionId}: {qData.text}");
            for (int i = 0; i < qData.choices.Length; i++) Debug.Log($"{i}: {qData.choices[i]}");
        }

        // 3) Submit answer (POST /answer?sessionId=&questionId=&choiceIndex=)
        using (var a = UnityWebRequest.PostWwwForm(
            $"{baseUrl}/answer?sessionId={sessionId}&questionId={qData.questionId}&choiceIndex=0", "")) {
            a.downloadHandler = new DownloadHandlerBuffer();
            yield return a.SendWebRequest();
            if (a.result != UnityWebRequest.Result.Success) { Debug.LogError(a.error); yield break; }
            var ans = JsonUtility.FromJson<AnswerResp>(a.downloadHandler.text);
            Debug.Log($"Correct: {ans.correct} | NewScore: {ans.newScore}");
        }
    }

    // DTOs for Unity's JsonUtility
    [System.Serializable] public class SessionResp { public int sessionId; }
    [System.Serializable] public class QuestionResp { public int questionId; public string text; public string[] choices; }
    [System.Serializable] public class AnswerResp { public bool correct; public int newScore; }
}
