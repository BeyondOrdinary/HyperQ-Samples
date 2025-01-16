using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HyperQ.Env;
using HyperQ.Learners;

namespace Simulation.LunarLander
{
    public class Rocket2HyperGameEnv : Rocket2BaseGameEnv, IMACEPvEEnv<QState<decimal>>
    {
        public Rocket2HyperGameEnv(Random ran = null, bool quiet = false) : base(ran, quiet)
        {
        }
        public QState<decimal> Discretize()
        {
            QState<decimal> s = new QState<decimal>();
            Rocket2Report rpt = Report;
            //            s.Push(Math.Truncate((decimal)(rpt.Item3 / 32500 * 100.0)));
            s.Push(Math.Truncate((decimal)rpt.AltitudeInMiles / 60M * 100M));
            s.Push(Math.Truncate((decimal)rpt.DistanceToLZInMiles));
            s.Push(Math.Truncate((decimal)rpt.VerticalSpeed));
            s.Push(Math.Truncate((decimal)rpt.HorizontalSpeed));
            s.Push(Math.Truncate((decimal)rpt.FuelLevel / 750M * 100M));
            return s;
        }
    }
}
