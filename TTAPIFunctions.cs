using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TTAPI_Sample_Console_ASEOrderRouting
{
    using TradingTechnologies.TTAPI;
    using TradingTechnologies.TTAPI.Tradebook;
    using TradingTechnologies.TTAPI.Autospreader;
    using ASG_Tools;

    /// <summary>
    /// Main TT API class
    /// </summary>
    class TTAPIFunctions : IDisposable
    {
        #region Variables
        private UniversalLoginTTAPI m_apiInstance = null;
        private WorkerDispatcher m_disp = null;
        private bool m_disposed = false;
        private object m_lock = new object();
        private InstrumentLookupSubscription m_req1 = null;
        private InstrumentLookupSubscription m_req2 = null;
        private CreateAutospreaderInstrumentRequest m_casReq = null;
        private PriceSubscription m_ps = null;
        private ASInstrumentTradeSubscription m_ts = null;
        private string m_orderKey = "";
        private Dictionary<int, Instrument> m_spreadLegKeys = new Dictionary<int, Instrument>();

        private string m_username = "";
        private string m_password = "";

        // TODO: replace custom spread class  
        //private SpreadDetails spread_order;
        //private SpreadLegDetails spread_legA;
        //private SpreadLegDetails spread_legB;


        private SpreadOrder m_order;
        private SpreadLeg m_leg1;
        private SpreadLeg m_leg2;

        //Spread variables
        private string  m_ASEGateway = "AlgoSE";
        private string  m_SpreadName = "TestSpread";
        private BuySell m_BS = BuySell.Buy;
        private int     m_QTY = 1;
        private Double  m_SpreadPrice = -30.00;

        //leg variables
        private MarketKey m_mk1 = MarketKey.Cme;
        private ProductType m_pt1 = ProductType.Future;
        private string  m_Product1 = "CL";
        private string  m_Contract1 = "JAN14";
        private string  m_Customer1 = "<Default>";
        private int     m_Ratio1 = 1;
        private double  m_Mult1 = 1;
        private bool    m_AQ1 = true;

        private MarketKey m_mk2 = MarketKey.Cme;
        private ProductType m_pt2 = ProductType.Future;
        private string  m_Product2 = "CL";
        private string  m_Contract2 = "FEB14";
        private string  m_Customer2 = "<Default>";
        private int     m_Ratio2 = -1;
        private double  m_Mult2 = -1;
        private bool    m_AQ2 = true;
        #endregion

        /// <summary>
        /// Private default constructor
        /// </summary>
        private TTAPIFunctions()
        {
        }

        /// <summary>
        /// Primary constructor
        /// </summary>
        public TTAPIFunctions(string u, string p, SpreadOrder so, SpreadLeg sl1, SpreadLeg sl2)
        {
            m_username = u;
            m_password = p;
            m_order = so;
            m_leg1 = sl1;
            m_leg2 = sl2;

            m_ASEGateway = so.Server;
            m_SpreadName = so.Name;
            m_BS = so.Side;
            m_QTY = so.Quantity;
            m_SpreadPrice = so.Price;

            m_mk1 = sl1.Market;
            m_Product1 = sl1.Product;
            m_pt1 = sl1.Type;
            m_Contract1 = sl1.Contract;
            m_Customer1 = sl1.Customer;
            m_Ratio1 = sl1.Ratio;
            m_Mult1 = sl1.Multiplier;
            m_AQ1 = sl1.ActiveQuoting;

            m_mk2 = sl2.Market;
            m_Product2 = sl2.Product;
            m_pt2 = sl2.Type;
            m_Contract2 = sl2.Contract;
            m_Customer2 = sl2.Customer;
            m_Ratio2 = sl2.Ratio;
            m_Mult2 = sl2.Multiplier;
            m_AQ2 = sl2.ActiveQuoting;

            Console.WriteLine("Data Populated");
        }

        /// <summary>
        /// Create and start the Dispatcher
        /// </summary>
        public void Start()
        {
            // Attach a WorkerDispatcher to the current thread
            m_disp = Dispatcher.AttachWorkerDispatcher();
            m_disp.BeginInvoke(new Action(Init));
            m_disp.Run();
        }

        /// <summary>
        /// Initialize TT API
        /// </summary>
        public void Init()
        {
            // Use "Universal Login" Login Mode
            TTAPI.UniversalLoginModeDelegate ulDelegate = new TTAPI.UniversalLoginModeDelegate(ttApiInitComplete);
            TTAPI.CreateUniversalLoginTTAPI(Dispatcher.Current, ulDelegate);
        }

        /// <summary>
        /// Event notification for status of TT API initialization
        /// </summary>
        public void ttApiInitComplete(UniversalLoginTTAPI api, Exception ex)
        {
            if (ex == null)
            {
                // Authenticate your credentials
                m_apiInstance = api;
                m_apiInstance.AuthenticationStatusUpdate += new EventHandler<AuthenticationStatusUpdateEventArgs>(apiInstance_AuthenticationStatusUpdate);
                m_apiInstance.Authenticate(m_username, m_password);
            }
            else
            {
                Console.WriteLine("TT API Initialization Failed: {0}", ex.Message);
                Dispose();
            }
        }

        /// <summary>
        /// Event notification for status of authentication
        /// </summary>
        public void apiInstance_AuthenticationStatusUpdate(object sender, AuthenticationStatusUpdateEventArgs e)
        {
            if (e.Status.IsSuccess)
            {
                // lookup the leg instruments

                ProductKey prodKeyLeg1 = new ProductKey(m_mk1, m_pt1, m_Product1);
                ProductKey prodKeyLeg2 = new ProductKey(m_mk2, m_pt2, m_Product2);
                // We will use a dictionary to hold all instrument requests and update it when each instrument is found.
                // Once all lookup requests for the legs are complete, we can continue with the creation of the spread.
                // tagValue will be used in the dictionary to identify each lookup request.

                int tagValue = 1;

                m_req1 = new InstrumentLookupSubscription(m_apiInstance.Session, Dispatcher.Current, prodKeyLeg1, m_Contract1);
                m_req1.Tag = tagValue;
                m_spreadLegKeys.Add(tagValue, null);
                m_req1.Update += new EventHandler<InstrumentLookupSubscriptionEventArgs>(m_req_Update);
                m_req1.Start();

                tagValue++;

                m_req2 = new InstrumentLookupSubscription(m_apiInstance.Session, Dispatcher.Current, prodKeyLeg2, m_Contract2);
                m_req2.Tag = tagValue;
                m_spreadLegKeys.Add(tagValue, null);
                m_req2.Update += new EventHandler<InstrumentLookupSubscriptionEventArgs>(m_req_Update);
                m_req2.Start();
            }
            else
            {
                Console.WriteLine("TT Login failed: {0}", e.Status.StatusMessage);
                Dispose();
            }
        }

        /// <summary>
        /// Event notification for instrument lookup
        /// </summary>
        void m_req_Update(object sender, InstrumentLookupSubscriptionEventArgs e)
        {
            if (e.Instrument != null && e.Error == null)
            {
                // Instrument was found
                Console.WriteLine("Found: {0}", e.Instrument.Name);

                // Update the dictionary to indicate that the instrument was found.
                InstrumentLookupSubscription instrLookupSub = sender as InstrumentLookupSubscription;

                if (m_spreadLegKeys.ContainsKey((int)instrLookupSub.Tag))
                {
                    m_spreadLegKeys[(int)instrLookupSub.Tag] = e.Instrument;
                }
            }
            else if (e.IsFinal)
            {
                // Instrument was not found and TT API has given up looking for it
                Console.WriteLine("Cannot find instrument: {0}", e.Error.Message);
                Console.ReadKey();
                Dispose();
            }

            // If we have found all of the leg instruments, proceed with the creation of the spread.
            if (HaveWeFoundAllLegs())
            {
                Console.WriteLine("All leg instruments have been found.  Creating the spread...");

                // SpreadDetails related properties
                SpreadDetails spreadDetails = new SpreadDetails();
                spreadDetails.Name = m_SpreadName ;

                
                // Add the legs to the SpreadDetails
                foreach (Instrument instrument in m_spreadLegKeys.Values)
                {
                    // In this example, the order is routed to the first order feed in the list of valid order feeds.
                    // You should use the order feed that is appropriate for your purposes.
                    SpreadLegDetails spreadlegDetails = new SpreadLegDetails(instrument.Key, instrument.GetValidOrderFeeds()[0].ConnectionKey);
                    if (m_spreadLegKeys[1].Equals(instrument))
                    {
                        spreadlegDetails.SpreadRatio = m_Ratio1;
                        spreadlegDetails.PriceMultiplier = m_Mult1;
                        spreadlegDetails.CustomerName = m_Customer1;
                        spreadlegDetails.ActiveQuoting = m_AQ1;
                        spreadDetails.Legs.Append(spreadlegDetails);
                        Console.WriteLine("Added Leg 1"); 
                    }
                    else if (m_spreadLegKeys[2].Equals(instrument))
                    {
                        spreadlegDetails.SpreadRatio = m_Ratio2;
                        spreadlegDetails.PriceMultiplier = m_Mult2;
                        spreadlegDetails.CustomerName = m_Customer2;
                        spreadlegDetails.ActiveQuoting = m_AQ2;
                        spreadDetails.Legs.Append(spreadlegDetails);
                        Console.WriteLine("Added Leg 2");
                    }
                }

                // Create an AutospreaderInstrument corresponding to the synthetic spread
                m_casReq = new CreateAutospreaderInstrumentRequest(m_apiInstance.Session, Dispatcher.Current, spreadDetails);
                m_casReq.Completed += new EventHandler<CreateAutospreaderInstrumentRequestEventArgs>(m_casReq_Completed);
                m_casReq.Submit();
            }
        }

        /// <summary>
        /// Event notification for AutospreaderInstrument creation
        /// </summary>
        public void m_casReq_Completed(object sender, CreateAutospreaderInstrumentRequestEventArgs e)
        {
            if (e.Error == null)
            {
                if (e.Instrument != null)
                {
                    Console.WriteLine("Synthetic Spread creation completed");
                    // In this example, the AutospreaderInstrument is launched to ASE-A.
                    // You should use the order feed that is appropriate for your purposes.
                    OrderFeed oFeed = this.GetOrderFeedByName(e.Instrument, m_ASEGateway);
                   
                    if (oFeed != null && oFeed.IsTradingEnabled)
                    {
                        Console.WriteLine("OrderFeed: {0} Trading Enabled: {1}", oFeed.Name, oFeed.IsTradingEnabled);   
                        // deploy the Autospreader Instrument to the specified ASE
                        e.Instrument.TradableStatusChanged += new EventHandler<TradableStatusChangedEventArgs>(Instrument_TradableStatusChanged);
                        LaunchReturnCode lrc = e.Instrument.LaunchToOrderFeed(oFeed);
                        if (lrc != LaunchReturnCode.Success)
                        {
                            Console.WriteLine("Launch request was unsuccessful");
                        }
                        else
                        { Console.WriteLine("Launch Request was {0}", lrc); }
                    }
                    else
                    { Console.WriteLine("Orderfeed {0} invalid", oFeed.Market.Name); }
                }
            }
            else
            {
                // AutospreaderInstrument creation failed
                Console.WriteLine("AutospreaderInstrument creation failed: " + e.Error.Message);
            }
        }

        /// <summary>
        /// Event notification for AutospreaderInstrument launch
        /// </summary>
        void Instrument_TradableStatusChanged(object sender, TradableStatusChangedEventArgs e)
        {
            if (e.Value)
            {
                Console.WriteLine("Launch of AutospreaderInstrument to {0} was successful.", e.OrderFeed.Name);
                // launch of AutospreaderInstrument was successful
                AutospreaderInstrument instr = sender as AutospreaderInstrument;

                // Subscribe for Inside Market Data
                m_ps = new PriceSubscription(instr, Dispatcher.Current);
                m_ps.Settings = new PriceSubscriptionSettings(PriceSubscriptionType.InsideMarket);
                m_ps.FieldsUpdated += new FieldsUpdatedEventHandler(m_ps_FieldsUpdated);
                m_ps.Start();

                // Create an ASTradeSubscription to listen for order / fill events only for orders submitted through it
                m_ts = new ASInstrumentTradeSubscription(m_apiInstance.Session, Dispatcher.Current, instr, true, true, false, false);
                m_ts.OrderUpdated += new EventHandler<OrderUpdatedEventArgs>(m_ts_OrderUpdated);
                m_ts.OrderAdded += new EventHandler<OrderAddedEventArgs>(m_ts_OrderAdded);
                m_ts.OrderDeleted += new EventHandler<OrderDeletedEventArgs>(m_ts_OrderDeleted);
                m_ts.OrderFilled += new EventHandler<OrderFilledEventArgs>(m_ts_OrderFilled);
                m_ts.OrderRejected += new EventHandler<OrderRejectedEventArgs>(m_ts_OrderRejected);
                m_ts.Start();
            }
            else
            {
                Console.WriteLine("Launch of AutospreaderInstrument to {0} was unsuccessful.", e.OrderFeed.Name);
            }
        }

        /// <summary>
        /// Event notification for price update
        /// </summary>
        void m_ps_FieldsUpdated(object sender, FieldsUpdatedEventArgs e)
        {
            if (e.Error == null)
            {

                if (m_orderKey == "")
                {
                    // In this example, the order is submitted to ASE-A.
                    // You should use the order feed that is appropriate for your purposes.
                    AutospreaderSyntheticOrderProfile op = new AutospreaderSyntheticOrderProfile(this.GetOrderFeedByName(e.Fields.Instrument, m_ASEGateway),
                        (AutospreaderInstrument)e.Fields.Instrument);
                    op.BuySell = m_BS;
                    op.OrderQuantity = Quantity.FromInt(e.Fields.Instrument, m_QTY);
                    op.OrderType = OrderType.Limit;
                    op.LimitPrice = Price.FromDouble(e.Fields.Instrument, m_SpreadPrice);
                        

                    if (!m_ts.SendOrder(op))
                    {
                        Console.WriteLine("Send new order failed.  {0}", op.RoutingStatus.Message);
                    }
                    else
                    {
                        m_orderKey = op.SiteOrderKey;
                        Console.WriteLine("Send new order succeeded. Order Key: {0}", m_orderKey);
                    }
                }
     
            }
            else
            {
                if (e.Error.IsRecoverableError == false)
                {
                    Console.WriteLine("Unrecoverable price subscription error: {0}", e.Error.Message);
                    Dispose();
                }
            }
        }

        /// <summary>
        /// Event notification for order rejected
        /// </summary>
        void m_ts_OrderRejected(object sender, OrderRejectedEventArgs e)
        {
            if (e.Order.SiteOrderKey == m_orderKey)
            {
                // Our parent order has been rejected
                Console.WriteLine("Our parent order has been rejected: {0}", e.Message);
            }
            else if (e.Order.SyntheticOrderKey == m_orderKey)
            {
                // A child order of our parent order has been rejected
                Console.WriteLine("A child order of our parent order has been rejected: {0}", e.Message);
            }
        }

        /// <summary>
        /// Event notification for order filled
        /// </summary>
        void m_ts_OrderFilled(object sender, OrderFilledEventArgs e)
        {
            if (e.Fill.SiteOrderKey == m_orderKey)
            {
                // Our parent order has been filled
                Console.WriteLine("Our parent order has been " + (e.Fill.FillType == FillType.Full ? "fully" : "partially") + " filled");
            }
            else if (e.Fill.ParentKey == m_orderKey)
            {
                // A child order of our parent order has been filled
                Console.WriteLine("A child order of our parent order has been " + (e.Fill.FillType == FillType.Full ? "fully" : "partially") + " filled");
            }

            Console.WriteLine("Average Buy Price = {0} : Net Position = {1} : P&L = {2}", m_ts.ProfitLossStatistics.BuyAveragePrice,
                m_ts.ProfitLossStatistics.NetPosition, m_ts.ProfitLoss.AsPrimaryCurrency);
        }

        /// <summary>
        /// Event notification for order deleted
        /// </summary>
        void m_ts_OrderDeleted(object sender, OrderDeletedEventArgs e)
        {
            if (e.DeletedUpdate.SiteOrderKey == m_orderKey)
            {
                // Our parent order has been deleted
                Console.WriteLine("Our parent order has been deleted: {0}", e.Message);
            }
            else if (e.DeletedUpdate.SyntheticOrderKey == m_orderKey)
            {
                // A child order of our parent order has been deleted
                Console.WriteLine("A child order of our parent order has been deleted: {0}", e.Message);
            }
        }

        /// <summary>
        /// Event notification for order added
        /// </summary>
        void m_ts_OrderAdded(object sender, OrderAddedEventArgs e)
        {
            if (e.Order.SiteOrderKey == m_orderKey)
            {
                // Our parent order has been added
                Console.WriteLine(e.Message);
            }
            else if (e.Order.SyntheticOrderKey == m_orderKey)
            {
                // "A child order of our parent order has been added: {0}",
                Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Event notification for order update
        /// </summary>
        void m_ts_OrderUpdated(object sender, OrderUpdatedEventArgs e)
        {
            if (e.OldOrder.SiteOrderKey == m_orderKey)
            {
                // Our parent order has been updated
                Console.WriteLine("Our parent order has been updated: {0}", e.Message);
            }
            else if (e.OldOrder.SyntheticOrderKey == m_orderKey)
            {
                // A child order of our parent order has been updated
                //Console.WriteLine("A child order of our parent order has been updated: {0}", e.Message);
            }
        }

        /// <summary>
        /// Shuts down the TT API
        /// </summary>
        public void Dispose()
        {
            Console.WriteLine("Press any key to exit.");
            Console.Read();

            lock (m_lock)
            {
                if (!m_disposed)
                {
                    // Unattached callbacks and dispose of all subscriptions
                    if (m_req1 != null)
                    {
                        m_req1.Update -= m_req_Update;
                        m_req1.Dispose();
                        m_req1 = null;
                    }
                    if (m_req2 != null)
                    {
                        m_req2.Update -= m_req_Update;
                        m_req2.Dispose();
                        m_req2 = null;
                    }
                    if (m_ps != null)
                    {
                        m_ps.FieldsUpdated -= m_ps_FieldsUpdated;
                        m_ps.Dispose();
                        m_ps = null;
                    }
                    if (m_ts != null)
                    {
                        m_ts.OrderAdded -= m_ts_OrderAdded;
                        m_ts.OrderDeleted -= m_ts_OrderDeleted;
                        m_ts.OrderFilled -= m_ts_OrderFilled;
                        m_ts.OrderRejected -= m_ts_OrderRejected;
                        m_ts.OrderUpdated -= m_ts_OrderUpdated;
                        m_ts.Dispose();
                        m_ts = null;
                    }
                    if (m_casReq != null)
                    {
                        m_casReq.Completed -= m_casReq_Completed;
                        m_casReq.Dispose();
                        m_casReq = null;
                    }

                    // Shutdown the TT API
                    if (m_apiInstance != null)
                    {
                        m_apiInstance.Shutdown();
                        m_apiInstance = null;
                    }

                    // Shutdown the Dispatcher
                    if (m_disp != null)
                    {
                        m_disp.BeginInvokeShutdown();
                        m_disp = null;
                    }

                    m_disposed = true;
                }
            }
        }


        /// <summary>
        /// Helper function that determines if all leg instruments have been found
        /// </summary>
        private bool HaveWeFoundAllLegs()
        {
            if (m_spreadLegKeys.Count == 0)
            {
                return false;
            }

            foreach (Instrument instrument in m_spreadLegKeys.Values)
            {
                if (instrument == null)
                {
                    return false;
                }
            }

            return true;
        }
        /// <summary>
        /// Helper function for finding an OrderFeed given a gateway name
        /// </summary>
        OrderFeed GetOrderFeedByName(Instrument instr, string gateway)
        {
            foreach (OrderFeed oFeed in instr.GetValidOrderFeeds())
            {
                if (oFeed.Name.Equals(gateway))
                {
                    return oFeed;
                }
            }

            return (OrderFeed)null;
        }
        
    }
}
