using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChatCanvas : MonoBehaviour
{
    public static Color chatColor = new Color(0.2f, 0.2f, 0.2f);
    public static Color leaveColor = Color.grey;
    public static Color joinColor = Color.green;

    public GameObject CanvasMessager;
    public VerticalLayoutGroup chatMessageGroup;

    public void NewMessage(string message, Color messageColor)
    {
        Text t = Instantiate(CanvasMessager, chatMessageGroup.transform).GetComponentInChildren<Text>();
        t.text = message;
        t.color = messageColor;
    }
}
