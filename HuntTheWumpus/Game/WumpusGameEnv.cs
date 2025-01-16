﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HyperQ.Env;
using HyperQ.Learners;

namespace HuntTheWumpus
{
    public class WumpusGameEnv : WumpusBaseGameEnv, IMACEPvEEnv<decimal>
    {
        public WumpusGameEnv(Random ran = null, bool quiet = false) : base(ran, quiet)
        {
        }
        public decimal Discretize()
        {
            WumpusGameStatus rpt = Report;
            decimal ival = 0;
            int shift = 0;
            /*
            ival += (long)rpt.Location << shift;
            shift++;
            */
            // Encode the look into a 32 bit value. This assumes full observability
            for (int i = 0; i < rpt.Adjacent.Length; i++)
            {
                ulong lv = (ulong)rpt.Adjacent[i] << shift;
                // Console.WriteLine("{0}:{1} << {2} = 0x{3:X24}", i, rpt.Adjacent[i], shift, lv);
                ival += lv;
                shift += 3;
            }
            // Add the gold indicator
            if (rpt.Gold > 0)
            {
                ival += 1L << shift;
            }
            shift++;
            ival += (ulong)rpt.FoodLevel << shift;
            shift += 6;
            // Future - add distance from entrace
            return (ival);
        }
    }
}
