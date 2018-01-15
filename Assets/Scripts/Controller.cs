using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Controller : MonoBehaviour
{
    public GameObject Cursor, Target, TrackingLines, Text;
    public int LengthOfTrial, SystemOrder;
    public double K, DisturbanceGain, InputGain;

    private StreamWriter Writer;
    private String Filename, Subject, Type;
    private bool InitializedTrial, RunningTrial, InsideTarget;
    private float x, y, StartTime, CurrentTime;
    private Tracking[] Trackers = new Tracking[2];
    private List<Dictionary<string, object>> TrialData;
    private int TotalTrials, CurrentTrial, Trial;
    private double CircleRadius = 27.5;
    private float Radius, Angle;
    private double TargetOffset;

    void Start()
    {
        Screen.SetResolution(1000, 1000, false);
        // Read in the trial list for this subject
        try
        {
            var path = Application.dataPath + "/../../trials.csv";
            TrialData = CSVReader.Read(path);
        }
        catch
        {
            var path = Application.dataPath + "/../trials.csv";
            TrialData = CSVReader.Read(path);
        }

        // Assumes that the Subject and SystemOrder do not change throughout the trials
        CurrentTrial = 0;
        Subject = TrialData[0]["Subject"].ToString();
        TotalTrials = TrialData.Count;
    }

    private void FixedUpdate()
    {
        if (RunningTrial)
        {
            var r0 = Trackers[0].Logger();
            var r1 = Trackers[1].Logger();
            var r = r0 + r1 + InsideTarget.ToString();
            Log(r, Writer);
            RunTrial();
        }
        else if (!InitializedTrial && CurrentTrial < TotalTrials)
        {
            InitializeTrial();
        }
        else if (InitializedTrial && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.JoystickButton2) || Input.GetKeyDown(KeyCode.JoystickButton16)) && CurrentTrial < TotalTrials)
        {
            StartTrial();
        }
    }

    void InitializeTrial()
    {
        // Initialize the trackers
        Type = TrialData[CurrentTrial]["Type"].ToString();

        if (Type == "Tracking")
        {
            InitTracking();
        }
        else if (Type == "Fitts")
        {
            InitFitts();
        }
        else
        {
            print("You messed this one up, bucko.");
        }

        Cursor.transform.localPosition = new Vector3(0, 0, 0);
        InitializedTrial = true;
    }

    void InitTracking()
    {
        Trackers[0] = new Tracking("Horizontal", K, SystemOrder, DisturbanceGain, InputGain);
        Trackers[1] = new Tracking("Vertical", K, SystemOrder, DisturbanceGain, InputGain);
        Target.transform.localPosition = new Vector3(0, 0, 0);

        Cursor.SetActive(false);
        Target.SetActive(true);
        TrackingLines.SetActive(true);
    }

    void InitFitts()
    {
        Trackers[0] = new Tracking("Horizontal", K, SystemOrder, 0, InputGain);
        Trackers[1] = new Tracking("Vertical", K, SystemOrder, 0, InputGain);

        Radius = float.Parse(TrialData[CurrentTrial]["Radius"].ToString());
        Angle = float.Parse(TrialData[CurrentTrial]["Angle"].ToString());
        Target.transform.localPosition = PointOnCircle(Radius, Angle);
        // Target.transform.localPosition = RandomPointOnUnitCircle(250);

        Cursor.SetActive(false);
        Target.SetActive(false);
        TrackingLines.SetActive(false);
    }

    void StartTrial()
    {
        // Output data to file
        Trial = (int)TrialData[CurrentTrial]["Trial"];        
        Filename = String.Format("logs/subject_{0}_trial_{1}_log_{2}.csv",
                                 Subject, Trial, DateTime.UtcNow.ToLocalTime().ToString("yyy_MM_dd_hh_mm_ss"));
        Writer = File.AppendText(Filename);

        // Set start time
        RunningTrial = true;
        StartTime = Time.time;
        CurrentTime = 0;

        Cursor.SetActive(true);
        Target.SetActive(true);
    }

    void RunTrial()
    {
        if (Type == "Fitts")
        {
            // Figure out if we're inside the target
            TargetOffset = (Target.transform.localPosition - Cursor.transform.localPosition).magnitude;
            InsideTarget = TargetOffset < CircleRadius;

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetAxis("Axis 6") > 0.2 || Input.GetKeyDown(KeyCode.JoystickButton0))
            {
                EndTrial();
            }
        }

        // Update the dynamics
        CurrentTime = Time.time - StartTime;

        // Tracker update, yaxis is arbitrarily 60 seconds ahead
        Trackers[0].UpdateDynamics(CurrentTime);
        Trackers[1].UpdateDynamics(CurrentTime + 60);

        // Where should we put the cursor?
        x = Trackers[0].Output();
        y = Trackers[1].Output();

        // Keep the cursor within the bounds of the screen
        if (x > 500)
        {
            x = 500;
        }
        else if (x < -500)
        {
            x = -500;
        }

        if (y > 500)
        {
            y = 500;
        }
        else if (y < -500)
        {
            y = -500;
        }

        // Place the cursor
        Cursor.transform.localPosition = new Vector3(x, y, 0);

        // If we're done here--end
        if (CurrentTime > LengthOfTrial)
        {
            EndTrial();
        }
    }

    void EndTrial()
    {
        Writer.Close();

        CurrentTrial += 1;
        if (CurrentTrial >= TotalTrials)
        {
            // Exit experiment
            Text.SetActive(true);
            TrackingLines.SetActive(false);
            Cursor.SetActive(false);
            Target.SetActive(false);
        }

        RunningTrial = false;
        InitializedTrial = false;
    }

    static Vector3 RandomPointOnUnitCircle(float radius)
    {
        float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2);
        float x = Mathf.Sin(angle) * radius;
        float y = Mathf.Cos(angle) * radius;

        return new Vector3(x, y, 0);
    }

    static Vector3 PointOnCircle(float radius, float angle)
    {
        float x = Mathf.Sin(angle) * radius * 250;
        float y = Mathf.Cos(angle) * radius * 250;

        return new Vector3(x, y, 0);
    }

    void Log(string logMessage, TextWriter w)
    {
        w.WriteLine("{0}, {1}", CurrentTime, logMessage);
    }
}
