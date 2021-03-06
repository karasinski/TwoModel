﻿using System;
using MathNet.Numerics.LinearAlgebra;
using UnityEngine;

public class Tracking
{
    private int SystemOrder;
    private Matrix<double> A, B, C, D, y, x, x0, u, u0, InputGainMatrix;
    private double K, Disturbance, DisturbanceGain, InputGain;
    private string Axis;

    public Tracking(string Axis, double K, int SystemOrder, double DisturbanceGain, double InputGain)
    {
        this.K = K;
        this.SystemOrder = SystemOrder;
        this.DisturbanceGain = DisturbanceGain;
        this.InputGain = InputGain;
        this.Axis = Axis;
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
                D = Matrix<double>.Build.DenseOfArray(new double[,] { { K } });
                x = Matrix<double>.Build.DenseOfArray(new double[,] { { 0 } });
                break;
            case 1:
                A = Matrix<double>.Build.DenseOfArray(new double[,] { { 0 } });
                B = Matrix<double>.Build.DenseOfArray(new double[,] { { 1 } });
                C = Matrix<double>.Build.DenseOfArray(new double[,] { { K } });
                D = Matrix<double>.Build.DenseOfArray(new double[,] { { 0 } });
                x = Matrix<double>.Build.DenseOfArray(new double[,] { { 0 } });
                break;
            case 2:
                A = Matrix<double>.Build.DenseOfArray(new double[,] { { 0, 0 },
                                                                      { 1, 0 } });
                B = Matrix<double>.Build.DenseOfArray(new double[,] { { 1 },
                                                                      { 0 } });
                C = Matrix<double>.Build.DenseOfArray(new double[,] { { 0, K } });
                D = Matrix<double>.Build.DenseOfArray(new double[,] { { 0 } });
                x = Matrix<double>.Build.DenseOfArray(new double[,] { { 0 },
                                                                      { 0 } });
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

        u = InputGainMatrix * Input.GetAxis(Axis);

        x += (A * x + B * u) * Time.deltaTime;
        y = (C * x + D * u) + Disturbance;
    }

    private double generateDisturbance(float t)
    {

        double[] a = { 1, 1, 1, 1, 1, 1,
                       0.1, 0.1, 0.1, 0.1, 0.1, 0.1 };
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

    public float Output()
    {
        return (float)y[0, 0];
    }

    public void Reset()
    {
        x = x0;
        u = u0;
    }

    private string Stringify(Matrix<double> input)
    {
        var str = "";
        foreach (double element in input.ToArray())
        {
            str += element.ToString() + ", ";
        }
        return str;
    }

    public string Logger()
    {
        var ustr = Stringify(u);
        var ystr = Stringify(y);
        var xstr = Stringify(x);

        return String.Format("{0}{1}{2}, {3}", ustr, ystr, Disturbance, xstr);
    }
}
