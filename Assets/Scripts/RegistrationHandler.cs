using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class RegistrationHandler : MonoBehaviour
{
    public string Username { get; set; }
    public string Password { get; set; }
    public string PasswordValidation { get; set; }

    [SerializeField] private UnityEvent<string> onRegistrationFailed;

    private Coroutine coroutine;
    public void Register()
    {
        if(Password != PasswordValidation)
        {
            onRegistrationFailed?.Invoke("Passwords do not match!");
        }
        if(coroutine == null)
        {
            coroutine = StartCoroutine(AttemptRegistration());
        }
    }

    private IEnumerator AttemptRegistration()
    {
        using(UnityWebRequest www = UnityWebRequest.Get($"https://studenthome.hku.nl/~justin.stolk/NetworkProgramming/Insert_User.php?username={Username}&password={Password}"))
        {
            yield return www.SendWebRequest();

            if(www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
                onRegistrationFailed?.Invoke(www.error);
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
