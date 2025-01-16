using HyperQ.Env;
using HyperQ.Learners;
using HyperQ.Util;
using HyperQ.MACE.Training;
using HyperQ.MACE.Eval;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using HyperQ.MACE;

namespace HuntTheWumpus
{
    public class QQRunner : WumpusRunner<decimal>
    {
        public QQRunner(int numepisodes, HyperParams h = null) : base(numepisodes, h) { }
        protected override Q<decimal> CreateLearner(uint nActions)
        {
            return new MappedQQ<decimal>(nActions, QRandom.Instance.DefaultRandomAction, ran: _TheRandom);
        }
        protected override WumpusBaseGameEnv CreateWorld()
        {
            WumpusGameEnv world = new WumpusGameEnv(ran: _TheRandom, quiet: Quiet);
            world.Metrics = new EnvMetrics(noHistory: true);
            return (world);
        }
    }
    public class HyperQQRunner : WumpusRunner<QState<decimal>>
    {
        public HyperQQRunner(int numepisodes, HyperParams h = null) : base(numepisodes, h) { }
        protected override Q<QState<decimal>> CreateLearner(uint nActions)
        {
            return new DoubleHyperQ<decimal>(nActions, QRandom.Instance.DefaultRandomAction, ran: _TheRandom);
        }
        protected override WumpusBaseGameEnv CreateWorld()
        {
            WumpusHyperGameEnv world = new WumpusHyperGameEnv(ran: _TheRandom, quiet: Quiet);
            world.Metrics = new EnvMetrics(noHistory: true);
            return (world);
        }
    }
    public class QRunner : WumpusRunner<decimal>
    {
        public QRunner(int numepisodes, HyperParams h = null) : base(numepisodes, h) { }
        protected override Q<decimal> CreateLearner(uint nActions)
        {
            return new BasicMappedQ<decimal>(nActions, QRandom.Instance.DefaultRandomAction);
        }
        protected override WumpusBaseGameEnv CreateWorld()
        {
            WumpusGameEnv world = new WumpusGameEnv(ran: _TheRandom, quiet: Quiet);
            world.Metrics = new EnvMetrics(noHistory: false);
            return (world);
        }
    }
    public class HyperQRunner : WumpusRunner<QState<decimal>>
    {
        public HyperQRunner(int numepisodes, HyperParams h = null) : base(numepisodes, h) { }
        protected override Q<QState<decimal>> CreateLearner(uint nActions)
        {
            return new SingleHyperQ<decimal>(nActions, QRandom.Instance.DefaultRandomAction);
        }
        protected override WumpusBaseGameEnv CreateWorld()
        {
            WumpusHyperGameEnv world = new WumpusHyperGameEnv(ran: _TheRandom, quiet: Quiet);
            world.Metrics = new EnvMetrics(noHistory: false);
            return (world);
        }
    }
    public class LayeredHyperQRunner : WumpusRunner<QState<decimal>>
    {
        public LayeredHyperQRunner(int numepisodes, HyperParams h = null) : base(numepisodes, h) { }
        protected override Q<QState<decimal>> CreateLearner(uint nActions)
        {
            SingleQGenerator<decimal> g = new SingleQGenerator<decimal>(nActions, QRandom.Instance.DefaultRandomAction);
            return new LayeredHyperQ<decimal>(g.HyperQGenerator, QRandom.Instance.DefaultRandomAction);
        }
        protected override WumpusBaseGameEnv CreateWorld()
        {
            WumpusHyperGameEnv world = new WumpusHyperGameEnv(ran: _TheRandom, quiet: Quiet);
            world.Metrics = new EnvMetrics(noHistory: false);
            return (world);
        }
    }
    public class LayeredHyperQQRunner : WumpusRunner<QState<decimal>>
    {
        public LayeredHyperQQRunner(int numepisodes, HyperParams h = null) : base(numepisodes, h) { }
        protected override Q<QState<decimal>> CreateLearner(uint nActions)
        {
            DoubleQGenerator<decimal> g = new DoubleQGenerator<decimal>(nActions, QRandom.Instance.DefaultRandomAction, ran: _TheRandom);
            return new LayeredHyperQ<decimal>(g.HyperGenerator, QRandom.Instance.DefaultRandomAction);
        }
        protected override WumpusBaseGameEnv CreateWorld()
        {
            WumpusHyperGameEnv world = new WumpusHyperGameEnv(ran: _TheRandom, quiet: Quiet);
            world.Metrics = new EnvMetrics(noHistory: false);
            return (world);
        }
    }

    public interface IWumpusRunner
    {
        void Run(ProgramArgs args);
        void Save(string fname);
        void Load(string fname);
    }

    [Serializable]
    public class VerboseMaxSelector<T> : MaxActionSelector<T>
    {
        public bool Verbose { get; set; } = false;
        public VerboseMaxSelector(int numActions, Random ran) : base(numActions, ran)
        {
        }

        protected override int ProcessSelection(QAction t, uint maxNumActions)
        {
            if(Verbose)
                if(t != null)
                    Console.WriteLine("Process: {0} = {1}", t.Action, t.Reward);
            return base.ProcessSelection(t, maxNumActions);
        }
        public override int SelectAction(Q<T> q, T state, double epsilon)
        {
            if (Verbose)
            {
                double[] array = q.GetActionArray(state);
                Console.Write("(max) {0} : ", state);
                foreach (double d in array)
                {
                    Console.Write("{0},", d);
                }
                Console.Write(" [e={0}]", epsilon);
            }
            int action = base.SelectAction(q, state, epsilon);
            if (LastActionWasRandom)
            {
                Console.Write('*');
            }
            Console.WriteLine(" => a={0}", action);
            return (action);
        }
    }

    [Serializable]
    public class VerboseGreedySelector<T> : eGreedyActionSelector<T>
    {
        public bool Verbose { get; set; } = false;
        public VerboseGreedySelector(Random ran) : base(ran)
        {
        }

        public override int SelectAction(Q<T> q, T state, double epsilon)
        {
            if (Verbose)
            {
                double[] array = q.GetActionArray(state);
                Console.Write("(greedy) {0} : ", state);
                foreach (double d in array)
                {
                    Console.Write("{0},", d);
                }
                Console.Write(" [e={0}]", epsilon);
            }
            int action = base.SelectAction(q, state, epsilon);
            if (LastActionWasRandom)
            {
                Console.Write('*');
            }
            Console.WriteLine(" => a={0}", action);
            return action;
        }
    }

    [Serializable]
    public abstract class WumpusRunner<T> : IWumpusRunner
    {
        protected Random _TheRandom = null; // QRandom.Instance.Ran
        public int NumEpisodes { get; private set; }
        public WumpusBaseGameEnv World { get; private set; }
        protected MACEMind<T>[] _Q;
        public double StepDuration { get; set; } = 10.0;
        public bool Quiet { get; set; } = true;
        public HyperParams Hypers { get; set; } = new HyperParams(g: 0.997, e: .5, a: 0.7, edecay: 0.999991, adecay: 0.999991, min_epsilon: 0.05, min_alpha: 0.1, tau: 0.95);
        public MACEPvESARSATrainer<T> Model { get; private set; }

        public WumpusRunner(int numepisodes, HyperParams h = null)
        {
            NumEpisodes = numepisodes;
            QRandom.Instance.Seed(903387237);
            if (h != null)
            {
                Hypers = h;
            }
        }

        protected abstract Q<T> CreateLearner(uint nActions);
        protected abstract WumpusBaseGameEnv CreateWorld();

        private void EvaluateIt(bool quiet = false)
        {
            MACEEvaluator<T> eval = null;
            eval = new MACEEvaluator<T>(QRandom.Instance.Ran);
            foreach (MACEMind<T> q in _Q)
            {
                VerboseMaxSelector<T> maxs = new VerboseMaxSelector<T>((int)q.Mind.MaxNumActions, QRandom.Instance.Ran);
                eval.Add(q.Mind, maxs);
                maxs.Verbose = quiet;
            }
            World.Quiet = quiet;
            World.Reset();
            //
            // Evaluate
            //
            Console.WriteLine("==-==-==-==-==-==-==-==-==-==-==-==-==-==-==-==-==-==-==-==-==-==-==-==-==-==-==-==");
            HyperParams hpz = new HyperParams(g: 0.997, e: 0.0, a: 1, edecay: 1, adecay: 1, min_epsilon: 0.0, min_alpha: 1);
            eval.Episode((IMACEPvEEnv<T>)World, hpz);
            Console.WriteLine("Ending evaluation with total reward {0} in {1} states and {2} actions.", World.Metrics.TotalReward, World.Metrics.NumberOfStatesVisited, World.Metrics.NumberOfActionsTaken);
        }


        /// <summary>
        /// Training method where the Q learner is trained.
        /// </summary>
        /// <param name="args"></param>
        public void TrainIt(ProgramArgs args)
        {
            QEvalType evalType = QEvalType.OffPolicy;
            if (args.onpolicy)
                evalType = QEvalType.OnPolicy;
            TextWriter console = Console.Out;
            List<string> files = new List<string>();
            using (StreamWriter swout = new StreamWriter($"trainit-{NumEpisodes}x{args.epStep}-{evalType}.log"))
            {
                Console.SetOut(swout);
                using (StreamWriter swResult = new StreamWriter($"wumpus-result-{NumEpisodes}-final.csv"))
                {
                    swResult.WriteLine("deaths,escapes,totalreward,avgreward,epsilon,alpha,gold,food,wins");
                    Console.WriteLine("Training {0} episodes", NumEpisodes);
                    Console.WriteLine("  output step = {0}", args.epStep);
                    Console.WriteLine("  warmup = {0}", args.warmup_episodes);
                    Console.WriteLine("  memory = {0} {1}", args.memory_size, args.use_negpos_memory ? "neg/pos" : "all");
                    Console.WriteLine("  dyna = {0} @ {1:F3}", args.dyna_size, args.dyna_freq);
                    if (World == null)
                    {
                        World = CreateWorld();
                        World.RewardModel = args.model;
                        World.Dimensions = args.Dimensions;
                        World.Reset();
                        Console.WriteLine("Created the simulation world.");
                    }
                    int[] numActions = World.NumActions;
                    _Q = new MACEMind<T>[numActions.Length];
                    // Setup the MACE trainer with a default eGreedy selector
                    IActionSelector<T> selector = new eGreedyActionSelector<T>(QRandom.Instance.Ran);
                    MACEPvESARSATrainer<T> s = new MACEPvESARSATrainer<T>(evalType, selector, QRandom.Instance.Ran);
                    for (int i = 0; i < numActions.Length; i++)
                    {
                        Q<T> learner = CreateLearner((uint)numActions[i]);
                        // selector = new MostProbableMaxActionSelector<T>((int)learner.MaxNumActions, QRandom.Instance.Ran);
                        selector = new eGreedyActionSelector<T>(QRandom.Instance.Ran);
                        _Q[i] = new MACEMind<T>(learner, selector);
                        s.Add(_Q[i]);
                    }
                    IMACEPvEEnv<T> env = (IMACEPvEEnv<T>)World;
                    if (args.memory_size > 0)
                    {
                        QMemory<T> mem = null;
                        if (args.episodic)
                        {
                            if (args.use_negpos_memory)
                                mem = new QEpisodicNegPosMemory<T>(args.memory_size, QRandom.Instance.Ran);
                            else
                                mem = new QEpisodicMemory<T>(args.memory_size, QRandom.Instance.Ran);
                        }
                        else
                        {
                            if (args.use_negpos_memory)
                                mem = new QNegPosMemory<T>(args.memory_size, QRandom.Instance.Ran);
                            else
                                mem = new QMemory<T>(args.memory_size, QRandom.Instance.Ran);
                        }
                        s.EnableMemory(mem);
                    }
                    if (args.dyna_size > 0)
                        s.EnableDyna(new DynaState<T>(args.dyna_iters, args.dyna_size), args.dyna_freq);
                    HyperParams hp = new HyperParams(Hypers);
                    s.InTraining = true;
                    if (args.warmup_episodes > 0)
                        s.Warmup(env, hp, args.warmup_episodes);
                    RunningAverage avgReward = new RunningAverage();
                    RunningAverage avgEpisodeTime = new RunningAverage();
                    int exceptions = 0;
                    Dictionary<PlayerStatus, int> status_count = new Dictionary<PlayerStatus, int>();
                    status_count[PlayerStatus.Dead] = 0;
                    status_count[PlayerStatus.Escaped] = 0;
                    status_count[PlayerStatus.Win] = 0;
                    double reward_max = double.MinValue;
                    using (StreamWriter status_sw = new StreamWriter(string.Format("train-status-{0}.csv", NumEpisodes)))
                    {
                        status_sw.Write("episodes,exceptions,deaths,escapes,wins,when");
                        status_sw.WriteLine();
                        for (int i = 0; i < NumEpisodes; i++)
                        {
                            long lstart = DateTime.Now.Ticks;
                            World.Quiet = true;
                            s.Episode(env, hp);
                            var report = World.Report;
                            if ((World.Status == PlayerStatus.Win || World.Metrics.TotalReward > reward_max) && args.obsession)
                            {
                                // TODO: Want to remember this for obsessive replay
                                Console.WriteLine("{0} : +Obsessing on the win with {1} reward!", i, World.Metrics.TotalReward);
                                s.Obsess(hp, 100,ReminisceType.Positive);
                                if (World.Metrics.TotalReward > reward_max)
                                {
                                    reward_max = World.Metrics.TotalReward;
                                }
                            }
                            avgReward.Add(World.Metrics.TotalReward);
                            // Log the result
                            float f = (float)i / (float)NumEpisodes;
                            // Reminisce the past
                            World.Metrics.StartReminiscing();
                            s.Reminisce(hp, 300);
                            World.Metrics.EndReminiscing();
                            // Only decay in training
                            hp.DecayEpsilon();
                            hp.DecayAlpha();
                            if ((i + 1) % args.epStep == 0)
                            {
                                Console.WriteLine("Ending training episode {0} with total reward {1} in {2} states and {3} actions.", i + 1, World.Metrics.TotalReward, World.Metrics.NumberOfStatesVisited, World.Metrics.NumberOfActionsTaken);
                                Console.WriteLine("Hypers: e={0}, a={1}", hp.Epsilon.Value, hp.Alpha.Value);
                                swout.Flush();
                                status_sw.WriteLine("{0},{1},{2},{3},{4},{5:MM/dd/yyyy HH:mm:ss}", i, exceptions, status_count[PlayerStatus.Escaped], status_count[PlayerStatus.Dead], status_count[PlayerStatus.Win], DateTime.Now);
                                status_sw.Flush();
                                World.Metrics.Dump();
                                //
                                // Evaluate
                                //
                                Console.WriteLine("==-==-==-==-==-==-==-==-==-==-==-==-==-==-==-==-==-==-==-==-==-==-==-==-==-==-==-==");
                                int mm = 1;
                                Console.WriteLine("Evaluating epoch {0}", i / args.epStep);
                                foreach (MACEMind<T> mind in _Q)
                                {
                                    Console.WriteLine("Mind {1} Took {0:N0} random acts...", mind.Selector.RandomActionCount, i / args.epStep, mm);
                                    try
                                    {
                                        double[,] matrix = mind.Mind.AsMatrix;
                                        files.Add($"matrix-{i}-{mm}.csv");
                                        using (StreamWriter swq = new StreamWriter($"matrix-{i}-{mm}.csv"))
                                        {
                                            for (int jx = 0; jx < matrix.GetLength(0); jx++)
                                            {
                                                for (int ix = 0; ix < matrix.GetLength(1); ix++)
                                                {
                                                    swq.Write("{0},", matrix[jx, ix]);
                                                }
                                                swq.WriteLine();
                                            }
                                            swq.Flush();
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        Console.WriteLine("No AsMatrix output.");
                                    }
                                    mm++;
                                }
                                Console.WriteLine("Total memory in use {0:N0} bytes.", GC.GetTotalMemory(false));
                                swout.Flush();
                                using (StreamWriter swout2 = new StreamWriter($"eval-{i}-{NumEpisodes}x{args.epStep}-{evalType}.log"))
                                {
                                    Console.SetOut(swout2);
                                    // Evaluate
                                    EvaluateIt(false);
                                }
                                Console.SetOut(swout);
                            }
                            World.Metrics.ClearActionHistory();
                            lstart = DateTime.Now.Ticks - lstart;
                            TimeSpan ts = new TimeSpan(lstart);
                            avgEpisodeTime.Add(ts.TotalMilliseconds);
                            status_count[World.Status] += 1;
                            WumpusGameStatus sg = World.Report;
                            swResult.WriteLine("{0},{1},{2},{3},{4},{5},{6},{7},{8}", status_count[PlayerStatus.Dead], status_count[PlayerStatus.Escaped], World.Metrics.TotalReward, avgReward.Value, hp.Epsilon.Value, hp.Alpha.Value,sg.Gold,sg.FoodLevel, status_count[PlayerStatus.Win]);
                        }
                        status_sw.Write(NumEpisodes);
                        status_sw.Write(',');
                        status_sw.Write(exceptions);
                        status_sw.WriteLine();
                    }
                    Console.WriteLine("===================================================================================");
                    Console.WriteLine("Average reward {0:F3}", avgReward.Value);
                    Console.WriteLine("Average episode runtime {0:F3} ms", avgEpisodeTime.Value);
                    Console.WriteLine("{0} exceptions (out of iterations)", exceptions);
                    Console.WriteLine("{0} Escapes, {1} deaths, {2} wins", status_count[PlayerStatus.Escaped], status_count[PlayerStatus.Dead], status_count[PlayerStatus.Win]);
                    foreach(MACEMind<T> mx in _Q)
                        Console.WriteLine("{0} states, {1} actions, {2} random acts in the Q", mx.Mind.Shape.Item1, mx.Mind.Shape.Item2, mx.Selector.RandomActionCount);
                    // Write the frames file for the animation
                    using (StreamWriter swx = new StreamWriter("frames.txt"))
                    {
                        foreach(string s1 in files)
                            swx.WriteLine(s1);
                    }
                    // for /f %i in (frames.txt) do if exist %i python ..\Heatmap.py %i
                }
            }
            // Reset the console output
            Console.SetOut(console);
        }

        public void Save(string fname)
        {
            using (System.IO.Compression.GZipStream gz = new System.IO.Compression.GZipStream(new FileStream(fname + ".gz", FileMode.OpenOrCreate), System.IO.Compression.CompressionLevel.Optimal))
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(gz, _Q);
            }
            Console.WriteLine("Created compressed serialization of Q at {0}", fname + ".gz");
        }

        public void Load(string fname)
        {
            if (!File.Exists(fname) && File.Exists(fname + ".gz"))
            {
                fname += ".gz";
            }
            if (fname.EndsWith(".gz"))
            {
                using (System.IO.Compression.GZipStream gz = new System.IO.Compression.GZipStream(new FileStream(fname, FileMode.Open), System.IO.Compression.CompressionMode.Decompress, false))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    _Q = bf.Deserialize(gz) as MACEMind<T>[];
                }
            }
            else
            {
                using (FileStream fs = new FileStream(fname, FileMode.Open))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    _Q = bf.Deserialize(fs) as MACEMind<T>[];
                }
            }
            if (_Q == null)
            {
                Console.WriteLine("OOPS, that file does not have a Q type that is compatible. Ignoring.");
            }
            else
            {
                Console.WriteLine("Loaded the Q state from {0}", fname);
            }
        }

        // int epStep, int warmup_episodes = 0, int memory_size = 500, int dyna_size = 200, bool use_negpos_memory = false, double dyna_freq = 0.2, QEvalType evalType = QEvalType.OffPolicy, int max_training_steps = 500, bool use_episodic_memory = false
        public void Run(ProgramArgs args)
        {
            if (args.randomize)
            {
                this._TheRandom = QRandom.Instance.Ran;
            }
            QEvalType evalType = QEvalType.OffPolicy;
            if (args.onpolicy)
                evalType = QEvalType.OnPolicy;
            TextWriter console = Console.Out;
            Stopwatch watch = new Stopwatch();
            watch.Start();
            if (args.replay != null)
            {
                World = CreateWorld();
                World.RewardModel = args.model;
                World.Dimensions = args.Dimensions;
                World.Reset();
                World.Quiet = false;
                Console.WriteLine("===================================================================================");
                Console.WriteLine("Action Sequence Replay");
                int[] action = new int[1];
                using (StreamWriter swout2 = new StreamWriter($"eval-replay-{NumEpisodes}x{args.epStep}-{evalType}.log"))
                {
                    Console.SetOut(swout2);
                    foreach (string a in args.replay)
                    {
                        action[0] = int.Parse(a);
                        World.Step(action);
                        World.Render();
                    }
                }
                Console.SetOut(console);
            }
            else
            {
                TrainIt(args);
                using (StreamWriter swout = new StreamWriter($"eval-final-{NumEpisodes}x{args.epStep}-{evalType}.log"))
                {
                    Console.SetOut(swout);
                    // Evaluate
                    EvaluateIt(false);
                }
            }
            // Reset the console output
            Console.SetOut(console);
            // _Q.Dump();
            World.Metrics.Dump();
            Console.WriteLine("===================================================================================");
            Console.WriteLine("Best Action Sequence Replay");
            World.Reset();
            World.Quiet = false;
            /*foreach (Tuple<int[], bool> action in World.Metrics.BestEpisodeTrace)
            {
                World.Step(action.Item1);
                World.Render();
            }*/
            watch.Stop();
            Console.WriteLine("Run took {0:c}", watch.Elapsed);
        }
    }
}
