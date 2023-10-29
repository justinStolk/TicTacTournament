using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LocalGameManager : MonoBehaviour
{
    public static LocalGameManager Instance { get; private set; }

    [SerializeField] private ChatClientExample.Client client;

    [SerializeField] private Button defaultButton;
    [SerializeField] private Sprite player1Graphic;
    [SerializeField] private Sprite player2Graphic;

    [SerializeField] private GridLayoutGroup fieldLayout;

    private Button[,] fieldButtons = new Button[3,3];

    private void Awake()
    {
        for (uint x = 0; x < 3; x++)
        {
            for (uint y = 0; y < 3; y++)
            {
                uint posX = x;
                uint posY = y;
                Button empty = Instantiate(defaultButton, fieldLayout.transform);
                empty.onClick.AddListener(() => client.SendIntendedPosition(posX, posY));
                empty.image.color = new Color(1, 1, 1, 0.5f);
                fieldButtons[x, y] = empty;
            }
        }
    }

    public void ReceiveInput(uint x, uint y, uint value)
    {
        Debug.Log("Received input back and should now update an image");
        Sprite graphic = value == 1 ? player1Graphic : player2Graphic;
        fieldButtons[x, y].image.sprite = graphic;
        fieldButtons[x, y].image.color = Color.white;
    }
}
