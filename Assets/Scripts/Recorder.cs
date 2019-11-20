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
    List<List<float>> theta1 = new List<List<float>>();
    List<List<float>> theta2 = new List<List<float>>();

    public enum Purpose { Record, Playback, Predict };
    public enum Handedness { Left, Right };
    public Purpose choice = Purpose.Record;
    public Handedness handChoice = Handedness.Left;
    int frame = 1;
    public GameObject palmRef;
    bool recording = false;
    int recordCount = 0;
    int prevPrediction = -1;

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
            LoadFile("./CSVOutput/" + filename + ".csv", playBackData);

        //if (choice == Purpose.Predict)
        //{
            LoadFile("Assets/Scripts/Theta1.csv", theta1);
            LoadFile("Assets/Scripts/Theta2.csv", theta2);
        //}
    }

    void Update()
    {
        if (choice == Purpose.Record || choice == Purpose.Predict)
        {
            if (controller.IsConnected)
            {
                Frame frame = controller.Frame();

                if (recording) // 1 second, auto stop
                {
                    if (csvRows.Count == 61)
                        Log();
                }

                if (frame.Hands.Count > 0)  // if there are any hands being tracked
                {
                    List<Hand> hands = frame.Hands;
                    for (int i = 0; i < hands.Count; ++i)
                        RecordHands(hands[i]);
                }

                //if (recording && Input.GetKeyDown(KeyCode.Space))
                    //Log();
            }
        }

        else
            Visualize();
    }

    public void StartRecording()
    {
        if (choice == Purpose.Record)
            recording = true;
    }

    void RecordHands(Hand hand)
    {
        List<Finger> fingers = hand.Fingers;

        // create leap hand basis matrix
        Vector3 palmPos = hand.PalmPosition.ToVector3();
        palmPos = new Vector3(palmPos.x, palmPos.y, -palmPos.z) / 1000f + leapOrigin.position;
        Vector3 palmNormal = hand.PalmNormal.ToVector3();
        palmNormal = new Vector3(palmNormal.x, palmNormal.y, -palmNormal.z);
        Vector3 palmDirection = hand.Direction.ToVector3();
        palmDirection = new Vector3(palmDirection.x, palmDirection.y, -palmDirection.z);
        Vector3 zBasis = hand.IsLeft ? Vector3.Cross(palmDirection, palmNormal) : Vector3.Cross(palmNormal, palmDirection);    

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

        Quaternion q1 = palmRef.transform.rotation;
        Quaternion q2 = testFingers[0][0].transform.rotation;

        bool isRecordedHand = (hand.IsLeft && handChoice == Handedness.Left) || (hand.IsRight && handChoice == Handedness.Right);
        if (isRecordedHand)
        {
            csvRow = new List<string>(new string[header.Count]);    // initialize empty row with capacity = header.Count
            List<float> X = new List<float>();  // for collecting features
            int j = 0;

            // assume order THUMB, INDEX, MIDDLE, PINKY
            for (int i = 0; i < fingers.Count; ++i)
                RecordFinger(handBasis, newBasis, fingers[i], testFingers[i], ref j, ref X);

            if (recording)
                csvRows.Add(csvRow);

            if (choice == Purpose.Predict)
            {
                List<float> predictionList = Model.NNPredict(theta1, theta2, X);
                float max = float.NegativeInfinity;
                int prediction = 0;

                // index of highest probablity
                for (int i = 0; i < predictionList.Count; ++i)
                {
                    if (predictionList[i] > max)
                    {
                        max = predictionList[i];
                        prediction = i;
                    }
                }

                if (prediction != prevPrediction)
                {
                    prevPrediction = prediction;
                    print(Prediction());
                }
            }
        }
    }

    void RecordFinger(Matrix3x3 handBasis, Matrix3x3 newBasis, Finger finger, List<Transform> joints, ref int listStartIndex, ref List<float> X)
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
            Vector3 fingerX = handChoice == Handedness.Left ? Vector3.Cross(fingerDir, fingerNormal) : Vector3.Cross(fingerNormal, fingerDir);

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

            if (recording)
            {
                csvRow[listStartIndex++] = rotation.x.ToString();
                csvRow[listStartIndex++] = rotation.y.ToString();
                csvRow[listStartIndex++] = rotation.z.ToString();
                csvRow[listStartIndex++] = rotation.w.ToString();
            }

            if (choice == Purpose.Predict)
            {
                X.Add(rotation.x);
                X.Add(rotation.y);
                X.Add(rotation.z);
                X.Add(rotation.w);
            }
        }
    }


    void Visualize()
    {
        int k = 0;
        for(int i = 0; i < testFingers.Count; ++i)
        {
            for(int j = 0; j < testFingers[i].Count; ++j)
                testFingers[i][j].transform.rotation = new Quaternion(playBackData[frame][k++], playBackData[frame][k++], playBackData[frame][k++], playBackData[frame][k++]);
        }

        frame = (frame + 1) % playBackData.Count;
    }

    public string Prediction()
    {
        string name = "none";
        if (prevPrediction == 0) name = "rock";
        else if (prevPrediction == 1) name = "paper";
        else if (prevPrediction == 2) name = "scissors";
        return name;
    }

    void LoadFile(string _path, List<List<float>> _variable)
    {
        using (var reader = new StreamReader(_path))
        {
            int index = 0;
            if (choice == Purpose.Predict) index++;
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

                    _variable.Add(row);
                }

                index++;
            }
        }
    }


    public bool Recording()
    {
        return recording;
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

        string count = recordCount < 10 ? "0" + recordCount.ToString() : recordCount.ToString();
        string outputName = filename + count + ".csv";
        string filePath = "Assets/CSVOutput/" + outputName;

        StreamWriter outStream = System.IO.File.CreateText(filePath);
        outStream.WriteLine(stringBuilder);
        outStream.Close();

        Debug.Log("Raw file export completed! " + outputName);
        //UnityEditor.EditorApplication.isPlaying = false;

        recording = false;
        recordCount++;
        csvRows.Clear();
        csvRows.Add(header);
    }
}
