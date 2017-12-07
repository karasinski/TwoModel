using MathNet.Numerics.LinearAlgebra;
using System;
using System.IO;
using UnityEngine;


public class Controller : MonoBehaviour
{
    private StreamWriter Writer;

    public GameObject Cursor, Target;

    private String Filename;
    private bool RunningTrial;
    private float x, y, StartTime;
    private Vector3 Position;

    public int TotalTrials, CurrentTrial, LengthOfTrial, SystemOrder;
    public double DisturbanceGain, InputGain;
    private Tracking Tracker1;

    public class Tracking
    {
        public int SystemOrder;
        public Matrix<double> A, B, C, D, y, x, x0, u, u0, InputGainMatrix;
        public double Disturbance, DisturbanceGain, InputGain;

        public Tracking(int SystemOrder, double DisturbanceGain, double InputGain)
        {
            this.SystemOrder = SystemOrder;
            this.DisturbanceGain = DisturbanceGain;
            this.InputGain = InputGain;
            Initialize();
        }

        private void Initialize()
        {

            switch (SystemOrder)
            {
                case 0:
                    A = Matrix<double>.Build.DenseOfArray(new double[,] { { 0 } });
                    B = Matrix<double>.Build.DenseOfArray(new double[,] { { 0 } });
                    C = Matrix<double>.Build.DenseOfArray(new double[,] { { 0 } });
                    D = Matrix<double>.Build.DenseOfArray(new double[,] { { 5 } });
                    x = Matrix<double>.Build.DenseOfArray(new double[,] { { 0 } });
                    break;
                case 1:
                    A = Matrix<double>.Build.DenseOfArray(new double[,] { { -0 } });
                    B = Matrix<double>.Build.DenseOfArray(new double[,] { { 1 } });
                    C = Matrix<double>.Build.DenseOfArray(new double[,] { { 5 } });
                    D = Matrix<double>.Build.DenseOfArray(new double[,] { { 1 } });
                    x = Matrix<double>.Build.DenseOfArray(new double[,] { { 0 } });
                    break;
                case 2:
                    A = Matrix<double>.Build.DenseOfArray(new double[,] {
                        {-0, -0 },
                        { 1, 0 }
                    });

                    B = Matrix<double>.Build.DenseOfArray(new double[,] {
                        { 1 },
                        { 0 }
                    });

                    C = Matrix<double>.Build.DenseOfArray(new double[,] {
                        { 0, 5 }
                    });

                    D = Matrix<double>.Build.DenseOfArray(new double[,] { { 0 } });

                    x = Matrix<double>.Build.DenseOfArray(new double[,] {
                        { 0 },
                        { 0 }
                    });
                    break;
            };

            y = Matrix<double>.Build.DenseOfArray(new double[,] { { 0 } });
            InputGainMatrix = Matrix<double>.Build.DenseOfArray(new double[,] { { InputGain } });
            u = Matrix<double>.Build.DenseOfArray(new double[,] { { 0 } });

            // Set initial values for later
            x0 = x;
            u0 = u;
        }

        public void UpdateDynamics(float CurrentTime)
        {
            Disturbance = DisturbanceGain * generateDisturbance(CurrentTime);

            u = InputGainMatrix * Input.GetAxis("Vertical");

            x += (A * x + B * u) * Time.deltaTime;
            y = (C * x + D * u) + Disturbance;
        }

        private double generateDisturbance(float t)
        {

            double[] a = { 1, 1, 1, 1, 1, 1,
                       0.1, 0.1, 0.1, 0.1, 0.1, 0.1};
            double[] w = { 0.18850, 0.31416, 0.50265, 0.87965, 1.44513, 2.13628,
                       3.07876, 4.20973, 5.78053, 8.23097, 11.24690, 15.77079, 23.93894 };

            double phi1 = Math.PI / 6.5;
            double[] phi = { phi1, 2 * phi1, 3 * phi1, 4 * phi1, 5 * phi1, 6 * phi1,
                         7 * phi1, 8 * phi1, 9 * phi1, 10 * phi1, 11 * phi1, 12 * phi1, 13 * phi1 };

            double track = 0;
            for (int i = 0; i < a.Length; i++)
            {
                track += a[i] * Math.Sin(w[i] * t + phi[i]);
            }

            return track;
        }

        public float Output() {
            return (float)y[0, 0];
        }

        public void Reset() {
            x = x0;
            u = u0;
        }

        public string Logger(float CurrentTime)
        {
            return (String.Format("({0}, {1}, {2}, {3})", u, y, Disturbance, x));
        }
    }

    void Start()
    {
        // Initialize a 1D tracker
        Tracker1 = new Tracking(SystemOrder, DisturbanceGain, InputGain);
        var test = Tracker1.x.ToString();
        print(test);
        test = Tracker1.x.ToTypeString();
        print(test);
        test = Tracker1.Logger(5);
        print(test);
    }

    void Update()
    {
        if (RunningTrial)
        {
            RunTrial();
        }
        else if (Input.GetKeyDown("space"))
        {
            ClickStartTrial();
        }
    }

    private void FixedUpdate()
    {
        if (RunningTrial)
        {
            float CurrentTime = Time.time - StartTime;
            var r = Tracker1.Logger(CurrentTime);
            Log(r, Writer);
        }
    }

    void RunTrial()
    {
        // Note that the negative sign is purely conventional
        x = 0;
        y = Tracker1.Output();
        Position = new Vector3(x, y, 0);
        Cursor.transform.localPosition = Position;

        // Add in the disturbance
        float CurrentTime = Time.time - StartTime;

        //tracker update
        Tracker1.UpdateDynamics(CurrentTime);

        if (CurrentTime > LengthOfTrial)
        {
            EndTrial();
        }
    }

    void ClickStartTrial()
    {
        // Output data to file
        Filename = String.Format("logs/trial_{0}_log_{1}.csv", CurrentTrial, DateTime.UtcNow.ToLocalTime().ToString("yyy_MM_dd_hh_mm_ss"));
        Writer = File.AppendText(Filename);

        // Set start time
        RunningTrial = true;
        StartTime = Time.time;
    }

    void EndTrial()
    {
        Writer.Close();

        CurrentTrial += 1;
        if (CurrentTrial > TotalTrials)
        {
            // Exit experiment
        }
        else
        {
            // Something to begin next trial
        }

        RunningTrial = false;

        Tracker1.Reset();
    }

    void Log(string logMessage, TextWriter w)
    {
        var time = Time.time - StartTime;
        w.WriteLine("{0}, {1}", time, logMessage);
    }
}
