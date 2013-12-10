// **********************************************************************************************************************
//
//	Copyright © 2005-2013 Trading Technologies International, Inc.
//	All Rights Reserved Worldwide
//
// 	* * * S T R I C T L Y   P R O P R I E T A R Y * * *
//
//  WARNING: This file and all related programs (including any computer programs, example programs, and all source code) 
//  are the exclusive property of Trading Technologies International, Inc. (“TT”), are protected by copyright law and 
//  international treaties, and are for use only by those with the express written permission from TT.  Unauthorized 
//  possession, reproduction, distribution, use or disclosure of this file and any related program (or document) derived 
//  from it is prohibited by State and Federal law, and by local law outside of the U.S. and may result in severe civil 
//  and criminal penalties.
//
// ************************************************************************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace TTAPI_Sample_Console_ASEOrderRouting
{
    using TTAPI_Utility;
    using TradingTechnologies.TTAPI;


    class Program
    {
        static void Main(string[] args)
        {
            string ttUserId = String.Empty;
            string ttPassword = String.Empty;
        

            SpreadOrder order = new SpreadOrder();
            SpreadLeg leg1 = new SpreadLeg();
            SpreadLeg leg2 = new SpreadLeg();

            if (args.Length == 23)
            {
                ttUserId = (string)args[0];
                ttPassword = (string)args[1];
                string[] l1 = args.Skip(7).Take(8).ToArray();
                string[] l2 = args.Skip(15).Take(8).ToArray();
                
                order = new SpreadOrder(args.Skip(2).Take(5).ToArray());
                leg1 = new SpreadLeg(l1);
                leg2 = new SpreadLeg(l2);

                //DEBUG ONLY
                //Console.WriteLine("Side is {0}" , order.Side);
                //Console.WriteLine("array length {0}, first data {1}, last data {2}", spread.Length ,spread[0], spread[spread.GetUpperBound(0)]);
                //Console.WriteLine("array length {0}, first data {1}, last data {2}", leg1.Length, leg1[0], leg1[leg1.GetUpperBound(0)]);
                //Console.WriteLine("array length {0}, first data {1}, last data {2}", leg2.Length, leg2[0], leg2[leg2.GetUpperBound(0)]);
                //Console.ReadKey();
            }
            else
            {
                Console.WriteLine("Usage: {0} TTUSER TTPASSWORD ASE_Name SpreadName BS QTY Price L1Market L1Product L1Type L1Contract L1Customer L1Ratio L1Multiplier L1ActiveQuote(T/F)  L2Market L2Product L2Type L2Contract L2Customer L2Ratio L2Multiplier L2ActiveQuote(T/F)", System.Diagnostics.Process.GetCurrentProcess().ProcessName);
                Console.WriteLine("Press any key to exit application");
                Console.ReadKey();
                System.Environment.Exit(0);
                
            }

            // Check that the compiler settings are compatible with the version of TT API installed
            if (TTAPIArchitectureCheck.validate())
            {
                Console.WriteLine("Architecture check passed.");

                // Dictates whether TT API will be started on its own thread
                bool startOnSeparateThread = false;

                if (startOnSeparateThread)
                {

                    // Start TT API on a separate thread
                    TTAPIFunctions tf = new TTAPIFunctions(ttUserId, ttPassword, order, leg1, leg2);
                    Thread workerThread = new Thread(tf.Start);
                    workerThread.Name = "TT API Thread";
                    workerThread.Start();

                    // Insert other code here that will run on this thread
                }
                else
                {
                    // Start the TT API on the same thread
                    using (TTAPIFunctions tf = new TTAPIFunctions(ttUserId, ttPassword, order, leg1, leg2 ))
                    {
                        tf.Start();
                    }
                }
            }
            else
            {
                Console.WriteLine("Architecture check failed.  {0}", TTAPIArchitectureCheck.ErrorString);
                Console.WriteLine("Press any key to exit application");
                Console.Read();
            }
        }
    }
}
