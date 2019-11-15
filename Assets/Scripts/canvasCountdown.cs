using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class canvasCountdown : MonoBehaviour
{
    private GameObject canvasObj;
    private Text text;
    private int num;
    private bool countdownActive;
    // Start is called before the first frame update
    void Start()
    {
        num = 3;
        countdownActive = true;

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
        canvasObj.GetComponent<RawImage>().color = Color.black;

        // Create Text object
        textObj = new GameObject();
        textObj.transform.parent = canvasObj.transform;
        textObj.name = "TextObj";

        // Add Text component to textObject
        text = textObj.AddComponent<Text>();
        // text.font = (Font)Resources.Load("Library/Arial");
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.text = num.ToString();
        text.fontSize = 100;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;

        // Text position
        rectTransform = text.GetComponent<RectTransform>();
        rectTransform.localPosition = new Vector3(0, 0, 0);
        rectTransform.sizeDelta = new Vector2(200, 200);

        StartCoroutine("Countdown");
    }

    public void deactivateCountdown()
    {
        countdownActive = false;
        StopCoroutine("Countdown");
        canvasObj.SetActive(false);
    }

    public void activateCountdown()
    {
        num = 3;
        countdownActive = true;
        canvasObj.SetActive(true);
        StartCoroutine("Countdown");
    }

    IEnumerator Countdown()
    {
        Debug.Log("in coroutine countdown");
        while (num > 0)
        {
            yield return new WaitForSeconds(1);
            num = num - 1;
            text.text = num != 0 ? num.ToString() : "GO";
        }
        if (num == 0)
        {
            yield return new WaitForSeconds(1);
            deactivateCountdown();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!countdownActive && Input.GetKeyDown("space")) {
            activateCountdown();
        }
    }
}
