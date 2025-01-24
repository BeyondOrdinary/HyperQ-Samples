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

        private const int TERRAIN_WIDTH = 65;
        private const int TERRAIN_MIDDLE = 32; // index into moon surface
        private const int LZ_INDEX = 1000;
        private char[,] _MoonSurface = null;

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
            MakeMoonSurface();
        }

        private void MakeMoonSurface()
        {
            _MoonSurface = new char[3, 5897]; // moon is 5897 nm in circumference
            int next_alien = 0;
            int d_since_terrain = 0;
            int len = _MoonSurface.GetLength(1);
            for (int i = 0; i < len; i++)
            {
                _MoonSurface[0, i] = ' ';
                _MoonSurface[1, i] = ' ';
                _MoonSurface[2, i] = '~';
                if (_ran.Next(100) % 3 == 0)
                    _MoonSurface[2, i] = 'o';
            }
            for (int i = 0; i < len; i++)
            {
                d_since_terrain++;
                if (d_since_terrain > 45 && (i < LZ_INDEX - 3 || i > LZ_INDEX + 3))
                {
                    int r = _ran.Next(100);
                    if (r < 2 && next_alien < 3)
                    {
                        d_since_terrain = 0;
                        int i1 = i;
                        switch (next_alien)
                        {
                            case 0:
                                //  _--_
                                // (====)
                                //   /\
                                i1 = i - 3;
                                if (i1 < 0)
                                    i1 += len;
                                _MoonSurface[1, i1] = '(';
                                i1 = (i1 + 1) % len;
                                _MoonSurface[0, i1] = '_';
                                _MoonSurface[1, i1] = '=';
                                i1 = (i1 + 1) % len;
                                _MoonSurface[0, i1] = '-';
                                _MoonSurface[1, i1] = '=';
                                _MoonSurface[2, i1] = '/';
                                i1 = (i1 + 1) % len;
                                _MoonSurface[0, i1] = '-';
                                _MoonSurface[1, i1] = '=';
                                _MoonSurface[2, i1] = '\\';
                                i1 = (i1 + 1) % len;
                                _MoonSurface[0, i1] = '_';
                                _MoonSurface[1, i1] = '=';
                                i1 = (i1 + 1) % len;
                                _MoonSurface[1, i1] = ')';
                                i1 = (i1 + 1) % len;
                                i += 3;
                                break;
                            case 1:
                                // .-^-.
                                // (UFO)
                                // '---'
                                i1 = i - 2;
                                if (i1 < 0)
                                    i1 += len;
                                _MoonSurface[0, i1] = '.';
                                _MoonSurface[1, i1] = '(';
                                _MoonSurface[2, i1] = '\'';
                                i1 = (i1 + 1) % len;
                                _MoonSurface[0, i1] = '-';
                                _MoonSurface[1, i1] = 'U';
                                _MoonSurface[2, i1] = '-';
                                i1 = (i1 + 1) % len;
                                _MoonSurface[0, i1] = '^';
                                _MoonSurface[1, i1] = 'F';
                                _MoonSurface[2, i1] = '-';
                                i1 = (i1 + 1) % len;
                                _MoonSurface[0, i1] = '-';
                                _MoonSurface[1, i1] = 'O';
                                _MoonSurface[2, i1] = '-';
                                i1 = (i1 + 1) % len;
                                _MoonSurface[0, i1] = '.';
                                _MoonSurface[1, i1] = ')';
                                _MoonSurface[2, i1] = '\'';
                                i += 2;
                                break;
                            case 2:
                                //  __ 
                                // (==)
                                // /__\
                                i1 = i - 2;
                                if (i1 < 0)
                                    i1 += len;
                                _MoonSurface[1, i1] = '(';
                                _MoonSurface[2, i1] = '/';
                                i1 = (i1 + 1) % len;
                                _MoonSurface[0, i1] = '-';
                                _MoonSurface[1, i1] = '=';
                                _MoonSurface[2, i1] = '_';
                                i1 = (i1 + 1) % len;
                                _MoonSurface[0, i1] = '-';
                                _MoonSurface[1, i1] = '=';
                                _MoonSurface[2, i1] = '_';
                                i1 = (i1 + 1) % len;
                                _MoonSurface[1, i1] = ')';
                                _MoonSurface[2, i1] = '\\';
                                i += 2;
                                break;
                        }
                        next_alien++;
                    }
                    else if (r < 10)
                    {
                        d_since_terrain = 0;
                        // Big structure
                        //   ____
                        //  / oo \
                        // ~~~~~~~~
                        int i1 = i - 3;
                        if (i1 < 0)
                            i1 += len;
                        _MoonSurface[1, i1] = '/';
                        i1 = (i1 + 1) % len;
                        _MoonSurface[0, i1] = '_';
                        i1 = (i1 + 1) % len;
                        _MoonSurface[0, i1] = '_';
                        i1 = (i1 + 1) % len;
                        _MoonSurface[0, i1] = '_';
                        i1 = (i1 + 1) % len;
                        _MoonSurface[1, i1] = '\\';
                        i += 3;
                    }
                    else if (r < 35)
                    {
                        d_since_terrain = 0;
                        // little structure
                        //
                        // /-\
                        // ~~~
                        int i1 = i - 1;
                        if (i1 < 0)
                            i1 += _MoonSurface.GetLength(1);
                        _MoonSurface[1, i1] = '/';
                        i1 = (i1 + 1) % _MoonSurface.GetLength(1);
                        _MoonSurface[1, i1] = '-';
                        i1 = (i1 + 1) % _MoonSurface.GetLength(1);
                        _MoonSurface[1, i1] = '\\';
                        i += 1;
                    }
                }
            }
            // Start is X from LZ, so make a spot for the LZ (a little flag)
            _MoonSurface[0, LZ_INDEX] = '^';
            _MoonSurface[0, LZ_INDEX + 1] = '-';
            _MoonSurface[1, LZ_INDEX] = '|';
            /*
            Console.WriteLine("==+ MOON TERRAIN +==");
            for (int j = 0; j < _MoonSurface.GetLength(0); j++)
            {
                for (int i = 0; i < _MoonSurface.GetLength(1); i++)
                {
                    Console.Write(_MoonSurface[j, i]);
                }
                Console.WriteLine();
            }
            */
        }

        public void RenderGUI()
        {
            if (_LEM.LastBurn == null)
                return;
            double burn = _LEM.LastBurn.Power;
            // Compute the number of bars
            int bars3 = 0;
            while (burn > 50.0)
            {
                bars3++;
                burn = burn - 15.0;
            }
            int bars1 = 0;
            while (burn > 0)
            {
                bars1++;
                burn = burn - 15.0;
            }
            /*
               +-----+
               + LEM +
               +-----+
              /  ***  \
                 ***
                 *** bars3
                  *
                  *
                  * bars1
             */
            int max_lines = 30;
            int lem_lines = 4 + bars3 + bars1 - 1;
            max_lines -= lem_lines;
            double from_max_pct = 1 - (_LEM.AltitudeInMiles * 5280.0 + _LEM.AltitudeRemainderInFeet) / (60.0 * 5280.0); // Start at 60
            if (from_max_pct > 1.0)
                from_max_pct = 1.0;
            if (from_max_pct < 0.0)
                from_max_pct = 0.0;
            int top_lines = (int)Math.Truncate(max_lines * from_max_pct);
            // 30 lines for the 120 miles of altitude
            Console.WriteLine(">>frame");
            // Moon circumference is 5897 nautical miles
            // LZ is at index 1000 in the terrain
            // Terrain center is 
            int terrainLocationIndex = (int)Math.Floor(1000.0 + _LEM.DistanceToLZ);
            while (terrainLocationIndex < 0)
                terrainLocationIndex += _MoonSurface.GetLength(1);
            while (terrainLocationIndex >= _MoonSurface.GetLength(1))
                terrainLocationIndex -= _MoonSurface.GetLength(1);
            Console.Write("|");
            Console.Write(new string('~', TERRAIN_WIDTH));
            Console.WriteLine();
            for (int i = 0; i < top_lines; i++)
            {
                Console.WriteLine("|");
                max_lines--;
            }
            bool left_wind = _LEM.HorizontalSpeedInMiles > 0.0;
            bool right_wind = _LEM.HorizontalSpeedInMiles < 0.0;

            int lem_width = 7;
            string leftPad = new string(' ', 28 - lem_width / 2 - 3 * (left_wind ? 1 : 0));
            Console.Write($"|{leftPad}");
            if (left_wind)
            {
                Console.Write("  ~");
            }
            Console.Write("+-----+");
            if (right_wind)
                Console.Write("~");
            Console.WriteLine();

            Console.Write($"|{leftPad}");
            if (left_wind)
            {
                Console.Write("~~~");
            }
            Console.Write("+ LEM +");
            if (right_wind)
                Console.Write("~~~");
            Console.WriteLine();

            Console.Write($"|{leftPad}");
            if (left_wind)
            {
                Console.Write("  ~");
            }
            Console.Write("+-----+");
            if (right_wind)
                Console.Write("~");
            Console.WriteLine();

            leftPad = new string(' ', 28 - lem_width / 2);
            Console.Write($"|{leftPad}/ ");
            if (bars3 > 0)
            {
                Console.Write("***");
                bars3--;
            }
            else
            {
                if (bars1 > 0)
                {
                    Console.Write(" * ");
                    bars1--;
                }
                else
                {
                    Console.Write("   ");
                }
            }
            Console.WriteLine(" \\ ");
            while (bars3 > 0)
            {
                Console.WriteLine($"|{leftPad}  ***  ");
                bars3--;
            }
            while (bars1 > 0)
            {
                Console.WriteLine($"|{leftPad}   *   ");
                bars1--;
            }
            for (int i = 0; i < max_lines; i++)
            {
                Console.WriteLine("|");
            }
            Console.WriteLine("|");
            int moon_length = _MoonSurface.GetLength(1);
            int final_rows = 0;
            for (int j = 0; j < _MoonSurface.GetLength(0); j++)
            {
                Console.Write("|");
                for (int k = 0; k < TERRAIN_WIDTH; k++)
                {
                    int mlx = terrainLocationIndex + k - TERRAIN_MIDDLE;
                    while (mlx < 0)
                    {
                        mlx += moon_length;
                    }
                    while (mlx >= moon_length)
                        mlx = mlx - moon_length;
                    Console.Write(_MoonSurface[j, mlx]);
                    if (mlx > 1000 && final_rows < 4)
                        final_rows = 4;
                    else if (mlx > 100 && final_rows < 3)
                        final_rows = 3;
                    else if (mlx > 10 && final_rows < 2)
                        final_rows = 2;
                    else
                        final_rows = 1;
                }
                Console.WriteLine();
            }
            /*
            for (int j = 0; j < final_rows; j++)
            {
                Console.Write("|");
                for (int k = 0; k < TERRAIN_WIDTH; k++)
                {
                    int mlx = terrainLocationIndex + k - TERRAIN_MIDDLE;
                    while (mlx < 0)
                    {
                        mlx += moon_length;
                    }
                    while (mlx >= moon_length)
                        mlx = mlx - moon_length;
                    string sx = $"{mlx}";
                    if (j < sx.Length)
                        Console.Write(sx[j]);
                }
                Console.WriteLine();
            }
            */
            // 24 columns for the graph
            Console.Write("|ALTITUDE: [");
            int ratio = (int)Math.Truncate(_LEM.PercentAltitudeFromStart * 24);
            int c = 0;
            if (ratio < 0)
            {
                ratio = 24 + ratio;
                if (ratio > 24)
                {
                    ratio = 24;
                }
                for (int i = 24 - ratio; i > 0; i--)
                {
                    Console.Write(" ");
                    c++;
                }
            }
            else if (ratio > 24)
            {
                ratio = 24;
            }
            for (int i = 0; i < ratio; i++)
            {
                Console.Write("*");
                c++;
            }
            while (c < 24)
            {
                Console.Write(" ");
                c++;
            }
            Console.WriteLine("] {0:F2} Mi ({1:F2}%)", _LEM.AltitudeInMiles, _LEM.PercentAltitudeFromStart * 100.0);
            Console.Write("|    FUEL: [");
            c = 0;
            ratio = (int)Math.Truncate(_LEM.PercentFuelFromStart * 24);
            if (ratio < 0)
            {
                ratio = 0;
            }
            else if (ratio > 24)
            {
                ratio = 24;
            }
            for (int i = 0; i < ratio; i++)
            {
                Console.Write("*");
                c++;
            }
            while (c < 24)
            {
                Console.Write(" ");
                c++;
            }
            Console.WriteLine("] {0:F2} Lbs", _LEM.FuelLevel);
            Console.Write("| v SPEED: [");
            c = 0;
            ratio = (int)Math.Truncate(_LEM.PercentVerticalSpeedFromStart * 24);
            if (ratio < 0)
            {
                ratio = 0;
            }
            else if (ratio > 24)
            {
                ratio = 24;
            }
            for (int i = 0; i < ratio; i++)
            {
                Console.Write("*");
                c++;
            }
            while (c < 24)
            {
                Console.Write(" ");
                c++;
            }
            Console.WriteLine("] {0:F2} MPH", _LEM.VerticalSpeedInMiles);
            Console.Write("| h SPEED: [");
            c = 0;
            ratio = (int)Math.Truncate(_LEM.PercentHorizontalSpeedFromStart * 24);
            if (ratio < 0)
            {
                ratio = 0;
            }
            else if (ratio > 24)
            {
                ratio = 24;
            }
            for (int i = 0; i < ratio; i++)
            {
                Console.Write("*");
                c++;
            }
            while (c < 24)
            {
                Console.Write(" ");
                c++;
            }
            Console.WriteLine("] {0:F2} MPH", _LEM.HorizontalSpeedInMiles);
            Console.Write("|   TO LZ: [");
            c = 0;
            ratio = (int)Math.Truncate(_LEM.PercentToLZ * 24);
            if (ratio < 0)
            {
                ratio = 0;
            }
            else if (ratio > 24)
            {
                ratio = 24;
            }
            for (int i = 0; i < ratio; i++)
            {
                Console.Write("*");
                c++;
            }
            while (c < 24)
            {
                Console.Write(" ");
                c++;
            }
            Console.WriteLine("] {0:F2} Mi", _LEM.DistanceToLZInMiles);
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