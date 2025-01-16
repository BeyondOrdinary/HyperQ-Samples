using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HyperQ.Env;
using HyperQ.Learners;

namespace Simulation.LunarLander
{
    public class Rocket2HyperPIDGameEnv : Rocket2BaseGameEnv, IMACEPvEEnv<QState<decimal>>
    {
        public Rocket2HyperPIDGameEnv(Random ran = null, bool quiet = false) : base(ran, quiet)
        {
        }
        public QState<decimal> Discretize()
        {
            QState<decimal> s = new QState<decimal>();
            Rocket2Report rpt = Report;
            double h_target = Math.Abs(rpt.DistanceToLZInMiles) * 0.55;
            double angle_targ = Math.Atan2(Math.Abs(rpt.AltitudeInMiles), Math.Abs(rpt.DistanceToLZInMiles));
            double h_todo = (h_target - rpt.AltitudeInMiles) * 0.5 - rpt.VerticalSpeed * 0.5;
            double angle_todo = 0.5 * angle_targ;
            //            s.Push(Math.Truncate((decimal)(rpt.Item3 / 32500 * 100.0)));
            s.Push(Math.Truncate((decimal)angle_todo));
            s.Push(Math.Truncate((decimal)h_todo));
            s.Push(Math.Truncate((decimal)rpt.VerticalSpeed));
            s.Push(Math.Truncate((decimal)rpt.HorizontalSpeed));
            s.Push(Math.Truncate((decimal)rpt.FuelLevel / 750M * 100M));
            return s;
        }
    }
}
