using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Game : MonoBehaviour
{
    public GameObject recordObj1, recordObj2;
    Recorder player1, player2;
    private GameObject canvasObj;
    private bool countdownActive;
    private Text text;
    private int num;
    float startTime = 0;

    void Start()
    {
        if(recordObj1 && recordObj2)
        {
            player1 = recordObj1.GetComponent<Recorder>();
            player2 = recordObj2.GetComponent<Recorder>();
            player1.choice = Recorder.Purpose.Record;
            player1.handChoice = Recorder.Handedness.Left;
            player2.choice = Recorder.Purpose.Record;
            player2.handChoice = Recorder.Handedness.Right;
        }

        GameObject textObj;
        Canvas countdownCanvas;
        RectTransform rectTransform;

        // Create Canvas and containing Game Object
        canvasObj = new GameObject();
        canvasObj.name = "CanvasObj";
        canvasObj.AddComponent<Canvas>();

        //Create Canvas properties & set color to black
        countdownCanvas = canvasObj.GetComponent<Canvas>();
        countdownCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        canvasObj.AddComponent<RawImage>();
        // Create Text object
        textObj = new GameObject();
        textObj.transform.parent = canvasObj.transform;
        textObj.name = "TextObj";

        // Add Text component to textObject
        text = textObj.AddComponent<Text>();
        // text.font = (Font)Resources.Load("Library/Arial");
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        // Text position
        rectTransform = text.GetComponent<RectTransform>();
        rectTransform.localPosition = new Vector3(-Screen.width / 2 + Screen.width / 5, 0, 0);
        rectTransform.sizeDelta = new Vector2(Screen.width / 2, Screen.width / 2);

        Init();
    }

    private void Init()
    {
        num = 3;
        countdownActive = false;
        canvasObj.GetComponent<RawImage>().color = new Color(0, 0, 0, 0.5f);
        text.text = num.ToString();
        text.fontSize = Screen.width / 4;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space) && !countdownActive)
        {
            Init();
            countdownActive = true;
            startTime = Time.time;
            player1.choice = Recorder.Purpose.Predict;
            player2.choice = Recorder.Purpose.Predict;
        }

        if(countdownActive)
        {
            text.text = num != 0 ? num.ToString() : "GO";
            if (Time.time - startTime >= 1 && num > 0)
            {
                num--;
                startTime = Time.time;
            }

            if (num == 0 && Time.time - startTime >= 0.5f)
            {
                countdownActive = false;
                canvasObj.GetComponent<RawImage>().color = new Color(0, 0, 0, 0.0f);
                text.fontSize = Screen.width / 12;
                text.text = Winner();
            }
        }
    }

    string Winner()
    {
        string p1 = player1.Prediction();
        string p2 = player2.Prediction();

        if (p1 == "rock" && p2 == "paper")
            return "Player 2 Wins!!!";
        if (p2 == "rock" && p1 == "paper")
            return "Player 1 Wins!!!";
        if (p1 == "rock" && p2 == "scissors")
            return "Player 1 Wins!!!";
        if (p2 == "rock" && p1 == "scissors")
            return "Player 2 Wins!!!";
        if (p2 == "paper" && p1 == "scissors")
            return "Player 1 Wins!!!";
        if (p1 == "paper" && p2 == "scissors")
            return "Player 2 Wins!!!";
        if (p1 == "rock" && p2 == "lizard")
            return "Player 1 Wins!!!";
        if (p2 == "rock" && p1 == "lizard")
            return "Player 2 Wins!!!";
        if (p2 == "rock" && p1 == "spock")
            return "Player 1 Wins!!!";
        if (p1 == "rock" && p2 == "spock")
            return "Player 2 Wins!!!";
        if (p1 == "lizard" && p2 == "paper")
            return "Player 1 Wins!!!";
        if (p2 == "lizard" && p1 == "paper")
            return "Player 2 Wins!!!";
        if (p2 == "lizard" && p1 == "scissors")
            return "Player 1 Wins!!!";
        if (p1 == "lizard" && p2 == "scissors")
            return "Player 2 Wins!!!";
        if (p1 == "lizard" && p2 == "spock")
            return "Player 1 Wins!!!";
        if (p2 == "lizard" && p1 == "spock")
            return "Player 2 Wins!!!";
        if (p2 == "spock" && p1 == "scissors")
            return "Player 2 Wins!!!";
        if (p1 == "spock" && p2 == "scissors")
            return "Player 1 Wins!!!";
        if (p1 == "spock" && p2 == "paper")
            return "Player 2 Wins!!!";
        if (p2 == "spock" && p1 == "paper")
            return "Player 1 Wins!!!";

        print(p1 + " " + p2);
        return "Draw!!!";
    }
}
