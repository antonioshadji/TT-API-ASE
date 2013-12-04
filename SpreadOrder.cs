using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TTAPI_Sample_Console_ASEOrderRouting
{
    using TradingTechnologies.TTAPI;

    class SpreadOrder
    {
        public SpreadOrder()
        { }

        public SpreadOrder(string[] order) 
        {
            Server  = order[0];
            Name    = order[1];
            Side    = setSide(order[2]);
            Quantity = Convert.ToInt16(order[3]);
            Price = Convert.ToDouble(order[4]);
        }

        private string se_server;
        private string spread_name;       
        private BuySell side;
        private int quantity;
        private double price;

        public string Server {get; set;}
        public string Name { get; set; }
        public BuySell Side { get; set; }
        public int Quantity { get; set; }
        public double Price { get; set; }


        private void setServer(object o)
        {
            se_server = (string)o; 
        }

        private string getServer() 
        { 
            return se_server; 
        }

        private void setName(object o) 
        {
            spread_name = (string)o;
        }

        private string getName()
        {
            return spread_name;
        }
        
        private BuySell setSide(string value) 
        {
            //Console.WriteLine("Side value = {0}",value.ToUpper()[0]);
            //Console.WriteLine("value.ToUpper()[0].Equals(\'B\')={0}", value.ToUpper()[0].Equals('B'));
            //Console.ReadKey();
            if (value.ToUpper()[0].Equals('B'))
            {
                side = BuySell.Buy;
            }
            else if (value.ToUpper()[0].Equals('S'))
            {
                side = BuySell.Sell;
            }
            else
            { side = BuySell.Unknown; }
            
            return side;

        }

        private BuySell getSide()
        {
            return side;
        }


        private void setQty(object o)
        {
            quantity = (int)o;
        }

        private int getQty()
        {
            return quantity;        
        }
        
        private void setPrice(object o) 
        {
            price = (double)o;
        }

        private double getPrice()
        {
            return price;
        }

    }

    public class SpreadLeg
    {
        public SpreadLeg()
        { }

        public SpreadLeg(string[] leg)
        {
            Market  = setMarket(leg[0]);
            Product = leg[1];
            Type    = setProdType(leg[2]);
            Contract = leg[3];
            Customer = leg[4];
            Ratio   = Convert.ToInt16(leg[5]);
            Multiplier = Convert.ToDouble(leg[6]);
            ActiveQuoting = setAQ(leg[7]);

        }

        public MarketKey Market { get; set; }
        public string Product { get; set; }
        public ProductType Type { get; set; }
        public string Contract { get; set; }
        public string Customer { get; set; }
        public int Ratio { get; set; }
        public double Multiplier { get; set; }
        public bool ActiveQuoting { get; set; }


        private MarketKey setMarket(string market)
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

        

        private ProductType setProdType( string pt )
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

        private bool setAQ(string aq )
        {
            switch (aq.ToUpper()[0])
            {
                case 'T':
                    return true;
                case 'F':
                    return false;
                default:
                    throw new NotImplementedException();
            }
            
        }




    }
}
