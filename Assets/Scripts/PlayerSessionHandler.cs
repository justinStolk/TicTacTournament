using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class PlayerSessionHandler : MonoBehaviour
{
    public static PlayerSessionHandler Instance { get; private set; }
    public string UserID { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }

    [SerializeField] private UnityEvent<string> onLoginFailed;

    private Coroutine coroutine;

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    public void Login()
    {
        if(coroutine == null)
        {
            coroutine = StartCoroutine(AttemptLogin());
        }
    }

    public void SendScore()
    {
        if (coroutine == null)
        {
            coroutine = StartCoroutine(SendWinInformation());
        }
    }

    private IEnumerator AttemptLogin()
    {

        using(UnityWebRequest www = UnityWebRequest.Get($"https://studenthome.hku.nl/~justin.stolk/NetworkProgramming/user_login.php?username={Username}&password={Password}"))
        {
            yield return www.SendWebRequest();

            if(www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
                onLoginFailed?.Invoke(www.error);
            }
            else
            {
                Debug.Log(www.downloadHandler.text);
            }

            yield return null;
            coroutine = null;
        }
    }

    private IEnumerator SendWinInformation()
    {
        using (UnityWebRequest www = UnityWebRequest.Get($"https://studenthome.hku.nl/~justin.stolk/NetworkProgramming/Insert_Score.php?user_id={UserID}&score=1&game_id=6"))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                Debug.Log(www.downloadHandler.text);
            }
        }
    }
}
