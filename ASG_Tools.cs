using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASG_Tools
{ 
    using TradingTechnologies.TTAPI;

    public class ASG
    {
        public MarketKey mkt(string market)
        {
            switch (market.ToUpper())
            {
                case "CME":
                    return MarketKey.Cme;
                case "ICE":
                    return MarketKey.Ice;
                default:
                    return MarketKey.Invalid;
            }
        }

        public ProductType prodtype(string pt)
        {
            switch (pt.ToUpper())
            {
                case "FUTURE":
                    return ProductType.Future;
                case "SPREAD":
                    return ProductType.Spread;
                default:
                    return ProductType.Invalid;
            }
        }

    }
}