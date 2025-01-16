using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HyperQ.Env;
using HyperQ.Learners;

namespace Simulation.LunarLander
{
    public class Rocket2GameEnv : Rocket2BaseGameEnv, IMACEPvEEnv<decimal>
    {
        public Rocket2GameEnv(Random ran = null, bool quiet = false) : base(ran, quiet)
        {
        }
        public decimal Discretize()
        {
            Rocket2Report rpt = Report;
            decimal fmass = (decimal)Math.Truncate(rpt.FuelLevel / 750.0 * 100);
            decimal falt = (decimal)Math.Truncate(rpt.AltitudeInMiles / 60 * 100);
            decimal fvh = (decimal)Math.Truncate(rpt.HorizontalSpeed / 6000 * 100);
            decimal fvz = (decimal)Math.Truncate(rpt.VerticalSpeed / 6000 * 100);
            decimal s = Math.Truncate(1000000000M * fvz + 1000000M * fvh + 1000M * falt + fmass);
            return (s);
        }
    }
}
