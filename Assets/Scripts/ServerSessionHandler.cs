using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ServerSessionHandler : MonoBehaviour
{
    public static ServerSessionHandler Instance { get; private set; }

    public int ServerID = 1;
    public string Password = "chocoladeTaart7";
    public int SessionID { get; private set; }

    private void Awake()
    {
        Instance = this;
        StartCoroutine(AttemptLogin());
    }

    private IEnumerator AttemptLogin()
    {

        using (UnityWebRequest www = UnityWebRequest.Get($"https://studenthome.hku.nl/~justin.stolk/NetworkProgramming/server_login.php?server_id={ServerID}&password={Password}"))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                Debug.Log(www.downloadHandler.text);
                int echo = int.Parse(www.downloadHandler.text);
                if(echo == 0)
                {
                    Debug.Log("Failed to get a session Id back!");
                }
                else
                {
                    SessionID = echo;
                }
            }
            yield return null;
        }
    }
}
