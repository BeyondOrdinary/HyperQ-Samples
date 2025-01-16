﻿using HyperQ.Env;
using HyperQ.Learners;
using HyperQ.Training;
using HyperQ.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace LEM
{
    public class QQLEMRunner : LEMRunner<decimal>
    {
        public QQLEMRunner(int numepisodes, HyperParams h = null) : base(numepisodes,h) { }
        protected override Q<decimal> CreateLearner()
        {
            return new MappedQQ<decimal>(World.NumActions, QRandom.Instance.DefaultRandomAction, ran: QRandom.Instance.Ran);
        }
        protected override LEMBaseGameEnv CreateWorld()
        {
            LEMGameEnv world = new LEMGameEnv(ran: QRandom.Instance.Ran, quiet: Quiet, stepduration: StepDuration);
            world.Metrics = new EnvMetrics(false);
            return (world);
        }
    }
    public class HyperQQLEMRunner : LEMRunner<QState<decimal>>
    {
        public HyperQQLEMRunner(int numepisodes, HyperParams h = null) : base(numepisodes,h) { }
        protected override Q<QState<decimal>> CreateLearner()
        {
            return new DoubleHyperQ<decimal>(World.NumActions, QRandom.Instance.DefaultRandomAction, ran: QRandom.Instance.Ran);
        }
        protected override LEMBaseGameEnv CreateWorld()
        {
            LEMHyperGameEnv world = new LEMHyperGameEnv(ran: QRandom.Instance.Ran, quiet: Quiet, stepduration: StepDuration);
            world.Metrics = new EnvMetrics(false);
            return (world);
        }
    }
    public class QLEMRunner : LEMRunner<decimal>
    {
        public QLEMRunner(int numepisodes, HyperParams h = null) : base(numepisodes,h) { }
        protected override Q<decimal> CreateLearner()
        {
            return new BasicMappedQ<decimal>(World.NumActions, QRandom.Instance.DefaultRandomAction);
        }
        protected override LEMBaseGameEnv CreateWorld()
        {
            LEMGameEnv world = new LEMGameEnv(ran: QRandom.Instance.Ran, quiet: Quiet, stepduration: StepDuration);
            world.Metrics = new EnvMetrics(false);
            return (world);
        }
    }
    public class HyperQLEMRunner : LEMRunner<QState<decimal>>
    {
        public HyperQLEMRunner(int numepisodes, HyperParams h = null) : base(numepisodes,h) { }
        protected override Q<QState<decimal>> CreateLearner()
        {
            return new SingleHyperQ<decimal>(World.NumActions, QRandom.Instance.DefaultRandomAction);
        }
        protected override LEMBaseGameEnv CreateWorld()
        {
            LEMHyperGameEnv world = new LEMHyperGameEnv(ran: QRandom.Instance.Ran, quiet: Quiet, stepduration: StepDuration);
            world.Metrics = new EnvMetrics(false);
            return (world);
        }
    }
    public class LayeredHyperQLEMRunner : LEMRunner<QState<decimal>>
    {
        public LayeredHyperQLEMRunner(int numepisodes, HyperParams h = null) : base(numepisodes,h) { }
        protected override Q<QState<decimal>> CreateLearner()
        {
            SingleQGenerator<decimal> g = new SingleQGenerator<decimal>(World.NumActions, QRandom.Instance.DefaultRandomAction);
            return new LayeredHyperQ<decimal>(g.HyperQGenerator, QRandom.Instance.DefaultRandomAction);
        }
        protected override LEMBaseGameEnv CreateWorld()
        {
            LEMHyperGameEnv world = new LEMHyperGameEnv(ran: QRandom.Instance.Ran, quiet: Quiet, stepduration: StepDuration);
            world.Metrics = new EnvMetrics(false);
            return (world);
        }
    }
    public class LayeredHyperQQLEMRunner : LEMRunner<QState<decimal>>
    {
        public LayeredHyperQQLEMRunner(int numepisodes, HyperParams h = null) : base(numepisodes,h) { }
        protected override Q<QState<decimal>> CreateLearner()
        {
            DoubleQGenerator<decimal> g = new DoubleQGenerator<decimal>(World.NumActions, QRandom.Instance.DefaultRandomAction, ran: QRandom.Instance.Ran);
            return new LayeredHyperQ<decimal>(g.HyperGenerator, QRandom.Instance.DefaultRandomAction);
        }
        protected override LEMBaseGameEnv CreateWorld()
        {
            LEMHyperGameEnv world = new LEMHyperGameEnv(ran: QRandom.Instance.Ran, quiet: Quiet, stepduration: StepDuration);
            world.Metrics = new EnvMetrics(false);
            return (world);
        }
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
            if (Verbose)
                if (t != null)
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

    public interface ILEMRunner
    {
        void Run(ProgramArgs args);
        void Save(string fname);
        void Load(string fname);
    }

    [Serializable]
    public abstract class LEMRunner<T> : ILEMRunner
    {
        public int NumEpisodes { get; private set; }
        public LEMBaseGameEnv World { get; private set; }
        protected Q<T> _Q;
        public double StepDuration { get; set; } = 10.0;
        public bool Quiet { get; set; } = true;
        public HyperParams Hypers { get; set; } = new HyperParams(g: 0.997, e: .5, a: 0.7, edecay: 0.999991, adecay: 0.999991, min_epsilon: 0.05, min_alpha: 0.1, tau: 0.95);
        public PvESARSATrainer<T> Model { get; private set; }

        public LEMRunner(int numepisodes, HyperParams h = null)
        {
            NumEpisodes = numepisodes;
            QRandom.Instance.Seed(903387237);
            if (h != null)
            {
                Hypers = h;
            }
        }

        protected abstract Q<T> CreateLearner();
        protected abstract LEMBaseGameEnv CreateWorld();

        private void EvaluateIt(PvESARSATrainer<T> s = null)
        {
            QEvaluator<T> eval = null;
            if (s != null)
            {
                eval = s.MaxEvaluator();
            }
            else
            {
                eval = new QEvaluator<T>(_Q, new MaxActionSelector<T>(), QRandom.Instance.Ran);
            }
            World.Reset();
            //
            // Evaluate
            //
            Console.WriteLine("==-==-==-==-==-==-==-==-==-==-==-==-==-==-==-==-==-==-==-==-==-==-==-==-==-==-==-==");
            World.Quiet = false;
            HyperParams hpz = new HyperParams(g: 0.997, e: 0.0, a: 1, edecay: 1, adecay: 1, min_epsilon: 0.0, min_alpha: 1);
            eval.Episode((IPvEEnv<T>)World, hpz);
            Console.WriteLine("Ending evaluation with total reward {0} in {1} states and {2} actions.", World.Metrics.TotalReward, World.Metrics.NumberOfStatesVisited, World.Metrics.NumberOfActionsTaken);
            List<Tuple<int, bool>> trace = World.Metrics.LastEpisodeTrace;
            World.Reset();
            foreach (Tuple<int, bool> action in trace)
            {
                World.Step(action.Item1);
                World.RenderGUI();
            }
        }

        private int MatrixWriteCount = 0;
        private object mutex = new object();

        private void waitCallbackForAsyncMatrixWrite(object state)
        {
            lock (mutex)
            {
                MatrixWriteCount++;
            }
            Tuple<double[,],string> data = (Tuple<double[,],string>)state;
            double[,] matrix = data.Item1;
            try
            {
                using (StreamWriter swq = new StreamWriter(data.Item2))
                {
                    for (int jx = 0; jx < matrix.GetLength(0); jx++)
                    {
                        for (int ix = 0; ix < matrix.GetLength(1); ix++)
                        {
                            swq.Write($"{matrix[jx, ix]}");
                            if (ix < matrix.GetLength(1) - 1)
                            {
                                swq.Write(',');
                            }
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
            lock (mutex)
            {
                MatrixWriteCount--;
            }
        }

        public void TrainIt(ProgramArgs args)
        {
            QEvalType evalType = QEvalType.OffPolicy;
            if (args.onpolicy)
                evalType = QEvalType.OnPolicy;
            LanderStatus[] keys = new LanderStatus[] { LanderStatus.Crashed, LanderStatus.FreeFall, LanderStatus.Landed };
            List<string> files = new List<string>();
            TextWriter console = Console.Out;
            using (StreamWriter swout = new StreamWriter($"trainit-{NumEpisodes}x{args.epStep}-{evalType}.log"))
            {
                Console.SetOut(swout);
                using (StreamWriter swResult = new StreamWriter($"lem-result-{NumEpisodes}.csv"))
                {
                    swResult.WriteLine("crashed,freefall,landing,landed,totalreward,avgreward,epsilon,alpha");
                    Console.WriteLine("Training {0} episodes", NumEpisodes);
                    Console.WriteLine("  output step = {0}", args.epStep);
                    Console.WriteLine("  warmup = {0}", args.warmup_episodes);
                    Console.WriteLine("  memory = {0} {1}", args.memory_size, args.use_negpos_memory ? "neg/pos" : "all");
                    Console.WriteLine("  dyna = {0} @ {1:F3}", args.dyna_size, args.dyna_freq);
                    if (World == null)
                    {
                        World = CreateWorld();
                        World.Reset();
                        Console.WriteLine("Created the simulation world.");
                    }
                    if (_Q == null)
                    {
                        _Q = CreateLearner();
                        Console.WriteLine("Created the Q learner.");
                    }
                    IPvEEnv<T> env = (IPvEEnv<T>)World;
                    IActionSelector<T> selector = new eGreedyActionSelector<T>(QRandom.Instance.Ran);
                    PvESARSATrainer<T> s = new PvESARSATrainer<T>(_Q, evalType, selector, QRandom.Instance.Ran);
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
                    Dictionary<LanderStatus, int> statusCount = new Dictionary<LanderStatus, int>();
                    statusCount[LanderStatus.Crashed] = 0;
                    statusCount[LanderStatus.FreeFall] = 0;
                    statusCount[LanderStatus.Landed] = 0;
                    statusCount[LanderStatus.Landing] = 0;
                    List<Tuple<double, double, double, double, double, LanderStatus, Tuple<double, double>>> history = new List<Tuple<double, double, double, double, double, LanderStatus, Tuple<double, double>>>();
                    HyperParams hp = new HyperParams(Hypers);
                    s.InTraining = true;
                    if (args.warmup_episodes > 0)
                        s.Warmup(env, hp, args.warmup_episodes);
                    RunningAverage avgReward = new RunningAverage();
                    RunningAverage avgEpisodeTime = new RunningAverage();
                    double reward_max = double.MinValue;
                    using (StreamWriter status_sw = new StreamWriter(string.Format("lem-train-status-{0}.csv", NumEpisodes)))
                    {
                        Task asyncStatus = null;
                        foreach (LanderStatus kk in keys)
                        {
                            status_sw.Write("{0},", kk);
                        }
                        status_sw.Write("episodes,avgms");
                        status_sw.WriteLine();
                        for (int i = 0; i < NumEpisodes; i++)
                        {
                            long ltime = DateTime.Now.Ticks;
                            World.Quiet = true;
                            s.Episode(env, hp);
                            var report = World.Report;
                            if ((World.Status == LanderStatus.Landed || World.Metrics.TotalReward > reward_max) && args.obsession)
                            {
                                Console.WriteLine("{0} : +Obsessing on the win with {1} reward!", i, World.Metrics.TotalReward);
                                s.Obsess(hp, 100, ReminisceType.Positive);
                                if (World.Metrics.TotalReward > reward_max)
                                {
                                    reward_max = World.Metrics.TotalReward;
                                }
                            }
                            avgReward.Add(World.Metrics.TotalReward);
                            history.Add(new Tuple<double, double, double, double, double, LanderStatus, Tuple<double, double>>(report.Item1, report.Item2, report.Item3, report.Item4, report.Item5, report.Item6, new Tuple<double, double>(World.Metrics.TotalReward, (double)avgReward.Value)));
                            statusCount[World.Status]++;
                            World.Metrics.StartReminiscing();
                            s.Reminisce(hp, 300);
                            World.Metrics.EndReminiscing();
                            // Only decay in training
                            hp.DecayEpsilon();
                            hp.DecayAlpha();
                            ltime = DateTime.Now.Ticks - ltime;
                            avgEpisodeTime.Add(new TimeSpan(ltime).TotalMilliseconds);
                            if ((i + 1) % args.epStep == 0)
                            {
                                Console.WriteLine("Ending training episode {0} with total reward {1} in {2} states and {3} actions.", i + 1, World.Metrics.TotalReward, World.Metrics.NumberOfStatesVisited, World.Metrics.NumberOfActionsTaken);
                                Console.WriteLine("Hypers: e={0}, a={1}", hp.Epsilon.Value, hp.Alpha.Value);
                                swout.Flush();
                                using (StreamWriter sw = new StreamWriter(string.Format("lem-result-{0}.csv", i)))
                                {
                                    sw.WriteLine("altitude,velocity,mass,elapsed time,fuel mass remaining,status,reward,avg_reward");
                                    foreach (Tuple<double, double, double, double, double, LanderStatus, Tuple<double, double>> d in history)
                                    {
                                        sw.WriteLine("{0},{1},{2},{3},{4},{5},{6},{7}", d.Item1, d.Item2, d.Item3, d.Item4, d.Item5, d.Item6, d.Item7.Item1, d.Item7.Item2);
                                    }
                                }
                                if (asyncStatus == null || asyncStatus.IsCompleted)
                                {
                                    foreach (LanderStatus kk in keys)
                                    {
                                        status_sw.Write("{0},", statusCount[kk]);
                                    }
                                    status_sw.Write(i);
                                    status_sw.Write(',');
                                    status_sw.Write("{0:F2}", avgEpisodeTime.Value);
                                    status_sw.WriteLine();
                                    status_sw.Flush();
                                }
                                World.Metrics.Dump();
                                if (args.dump)
                                {
                                    files.Add($"matrix-{i}.csv");
                                    System.Threading.ThreadPool.QueueUserWorkItem(waitCallbackForAsyncMatrixWrite, new Tuple<double[,], string>(_Q.AsMatrix, $"matrix-{i}.csv"));
                                }
                                //
                                // Evaluate
                                //
                                Console.WriteLine("==-==-==-==-==-==-==-==-==-==-==-==-==-==-==-==-==-==-==-==-==-==-==-==-==-==-==-==");
                                Console.WriteLine("Evaluating epoch {1} after {0:N0} random acts...", selector.RandomActionCount, i / args.epStep);
                                Console.WriteLine("Total memory in use {0:N0} bytes.", GC.GetTotalMemory(false));
                                swout.Flush();
                                using (StreamWriter swout2 = new StreamWriter($"eval-{i}-{NumEpisodes}x{args.epStep}-{evalType}.log"))
                                {
                                    Console.SetOut(swout2);
                                    // Evaluate
                                    EvaluateIt(s);
                                }
                                Console.SetOut(swout);
                            }
                            if (World.Status == LanderStatus.Landed)
                            {
                                Console.WriteLine("==!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!==");
                                Console.WriteLine("LANDED");
                                swout.Flush();
                                World.Reset();
                                using (StreamWriter swout2 = new StreamWriter($"landing-{i}-{NumEpisodes}x{args.epStep}-{evalType}.log"))
                                {
                                    Console.SetOut(swout2);
                                    // Evaluate
                                    foreach (Tuple<int, bool> action in World.Metrics.LastEpisodeTrace)
                                    {
                                        World.Step(action.Item1);
                                        World.RenderGUI();
                                    }
                                }
                                Console.SetOut(swout);
                                EvaluateIt(s);
                                foreach (Tuple<int, bool> action in World.Metrics.LastEpisodeTrace)
                                {
                                    Console.WriteLine("K=: {0:F2} ({1}){2}", World.ActionToBurn(action.Item1), action.Item1, action.Item2 ? "*" : "");
                                }
                            }
                            World.Metrics.ClearActionHistory();
                            swResult.WriteLine("{0},{1},{2},{3},{4},{5},{6},{7}", statusCount[LanderStatus.Crashed], statusCount[LanderStatus.FreeFall], statusCount[LanderStatus.Landing], statusCount[LanderStatus.Landed], World.Metrics.TotalReward, avgReward.Value, hp.Epsilon.Value, hp.Alpha.Value);
                        }
                        try
                        {
                            Console.WriteLine("===================================FINAL================================================");
                            if (asyncStatus == null || asyncStatus.IsCompleted)
                            {
                                asyncStatus = null;
                                foreach (LanderStatus kk in keys)
                                {
                                    status_sw.Write("{0},", statusCount[kk]);
                                }
                                status_sw.Write(NumEpisodes);
                                status_sw.Write(',');
                                status_sw.Write("{0:F2}", avgEpisodeTime.Value);
                                status_sw.WriteLine();
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                    }
                    Console.WriteLine("=================================COMPLETE================================================");
                    Console.WriteLine("{0} random acts taken", selector.RandomActionCount);
                    Console.WriteLine("Average reward {0:F3}", avgReward.Value);
                    Console.WriteLine("Average ms per episode {0:F2}", avgEpisodeTime.Value);
                    using (StreamWriter sw = new StreamWriter($"lem-telemetry-{NumEpisodes}-final.csv"))
                    {
                        sw.WriteLine("altitude,velocity,mass,elapsed time,fuel mass remaining,status,reward,avg_reward");
                        foreach (Tuple<double, double, double, double, double, LanderStatus, Tuple<double, double>> d in history)
                        {
                            sw.WriteLine("{0},{1},{2},{3},{4},{5},{6},{7}", d.Item1, d.Item2, d.Item3, d.Item4, d.Item5, d.Item6, d.Item7.Item1, d.Item7.Item2);
                        }
                    }
                    using (StreamWriter swx = new StreamWriter("frames.txt"))
                    {
                        foreach (string s1 in files)
                            swx.WriteLine(s1);
                    }
                }
            }
            while (true)
            {
                int c = 0;
                lock (mutex)
                {
                    c = MatrixWriteCount;
                }
                if (c == 0)
                    break;
                Console.WriteLine($"Sleeping, waiting for {c} matrix writes to flush/complete.");
                System.Threading.Thread.Sleep(5000);
            }
            // Reset the console output
            Console.SetOut(console);
        }

        public void Save(string fname)
        {
            using (System.IO.Compression.GZipStream gz = new System.IO.Compression.GZipStream(new FileStream(fname+".gz", FileMode.OpenOrCreate),System.IO.Compression.CompressionLevel.Optimal))
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
                    _Q = bf.Deserialize(gz) as Q<T>;
                }
            }
            else
            {
                using (FileStream fs = new FileStream(fname, FileMode.Open))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    _Q = bf.Deserialize(fs) as Q<T>;
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

        public void Run(ProgramArgs args)
        {
            QEvalType evalType = QEvalType.OffPolicy;
            if (args.onpolicy)
                evalType = QEvalType.OnPolicy;
            TextWriter console = Console.Out;
            Stopwatch watch = new Stopwatch();
            watch.Start();
            TrainIt(args);
            using (StreamWriter swout = new StreamWriter($"eval-final-{NumEpisodes}x{args.epStep}-{evalType}.log"))
            {
                Console.SetOut(swout);
                // Evaluate
                EvaluateIt();
            }
            // Reset the console output
            Console.SetOut(console);
            _Q.Dump();
            World.Metrics.Dump();
            Console.WriteLine("===================================================================================");
            Console.WriteLine("Best Action Sequence Replay");
            World.Reset();
            World.Quiet = false;
            console = Console.Out;
            using (StreamWriter swout = new StreamWriter($"eval-best-{NumEpisodes}x{args.epStep}-{evalType}.log"))
            {
                Console.SetOut(swout);
                foreach (Tuple<int, bool> action in World.Metrics.BestEpisodeTrace)
                {
                    World.Step(action.Item1);
                    World.RenderGUI();
                }
            }
            // Reset the console output
            Console.SetOut(console);
            watch.Stop();
            Console.WriteLine("Run took {0:c}", watch.Elapsed);
        }
    }
}
