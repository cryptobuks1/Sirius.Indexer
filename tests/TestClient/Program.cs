﻿using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Swisschain.Sirius.Indexer.ApiClient;
using Swisschain.Sirius.Indexer.ApiContract;

namespace TestClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Press enter to start");
            Console.ReadLine();
            var client = new IndexerClient("http://localhost:5001");

            while (true)
            {
                try
                {
                    var sw = new Stopwatch();
                    sw.Start();
                    var result = await client.Monitoring.IsAliveAsync(new IsAliveRequest());

                    var guidv1 = Guid.NewGuid();
                    var guid = Guid.NewGuid().ToString();
                    await client.ObservedOperations.AddObservedOperationAsync(new AddObservedOperationRequest()
                    {
                        BlockchainId = "bitcoin-regtest",
                        OperationId = 1,
                        RequestId = "Fake-"+ guid,
                        TransactionId = guid,
                        Fees = {},
                        Bilv1OperationId = guidv1.ToString(),
                        OperationAmount = 1.0m,
                        DestinationAddress = "0x8BB7d9A81Ff3e08F59A83CE00Ecd45E079e80261",
                        AssetId = 100_000,
                    });

                    sw.Stop();
                    Console.WriteLine($"{result.Name}  {sw.ElapsedMilliseconds} ms");
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                Thread.Sleep(1000);
            }
        }
    }
}
