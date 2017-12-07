using System;
using System.IO;
using UnityEngine;

public class Controller : MonoBehaviour
{
    public GameObject Cursor, Target, Text;
    public int TotalTrials, CurrentTrial, LengthOfTrial, SystemOrder;
    public double K, DisturbanceGain, InputGain;

    private StreamWriter Writer;
    private String Filename;
    private bool RunningTrial;
    private float x, y, StartTime;
    private Vector3 Position;
    private Tracking[] Trackers = new Tracking[2];

    void Start()
    {
        // Initialize a 1D tracker
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
        Filename = String.Format("logs/trial_{0}_log_{1}.csv", CurrentTrial, DateTime.UtcNow.ToLocalTime().ToString("yyy_MM_dd_hh_mm_ss"));
        Writer = File.AppendText(Filename);

        // Set start time
        RunningTrial = true;
        StartTime = Time.time;
        Cursor.SetActive(true);
    }

    void RunTrial()
    {
        // Note that the negative sign is purely conventional
        x = Trackers[0].Output();
        y = Trackers[1].Output();

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

        Position = new Vector3(x, y, 0);
        Cursor.transform.localPosition = Position;

        // Add in the disturbance
        float CurrentTime = Time.time - StartTime;

        // Tracker update, yaxis is arbitrarily 60 seconds ahead
        Trackers[0].UpdateDynamics(CurrentTime);
        Trackers[1].UpdateDynamics(CurrentTime + 60);

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
        var time = Time.time - StartTime;
        w.WriteLine("{0}, {1}", time, logMessage);
    }
}
