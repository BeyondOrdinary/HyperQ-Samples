using HyperQ.Env;
using HyperQ.Learners;
using HyperQ.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace Simulation.LunarLander
{
    public enum LanderStatus
    {
        Landing, // In the process of landing
        Crashed, // Smash
        Landed, // Successful
        FreeFall // Out of fuel
    }
    public class Rocket2Report : Tuple<double, double, double, double, double, double, LanderStatus>
    {
        public double AltitudeInMiles { get { return Item1; } }
        public double DistanceToLZInMiles { get { return Item2; } }
        public double VerticalSpeed { get { return Item3; } }
        public double HorizontalSpeed { get { return Item4; } }
        public double ElapsedTime { get { return Item5; } }
        public double FuelLevel { get { return Item6; } }
        public LanderStatus Status { get { return Item7; } }
        public Rocket2Report(Rocket2LEM lem, double time, LanderStatus status) : base(lem.AltitudeInMiles, lem.DistanceToLZInMiles, lem.VerticalSpeed, lem.HorizontalSpeed, time, lem.FuelLevel, status) {
        }
    }
    public class Rocket2BaseGameEnv
    {
        public const double Z_Metric = 1852; // Meters in a nautical mile
        public const double Z_English = 6076.12; // Feet in a nautical mile
        public const double G_Metric = 3.6;
        public const double G_English = 0.592;

        protected const int DEFAULT_ACTION_PRECISION = 10;
        private double _ElapsedTime;

        private Random _ran = null;

        public bool Quiet { get; set; } = false;
        /// <summary>
        /// Max number of steps allowed, -1 if no limit. When zero the simulation will end with a crash status.
        /// </summary>
        public int StepsAllowed { get; set; } = -1;

        public LanderStatus Status { get; private set; }

        protected Rocket2LEM _LEM = null;

        public int[] NumActions
        {
            get
            {
                // action[0] = time in 1,2,3...,N seconds
                // action[1] = power in 1,2,3,...,100 percent increments
                // action[3] = rotation in degrees, 0, 1, ... 360 increments,
                return new int[] { 1000, 100, 360 };
            }
        }
        public EnvMetrics Metrics { get; set; }
        public double LastScore { get; private set; } = 0.0;
        private double _PriorRating = double.MinValue;

        public Rocket2BaseGameEnv(Random ran = null, bool quiet = false)
        {
            _ran = ran ?? new Random(0);
            _LEM = new Rocket2LEM();
            Quiet = quiet;
            _ElapsedTime = 0;
        }

        public void Render()
        {
            if (Quiet)
                return;
            if (_ElapsedTime < 1e-5)
            {
                Console.WriteLine("  YOU ARE ON A LUNAR LANDING MISSION. AS THE PILOT OF");
                Console.WriteLine("THE LUNAR EXCUSION MODULE, YOU WILL BE EXPECTED TO");
                Console.WriteLine("GIVE CERTAIN COMMANDS TO THE MODULE NAVIGATION SYSTEM.");
                Console.WriteLine("THE ON-BOARD COMPUTER WILL GIVE A RUNNING ACCOUNT");
                Console.WriteLine("OF INFORMATION NEEDED TO NAVIGATE THE SHIP.");
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine("THE ATTITUDE ANGLE CALLED FOR IS DESCRIBED AS FOLLOWS.");
                Console.WriteLine("+ OR -180 DEGREES IS DIRECTLY AWAY FROM THE MOON");
                Console.WriteLine("-90 DEGREES IS ON A TANGENT IN THE DIRECTION OF ORBIT");
                Console.WriteLine("+90 DEGREES IS ON A TANGENT FROM THE DIRECTION OF ORBIT");
                Console.WriteLine("0 (ZERO) DEGREES IS DIRECTLY TOWARDS THE MOON.");
                Console.WriteLine();
                Console.WriteLine("{0,30}-180,180", " ");
                Console.WriteLine("{0,34}*", " ");
                Console.WriteLine("{0,27}-90 < -+- > 90", " ");
                Console.WriteLine("{0,34}|", " ");
                Console.WriteLine("{0,34}0", " ");
                Console.WriteLine("{0,23}<< DIRECTION OF ORBIT <<", " ");
                Console.WriteLine();
                Console.WriteLine("{0,23}/\\___/\\_______/-----\\___", " ");
                Console.WriteLine("{0,25}SURFACE OF THE MOON", " ");
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine("ALL ANGLES BETWEEN -180 AND 180 DEGREES ARE ACCEPTED");
                Console.WriteLine();
                Console.WriteLine("1 FUEL UNIT = 1 SEC. AT MAX THRUST");
                Console.WriteLine("ANY DISCREPANCIES ARE ACCOUNTED FOR IN THE USE OF FUEL");
                Console.WriteLine("FOR AN ATTITUDE CHANGE.");
                Console.WriteLine("AVAILABLE ENGINE POWER: 0 (ZERO) AND ANY VALUE BETWEEN");
                Console.WriteLine("10 AND 100 PERCENT");
                Console.WriteLine();
                Console.WriteLine("NEGATIVE THRUST OR TIME IS PROHIBITED");
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine("INPUT: TIME INTERVAL IN SECONDS ------ (T)");
                Console.WriteLine("       PERCENTAGE OF THRUST ---------- (P)");
                Console.WriteLine("       ATTITUDE ANGLE IN DEGREES ----- (A)");
                Console.WriteLine();
                Console.WriteLine();
                //                 12345678...1234567890..1234567...12345678901234567.123456789012.1234567890123..12345678901..123456
                Console.WriteLine(" TIME(s) | Altitude (nm) + (ft) | To LZ (nm)+(ft) | Vert Speed | Horiz Speed | Fuel Units | Score");
            }
            Console.Write("{0,8:F0}", _ElapsedTime);
            // Altitude
            Console.Write("   {0,10}  {1,7}", Math.Truncate(_LEM.AltitudeInMiles), Math.Truncate(_LEM.AltitudeRemainderInFeet));
            // Distance from LZ
            Console.Write("   {0,10}{1,7}", Math.Truncate(_LEM.DistanceToLZInMiles), Math.Truncate(_LEM.DistanceToLZRemainderFeet));
            // Vz
            Console.Write(" {0,12:F2}", _LEM.VerticalSpeed);
            // Vh
            Console.Write(" {0,13:F2}", _LEM.HorizontalSpeed);
            // Fuel
            Console.Write("  {0,11:F1}", _LEM.FuelLevel);
            // Score
            Console.Write(" {0,6:F1}", LastScore);
            Console.WriteLine();
        }
        public virtual void Reset()
        {
            _LEM = new Rocket2LEM();
            Status = LanderStatus.Landing;
            _ElapsedTime = 0;
            _PriorRating = double.MinValue;
            SetRating(null);
            StepsAllowed = -1;
        }

        public Rocket2Report Report
        {
            get
            {
                return (new Rocket2Report(_LEM, _ElapsedTime, Status));
            }
        }

        public Rocket2Control ActionToControl(int[] action)
        {
            if (action.Length != 3)
            {
                throw (new Exception(string.Format("Expected 3 actions, only got {0}", action.Length)));
            }
            double t = (double)action[0];
            if (action[0] < 1)
            {
                t = 1.0;
            }
            double p = (double)action[1];
            if (action[1] < 0 || (action[1] < 10 && action[1] > 0))
            {
                p = 10.0;
            }
            // action[0] = time in 1,2,3...,N seconds
            // action[1] = power in 1,2,3,...,100 percent increments
            // action[3] = rotation in degrees, 0, 1, ... 360 increments,
            int angle = action[2];
            if (angle > 180)
            {
                angle = -(360 - angle);
            }
            Rocket2Control burn = new Rocket2Control(t, p, angle);
            return burn;
        }
        /*
            float shaping = -100f * np.sqrt(pos.X * pos.X + pos.Y * pos.Y);
            shaping += -100f * np.sqrt(vel.X * vel.X + vel.Y * vel.Y);
            shaping += -100f * Math.Abs((float)step.Information["angle"]);
            shaping += 10f * (_Lander.Legs[0].Contact ? 1f : 0f);
            shaping += 10f * (_Lander.Legs[1].Contact ? 1f : 0f);
            if (_PrevShaping != float.MinValue)
            {
                reward = shaping - _PrevShaping;
            }
            _PrevShaping = shaping;
            reward -= m_power * 0.3f;
            reward -= s_power * 0.03f;
         */

        private double SetRating(Rocket2Control burn)
        {
            double hz = _LEM.HorizontalSpeedInMiles;
            double vz = _LEM.VerticalSpeedInMiles;
            double rating = -1.0 * Math.Sqrt(hz*hz + vz*vz);
            if(burn != null) 
                rating += -1.0 * Math.Abs(burn.P);
            rating += -1.0 * Math.Sqrt(_LEM.DistanceToLZInMiles * _LEM.DistanceToLZInMiles + _LEM.AltitudeInMiles * _LEM.AltitudeInMiles);
            double diff = 0.0;
            if (_PriorRating > double.MinValue)
            {
                diff = rating - _PriorRating;
            }
            _PriorRating = rating;
            return diff;
        }

        /// <summary>
        /// A 3 action step in the simulation. Action[0] is the time in seconds, Action[1] is the power
        /// in percentage 1 .. 100, and Action[2] is the angle in degrees 0 .. 180
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public Tuple<double, bool> Step(int[] action)
        {
            double last_D = _LEM.DistanceToLZInMiles;
            double score = 0.0;
            Rocket2Control burn = ActionToControl(action);
            if (!Quiet)
                Console.WriteLine("PAT=: {0:F2}%,{1:F2} deg,{2:F2}s", burn.Power, burn.Rotation, burn.Time);
            _ElapsedTime += _LEM.ApplyControl(burn);
            score += SetRating(burn);
            score -= Math.Abs(burn.F * 0.3 * burn.C);
            score -= Math.Abs(burn.F * 0.03 * burn.S);
            if (_LEM.AtGroundLevel)
            {
                // Line 845
                if (_LEM.DidCrash)
                {
                    Status = LanderStatus.Crashed;
                    score -= _LEM.CrashDepth;
                    if (!Quiet)
                    {
                        Console.WriteLine("CRASH !!!!!!!!");
                        Console.WriteLine("YOUR IMPACT CRATER WAS {0:F2} FT DEEP AT {1:F2} FPS.", _LEM.CrashDepth, _LEM.CrashSpeed);
                    }
                }
                else
                {
                    Status = LanderStatus.Landed;
                    if (Math.Abs(_LEM.DistanceToLZ) > 10 * Z_English)
                    {
                        score = -Math.Abs(_LEM.DistanceToLZInMiles) / (10.0 * Z_English);
                        if (!Quiet)
                        {
                            Console.WriteLine("YOU ARE DOWN SAFELY, BUT YOU MISSED THE LZ BY {0:F2} nm", _LEM.DistanceToLZInMiles);
                        }
                    }
                    else
                    {
                        if (!Quiet)
                        {
                            Console.WriteLine("TRANQUILITY BASE HERE -- THE EAGLE HAS LANDED");
                        }
                        score += 100.0;
                    }
                }
            }
            else if (_LEM.OutOfFuel)
            {
                Status = LanderStatus.Crashed;
                score -= _LEM.CrashDepth;
                if (!Quiet)
                {
                    Console.WriteLine("CRASH !!!!!!!! - OUT OF FUEL");
                    Console.WriteLine("YOUR IMPACT CRATER WAS {0:F2} FT DEEP AT {1:F2} FPS.", _LEM.CrashDepth, _LEM.CrashSpeed);
                }
            }
            else if (_LEM.OutOfOrbit)
            {
                Status = LanderStatus.Crashed;
                if (!Quiet)
                {
                    Console.WriteLine("CRASH !!!!!!!! - OUT OF ORBIT");
                    Console.WriteLine("YOU ARE LOST IN SPACE");
                }
            }
            else if (StepsAllowed == 0)
            {
                Status = LanderStatus.Crashed;
                if (!Quiet)
                {
                    Console.WriteLine("CRASH !!!!!!!! - OUT OF TIME");
                    Console.WriteLine("YOU ARE LOST IN SPACE");
                }
            }
            else
            {
                //score = last_D - _LEM.DistanceToLZInMiles;
            }
            LastScore = score;
            StepsAllowed -= 1;
            return new Tuple<double, bool>(score, Status == LanderStatus.Landing ? false : true);
        }
    }
}