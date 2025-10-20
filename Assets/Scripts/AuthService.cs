using System;
using System.Text;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class AuthService : MonoBehaviour
{
    [SerializeField] private string baseUrl = "http://localhost:5164"; // set your port

    [Serializable] public class RegisterReq { public string username; public string password; public string email; }
    [Serializable] public class LoginReq    { public string usernameOrEmail; public string password; }
    [Serializable] public class TokenRes    { public string token; }

    public IEnumerator Register(string username, string password, string email,
                                Action<TokenRes> onOk, Action<string> onErr)
    {
        var body = JsonUtility.ToJson(new RegisterReq { username = username, password = password, email = email ?? "" });
        yield return Post("/auth/register", body,
            json => onOk?.Invoke(JsonUtility.FromJson<TokenRes>(json)),
            onErr);
    }

    public IEnumerator Login(string usernameOrEmail, string password,
                             Action<TokenRes> onOk, Action<string> onErr)
    {
        var body = JsonUtility.ToJson(new LoginReq { usernameOrEmail = usernameOrEmail, password = password });
        yield return Post("/auth/login", body,
            json => onOk?.Invoke(JsonUtility.FromJson<TokenRes>(json)),
            onErr);
    }

    private IEnumerator Post(string path, string json, Action<string> ok, Action<string> err)
    {
        var url = $"{baseUrl}{path}";
        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

#if UNITY_2020_3_OR_NEWER
        bool bad = req.result == UnityWebRequest.Result.ConnectionError || req.result == UnityWebRequest.Result.ProtocolError;
#else
        bool bad = req.isNetworkError || req.isHttpError;
#endif
        if (bad) err?.Invoke($"{req.responseCode} {req.error}");
        else ok?.Invoke(req.downloadHandler.text);
    }
}
