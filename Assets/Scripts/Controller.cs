using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Controller : MonoBehaviour
{
    public GameObject Cursor, Target, Text;
    public int TotalTrials, CurrentTrial, LengthOfTrial, SystemOrder;
    public double K, DisturbanceGain, InputGain;

    private StreamWriter Writer;
    private String Filename, Subject;
    private bool RunningTrial;
    private float x, y, StartTime, CurrentTime;
    private Vector3 Position;
    private Tracking[] Trackers = new Tracking[2];
    private List<Dictionary<string, object>> TrialData;

    void Start()
    {
        try {
            var path = Application.dataPath + "/../../trials.csv";
            TrialData = CSVReader.Read(path);
        } catch {
            var path = Application.dataPath + "/../trials.csv";
            TrialData = CSVReader.Read(path);
        }

        Subject = TrialData[0]["Subject"].ToString();

        // Initialize the trackers
        Trackers[0] = new Tracking("Horizontal", K, SystemOrder, DisturbanceGain, InputGain);
        Trackers[1] = new Tracking("Vertical", K, SystemOrder, DisturbanceGain, InputGain);
    }

    private void FixedUpdate()
    {
        if (RunningTrial)
        {
            var r0 = Trackers[0].Logger();
            var r1 = Trackers[1].Logger();
            var r = r0 + r1;
            Log(r, Writer);
            RunTrial();
        }
        else if (Input.GetKeyDown("space") && CurrentTrial <= TotalTrials)
        {
            StartTrial();
        }
    }

    void StartTrial()
    {
        // Output data to file
        Filename = String.Format("logs/subject_{0}_trial_{1}_log_{2}.csv", Subject, CurrentTrial, DateTime.UtcNow.ToLocalTime().ToString("yyy_MM_dd_hh_mm_ss"));
        Writer = File.AppendText(Filename);

        // Set start time
        RunningTrial = true;
        StartTime = Time.time;
        Cursor.SetActive(true);
    }

    void RunTrial()
    {
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
        Position = new Vector3(x, y, 0);
        Cursor.transform.localPosition = Position;

        // Update the dynamics
        CurrentTime = Time.time - StartTime;

        // Tracker update, yaxis is arbitrarily 60 seconds ahead
        Trackers[0].UpdateDynamics(CurrentTime);
        Trackers[1].UpdateDynamics(CurrentTime + 60);

        // If we're done here--end
        if (CurrentTime > LengthOfTrial)
        {
            EndTrial();
        }
    }

    void EndTrial()
    {
        Writer.Close();
        Cursor.SetActive(false);

        CurrentTrial += 1;
        if (CurrentTrial > TotalTrials)
        {
            // Exit experiment
            Text.SetActive(true);
        }

        RunningTrial = false;

        Trackers[0].Reset();
        Trackers[1].Reset();
    }

    void Log(string logMessage, TextWriter w)
    {
        w.WriteLine("{0}, {1}", CurrentTime, logMessage);
    }
}
