using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using Leap.Unity;
using Leap;

public class Recorder : MonoBehaviour
{
    Controller controller;
    List<string> csvRow = new List<string>();
    List<string> header = new List<string>();
    List<List<string>> csvRows = new List<List<string>>();
    public List<Transform> testIndex = new List<Transform>();
    public List<Transform> testMiddle = new List<Transform>();
    public List<Transform> testRing = new List<Transform>();
    public List<Transform> testPinky = new List<Transform>();
    public List<Transform> testThumb = new List<Transform>();
    List<List<Transform>> testFingers = new List<List<Transform>>();
    public Transform leapOrigin;

    public string filename;
    List<List<float>> playBackData = new List<List<float>>();
    public enum Purpose { Record, Playback };
    public Purpose choice = Purpose.Record;
    int frameCount = 0;
    int playBackCount = 0;
    public GameObject palmRef;

    void Start()
    {
        // Instantiate Leap controller
        controller = new Controller();

        // store joints in 2D list
        testFingers.Add(testThumb);
        testFingers.Add(testIndex);
        testFingers.Add(testMiddle);
        testFingers.Add(testRing);
        testFingers.Add(testPinky);

        // create header for output file
        List<string> temp = new List<string> { "Thumb1", "Thumb2", "Thumb3", "Index1", "Index2", "Index3", "Index4", "Middle1", "Middle2", "Middle3",
        "Middle4", "Ring1", "Ring2", "Ring3","Ring4", "Pinky1", "Pinky2", "Pinky3", "Pinky4" };

        for(int i = 0; i < temp.Count; ++i)
        {
            header.Add(temp[i] + "_X");
            header.Add(temp[i] + "_Y");
            header.Add(temp[i] + "_Z");
            header.Add(temp[i] + "_W");
        }

        csvRows.Add(header);

        if(choice == Purpose.Playback)
        {
            LoadFile();
        }
    }

    void Update()
    {
        if (choice == Purpose.Record)
        {
            if (controller.IsConnected)
            {
                Frame frame = controller.Frame();
                if (frame.Hands.Count > 0)
                {
                    List<Hand> hands = frame.Hands;
                    for (int i = 0; i < hands.Count; ++i)
                        RecordHands(hands[i]);
                }
                /*
                if(record && frameCount == 120) // 2 seconds, auto stop
                {
                    Log();
                    UnityEditor.EditorApplication.isPlaying = false;
                }*/

                KeyListener();
                frameCount++;
            }
        }

        else
        {
            Visualize();
        }
    }

    void RecordHands(Hand hand)
    {
        List<Finger> fingers = hand.Fingers;

        if (hand.IsLeft)
        {
            // create leap hand basis matrix
            Vector3 palmPos = hand.PalmPosition.ToVector3();
            palmPos = new Vector3(palmPos.x, palmPos.y, -palmPos.z) / 1000f + leapOrigin.position;
            Vector3 palmNormal = hand.PalmNormal.ToVector3();
            palmNormal = new Vector3(palmNormal.x, palmNormal.y, -palmNormal.z);
            Vector3 palmDirection = hand.Direction.ToVector3();
            palmDirection = new Vector3(palmDirection.x, palmDirection.y, -palmDirection.z);
            Vector3 zBasis = Vector3.Cross(palmDirection, palmNormal);
            Matrix3x3 handBasis = new Matrix3x3(palmNormal, palmDirection, zBasis);

            // draw leap hand basis (debug)
            Debug.DrawLine(palmPos, palmPos + 0.1f * palmNormal, Color.red, 0.01f, false);
            Debug.DrawLine(palmPos, palmPos + 0.1f * palmDirection, Color.blue, 0.01f, false);
            Debug.DrawLine(palmPos, palmPos + 0.1f * zBasis, Color.white, 0.01f, false);

            // create new basis matrix
            Vector3 b1 = -palmRef.transform.right;
            Vector3 b2 = -palmRef.transform.forward;
            Vector3 b3 = Vector3.Cross(b2, b1);
            Matrix3x3 newBasis = new Matrix3x3(b3, b1, b2);

            // draw new hand basis (debug)
            Debug.DrawLine(palmRef.transform.position, palmRef.transform.position + 0.1f * b3, Color.red, 0.01f, false); // normal
            Debug.DrawLine(palmRef.transform.position, palmRef.transform.position + 0.1f * b1, Color.blue, 0.01f, false); // direction
            Debug.DrawLine(palmRef.transform.position, palmRef.transform.position + 0.1f * b2, Color.white, 0.01f, false); // right

            csvRow = new List<string>(new string[header.Count]);    // initialize empty row with capacity = header.Count
            int j = 0;

            // assume order THUMB, INDEX, MIDDLE, PINKY
            for (int i = 0; i < fingers.Count; ++i)
                RecordFinger(handBasis, newBasis, fingers[i], testFingers[i], ref j);

            if (choice == Purpose.Record)
                csvRows.Add(csvRow);
        }
    }

    void RecordFinger(Matrix3x3 handBasis, Matrix3x3 newBasis, Finger finger, List<Transform> joints, ref int listStartIndex)
    {
        for (int i = 0; i < joints.Count; ++i)
        {
            Vector3 fingerDir = finger.bones[i].Basis.zBasis.ToVector3();
            Vector3 fingerNormal = finger.bones[i].Basis.yBasis.ToVector3();

            if (finger.Type == Finger.FingerType.TYPE_THUMB /*&& finger.bones[i].Type == Bone.BoneType.TYPE_METACARPAL*/)
            {
                fingerDir = finger.bones[i + 1].Basis.zBasis.ToVector3();
                fingerNormal = finger.bones[i + 1].Basis.yBasis.ToVector3();
            }

            fingerNormal = new Vector3(-fingerNormal.x, -fingerNormal.y, fingerNormal.z);
            fingerDir = new Vector3(-fingerDir.x, -fingerDir.y, fingerDir.z);

            Vector3 fingerPos = finger.bones[i].Center.ToVector3();
            fingerPos = new Vector3(fingerPos.x, fingerPos.y, -fingerPos.z) / 1000f + leapOrigin.position;
            Vector3 fingerX = Vector3.Cross(fingerDir, fingerNormal);

            Debug.DrawLine(fingerPos, fingerPos + 0.01f * fingerNormal, Color.red, 0.01f, false);
            Debug.DrawLine(fingerPos, fingerPos + 0.01f * fingerDir, Color.blue, 0.01f, false);
            Debug.DrawLine(fingerPos, fingerPos + 0.01f * fingerX, Color.white, 0.01f, false);

            // convert from world to hand
            Vector3 newNorm = handBasis * fingerNormal;
            Vector3 newFwd = handBasis * fingerX;

            // convert from hand to new hand, update rotation
            joints[i].transform.rotation = Quaternion.LookRotation(-(newBasis * newFwd), newBasis * newNorm);

            //Vector3 eulers = joints[i].transform.rotation.eulerAngles;
            Quaternion rotation = joints[i].transform.rotation;
            csvRow[listStartIndex++] = rotation.x.ToString();
            csvRow[listStartIndex++] = rotation.y.ToString();
            csvRow[listStartIndex++] = rotation.z.ToString();
            csvRow[listStartIndex++] = rotation.w.ToString();
        }
    }

    void KeyListener()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            Log();
            UnityEditor.EditorApplication.isPlaying = false;
        }
    }

    void Visualize()
    {
        int j = 0;
        for(int i = 0; i < testThumb.Count; ++i)
        {
            Quaternion q = new Quaternion(playBackData[playBackCount][j++], playBackData[playBackCount][j++], playBackData[playBackCount][j++], playBackData[playBackCount][j++]);
            testThumb[i].transform.rotation = q;
        }

        for (int i = 0; i < testIndex.Count; ++i)
        {
            Quaternion q = new Quaternion(playBackData[playBackCount][j++], playBackData[playBackCount][j++], playBackData[playBackCount][j++], playBackData[playBackCount][j++]);
            testIndex[i].transform.rotation = q;
        }

        for (int i = 0; i < testMiddle.Count; ++i)
        {
            Quaternion q = new Quaternion(playBackData[playBackCount][j++], playBackData[playBackCount][j++], playBackData[playBackCount][j++], playBackData[playBackCount][j++]);
            testMiddle[i].transform.rotation = q;
        }

        for (int i = 0; i < testRing.Count; ++i)
        {
            Quaternion q = new Quaternion(playBackData[playBackCount][j++], playBackData[playBackCount][j++], playBackData[playBackCount][j++], playBackData[playBackCount][j++]);
            testRing[i].transform.rotation = q;
        }

        for (int i = 0; i < testPinky.Count; ++i)
        {
            Quaternion q = new Quaternion(playBackData[playBackCount][j++], playBackData[playBackCount][j++], playBackData[playBackCount][j++], playBackData[playBackCount][j++]);
            testPinky[i].transform.rotation = q;
        }

        playBackCount = (playBackCount + 1) % playBackData.Count;
    }

    void LoadFile()
    {
        using (var reader = new StreamReader("./Assets/CSVOutput/" + filename + ".csv"))
        {
            int index = 0;
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();

                if (index > 0 && line.Length > 0)  // skip header
                {
                    string[] values = line.Split(',');
                    List<float> row = new List<float>();

                    for (int i = 0; i < values.Length; ++i)
                    {
                        float num = float.Parse(values[i]);
                        row.Add(num);
                    }

                    playBackData.Add(row);
                }

                index++;
            }
        }
    }

    void Log()
    {
        // default file name
        if (filename.Length == 0)
            filename = "untitled";

        string[][] output = new string[csvRows.Count][];

        for (int i = 0; i < output.Length; i++)
        {
            output[i] = csvRows[i].ToArray();
        }

        int length = output.GetLength(0);
        string delimiter = ",";

        StringBuilder stringBuilder = new StringBuilder();

        for (int j = 0; j < length; j++)
            stringBuilder.AppendLine(string.Join(delimiter, output[j]));

        string filePath = "Assets/CSVOutput/" + filename + ".csv";

        StreamWriter outStream = System.IO.File.CreateText(filePath);
        outStream.WriteLine(stringBuilder);
        outStream.Close();

        Debug.Log("Raw file export completed! " + filename);
    }
}
