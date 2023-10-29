using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class PlayerSessionHandler : MonoBehaviour
{
    public static PlayerSessionHandler Instance { get; private set; }
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
}
