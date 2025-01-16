using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Simulation.LunarLander
{
    /// <summary>
    /// The control parameters for the LEM. Parameters are (Power, Rotation, Time)
    /// where Power is the percentage of thrust from 0 to 100, Rotation is the attitude
    /// of the approach, and Time is the amount of time to apply the thrust.
    /// </summary>
    public class Rocket2Control : Tuple<double, double, double>
    {
        public double Time { get { return base.Item3; } }
        public double Power { get { return base.Item1; } }
        public double Rotation { get { return base.Item2; } }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="t">Time in seconds</param>
        /// <param name="p">Power in whole percent</param>
        /// <param name="a">Rotation in degrees</param>
        public Rocket2Control(double t, double p, double a) : base(p, a, t)
        {
            S = Math.Sin(P);
            C = Math.Cos(P);
            N = 20;
            /*
                620 N=20
                625 IF T1<400 THEN 635
                630 N=T1/20
                635 T1=T1/N
             */
            if (Time < 400.0)
            {
                T1 = Time / 20.0;
            }
            else
            {
                N = (int)Math.Truncate(Time / 20.0);
                T1 = Time / N;
            }
        }

        internal double T1 { get; private set; }
        internal int N { get; private set; }
        internal double P { get { return Rotation * Math.PI / 180.0; } }
        internal double S { get; private set; } = 0.0;
        internal double C { get; private set; } = 0.0;
        /// <summary>
        /// Internal - used as the fractional force [0,1]
        /// </summary>
        internal double F { get { return Power / 100.0; } }
    }

    public class Rocket2LEM
    {
        public const double F1 = 5.25;
        public const double Z_Metric = 1852; // Meters in a nautical mile
        public const double Z_English = 6076.12; // Feet in a nautical mile
        public const double G_Metric = 3.6;
        public const double G_English = 0.592;

        public double FuelLevel { get; private set; } = 0.0;

        public double AltitudeInMiles { get; private set; } = 0.0;
        public double AltitudeInFeet { get { return Math.Truncate(AltitudeInMiles * Z_English); } }
        public double AltitudeRemainderInFeet { get { return Math.Truncate((AltitudeInMiles - Math.Truncate(AltitudeInMiles)) * Z_English); } }
        public double AltitudeInMeters { get { return Math.Truncate(AltitudeInMiles * Z_Metric); } }

        public double VerticalSpeed { get; private set; } = 0.0;
        public double VerticalSpeedInMiles { get { return VerticalSpeed / Z_English; } }
        public double HorizontalSpeed { get; private set; } = 0.0;
        public double HorizontalSpeedInMiles { get { return HorizontalSpeed / Z_English; } }
        public double DistanceToLZ { get; private set; } = 0.0;
        public double DistanceToLZInMiles { get { return DistanceToLZ / Z_English; } }
        public double DistanceToLZRemainderFeet { get { return Math.Abs(Math.Truncate((DistanceToLZInMiles - Math.Truncate(DistanceToLZInMiles)) * Z_English)); } }

        private double R1 = 0.0;
        private double A = 0.0;
        private double A1 = 0.0;
        private double M = 0.0;
        private double H0 = 60.0;
        private double R = 0.0;
        private double B = 750.0;
        private double R0 = 926;
        private double V0 = 1.29;
        private double M1 = 0.0;
        private double M0 = 0.0;

        private const double ZeroEpsilon = 1e-5;

        //
        // Initial Conditions
        //   Altitude = 6080 Feet, or 1852.8 Meters
        public Rocket2LEM(double a = 60.0, double max_fuel = 750.0)
        {
            Reset(a,max_fuel);
        }

        public bool OutOfFuel
        {
            get
            {
                return (FuelLevel < ZeroEpsilon);
            }
        }

        public bool AtGroundLevel
        {
            get
            {
                return (AltitudeInMiles < 3.287828e-4);
            }
        }

        public bool DidCrash
        {
            get
            {
                double hv = Math.Abs(R * A1);
                return (R1 < -8.21957e-4 || hv > 4.931742e-4 || H0 < -3.287828e-4);
            }
        }

        public double CrashSpeed
        {
            get
            {
                return (Math.Sqrt(HorizontalSpeed * HorizontalSpeed + VerticalSpeed * VerticalSpeed) * G_English);
            }
        }

        public double CrashDepth
        {
            get
            {
                return 0.277 * CrashSpeed;
            }
        }

        public void Reset(double a=60.0,double max_fuel=750.0)
        {
            M1 = 7.45;
            M0 = M1;
            AltitudeInMiles = a;
            R1 = 0.0;
            A = -3.425;
            A1 = 8.84361e-4;
            M = 17.95;
            R0 = 926;
            H0 = a;
            B = max_fuel;
            R = R0 + H0;
            FuelLevel = B;
            VerticalSpeed = R1*Z_English;
            HorizontalSpeed = A1 * R * Z_English;
            DistanceToLZ = R0 * A * Z_English;
        }

        public bool OutOfOrbit
        {
            get
            {
                return (R0 * A > 164.4736);
            }
        }

        public double ApplyControl(Rocket2Control ctrl)
        {
            double F = ctrl.F;
            if (F > 1.0)
            {
                F = 1.0;
            }
            if (F < 0.05 && F > 0.0)
            {
                F = 0.05;
            }
            double M2 = M0 * ctrl.T1 * F / B;
            double R3 = -0.5 * R0 * ((V0 / R) * (V0 / R)) + R * A1 * A1;
            double A3 = -2.0 * R1 * A1 / R;
            double T = 0.0;
            // Time step loop
            for (int i = 0; i < ctrl.N; i++)
            {
                if (M1 < ZeroEpsilon)
                {
                    F = 0.0;
                    M2 = 0.0;
                }
                else
                {
                    M1 -= M2;
                    if (M1 < ZeroEpsilon)
                    {
                        F = F * (1.0 + M1 / M2);
                        M2 += M1; // 695
                        M1 = 0.0;
                    }
                }
                double t1sqrd = ctrl.T1 * ctrl.T1;
                M -= 0.5 * M2; // Line 725
                double R4 = R3;
                R3 = -0.5 * R0 * ((V0 / R) * (V0 / R)) + R * A1 * A1;
                double R2 = (3.0 * R3 - R4) / 2.0 + 0.00526 * F1 * F * ctrl.C / M; // Line 740
                double A4 = A3;
                A3 = -2.0 * R1 * A1 / R;
                double A2 = (3.0 * A3 - A4) / 2.0 + 0.0056 * F1 * F * ctrl.S / (M * R); // Line 755
                double X = R1 * ctrl.T1 + 0.5 * R2 * t1sqrd;
                R += X;
                H0 += X;
                R1 += R2 * ctrl.T1;
                A += A1 * ctrl.T1 + 0.5 * A2 * t1sqrd;
                A1 += A2 * ctrl.T1; // Line 785
                M -= 0.5 * M2;
                T += ctrl.T1;
                if (H0 < 3.287828e-4)
                {
                    // Line 800
                    // hit the ground, break out of the step loop
                    break;
                }
            }
            // Update
            AltitudeInMiles = H0;
            FuelLevel = M1 * B / M0; // Line 830
            VerticalSpeed = R1 * Z_English;
            HorizontalSpeed = R * A1 * Z_English;
            DistanceToLZ = R0 * A * Z_English;
            return (T);
        }
    }
}
