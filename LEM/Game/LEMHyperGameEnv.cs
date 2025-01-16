using HyperQ.Env;
using HyperQ.Learners;
using HyperQ.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LEM
{
    [Serializable]
    public class LEMHyperGameEnv : LEMBaseGameEnv, IPvEEnv<QState<decimal>>
    {

        public LEMHyperGameEnv(double g = 0.001, double netmass = 16500, Random ran = null, bool quiet = false, EnvMetrics metrics = null, int precision = DEFAULT_ACTION_PRECISION, double stepduration = 10.0)
        : base(g, netmass, ran, quiet, precision, stepduration)
        {
            Metrics = metrics ?? new EnvMetrics();
        }

        public Tuple<double, bool> Step(int[] action)
        {
            return (Step(action[0]));
        }

        public QState<decimal> Discretize()
        {
            QState<decimal> s = new QState<decimal>();
            var rpt = Report;
            // 1: _LEM.Altitude
            // 2: _LEM.Speed
            // 3: _LEM.Mass
            // 4: _ElapsedTime
            // 5: _LEM.NetFuel
            // 6: Status
            s.Push((decimal)rpt.Item1); // Altitude
            s.Push((decimal)rpt.Item2); // Speed
            s.Push((decimal)rpt.Item5); // Fuel (this includes mass)
            //            s.Push(Math.Truncate((decimal)(rpt.Item3 / 32500 * 100.0)));
            //s.Push(Math.Truncate((decimal)(rpt.Item5 / 16000 * 100.0)));
            //s.Push(Math.Truncate((decimal)(rpt.Item1 / 120.0 * 100.0)));
            //s.Push(Math.Truncate((decimal)(rpt.Item2 * 3600.0 * 100.0)));
            return s;
        }
    }
}
