﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Binance.WebSocket;
using Moq;
using Xunit;

namespace Binance.Tests.Api.WebSocket
{
    public class TradesWebSocketClientTest
    {
        [Fact]
        public async Task StreamThrows()
        {
            var client = new AggregateTradeWebSocketClient(new Mock<IWebSocketStream>().Object);

            using (var cts = new CancellationTokenSource())
            {
                await Assert.ThrowsAsync<ArgumentNullException>("symbol", () => client.SubscribeAndStreamAsync(null, cts.Token));
            }
        }
    }
}
