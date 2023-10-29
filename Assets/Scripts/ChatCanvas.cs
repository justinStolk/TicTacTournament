using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChatCanvas : MonoBehaviour
{
    public static Color chatColor = new Color(0.2f, 0.2f, 0.2f);
    public static Color leaveColor = Color.grey;
    public static Color joinColor = Color.green;
    public static float messageTime = 5f;

    public GameObject CanvasMessager;
    public VerticalLayoutGroup chatMessageGroup;

    public void NewMessage(string message, Color messageColor)
    {
        GameObject messager = Instantiate(CanvasMessager, chatMessageGroup.transform);
        Text t = messager.GetComponentInChildren<Text>();
        t.text = message;
        t.color = messageColor;
        Destroy(messager, messageTime);
    }
}
