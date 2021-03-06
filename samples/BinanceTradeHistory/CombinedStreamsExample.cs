﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Binance;
using Binance.Application;
using Binance.Cache;
using Binance.Market;
using Binance.Utility;
using Binance.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// ReSharper disable AccessToDisposedClosure

namespace BinanceTradeHistory
{
    /// <summary>
    /// Demonstrate how to monitor aggregate trades for multiple symbols
    /// and how to unsubscribe/subscribe a symbol after streaming begins.
    /// </summary>
    internal class CombinedStreamsExample
    {
        public static async Task ExampleMain()
        {
            try
            {
                // Load configuration.
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", true, false)
                    .Build();

                // Configure services.
                var services = new ServiceCollection()
                    .AddBinance()
                    .AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace))
                    .BuildServiceProvider();

                // Configure logging.
                services.GetService<ILoggerFactory>()
                    .AddFile(configuration.GetSection("Logging:File"));

                // Get configuration settings.
                var symbols = configuration.GetSection("CombinedStreamsExample:Symbols").Get<string[]>()
                    ?? new string[] { Symbol.BTC_USDT };

                var client = services.GetService<IAggregateTradeWebSocketClient>();

                using (var controller = new RetryTaskController())
                {
                    if (symbols.Length == 1)
                    {
                        // Monitor latest aggregate trade for a symbol and display.
                        controller.Begin(
                            tkn => client.SubscribeAndStreamAsync(symbols[0], evt => Display(evt.Trade), tkn),
                            err => Console.WriteLine(err.Message));
                    }
                    else
                    {
                        // Alternative usage (combined streams).
                        client.AggregateTrade += (s, evt) => { Display(evt.Trade); };

                        // Subscribe to all symbols.
                        foreach (var symbol in symbols)
                        {
                            client.Subscribe(symbol); // using event instead of callbacks.
                        }

                        // Begin streaming.
                        controller.Begin(
                            tkn => client.StreamAsync(tkn),
                            err => Console.WriteLine(err.Message));
                    }

                    message = "...press any key to continue.";
                    Console.ReadKey(true); // wait for user input.

                    //*//////////////////////////////////////////////////
                    // Example: Unsubscribe/Subscribe after streaming...

                    // Cancel streaming.
                    await controller.CancelAsync();
                    
                    // Unsubscribe a symbol.
                    client.Unsubscribe(symbols[0]);
                    
                    // Remove unsubscribed symbol and clear display (application specific).
                    _trades.Remove(symbols[0]);
                    Console.Clear();

                    // Subscribe to the real Bitcoin :D
                    client.Subscribe(Symbol.BCH_USDT); // a.k.a. BCC.

                    // Begin streaming again.
                    controller.Begin(
                        tkn => client.StreamAsync(tkn),
                        err => Console.WriteLine(err.Message));

                    message = "...press any key to exit.";
                    Console.ReadKey(true); // wait for user input.
                    ///////////////////////////////////////////////////*/
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine();
                Console.WriteLine("  ...press any key to close window.");
                Console.ReadKey(true);
            }
        }

        private static string message;

        private static readonly object _sync = new object();

        private static IDictionary<string, AggregateTrade> _trades
            = new SortedDictionary<string, AggregateTrade>();

        private static Task _displayTask = Task.CompletedTask;

        private static void Display(AggregateTrade trade)
        {
            lock (_sync)
            {
                _trades[trade.Symbol] = trade;

                if (_displayTask.IsCompleted)
                {
                    // Delay to allow multiple data updates between display updates.
                    _displayTask = Task.Delay(100)
                        .ContinueWith(_ =>
                        {
                            AggregateTrade[] latestsTrades;
                            lock (_sync)
                            {
                                latestsTrades = _trades.Values.ToArray();
                            }

                            Console.SetCursorPosition(0, 0);

                            foreach (var t in latestsTrades)
                            {
                                Console.WriteLine($" {t.Time.ToLocalTime()} - {t.Symbol.PadLeft(8)} - {(t.IsBuyerMaker ? "Sell" : "Buy").PadLeft(4)} - {t.Quantity:0.00000000} @ {t.Price:0.00000000}{(t.IsBestPriceMatch ? "*" : " ")} - [ID: {t.Id}] - {t.Time.ToTimestamp()}".PadRight(119));
                                Console.WriteLine();
                            }

                            Console.WriteLine(message);
                        });
                }
            }
        }
    }
}
