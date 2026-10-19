using System.Net;
using System.Net.Sockets;
using Utils;
using System.Threading;
using System;
using System.Collections.Generic;

namespace distributedAuction.Server
{
    public class Server
    {
        private const int SOCKET_TIMEOUT = 30000;
        private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(0);
        private static readonly Mutex mutex = new Mutex();

        public static void Main()
        {
            int nBidders = 0;

            Console.WriteLine("Insert the name of the article: ");
            string articleName = Console.ReadLine()!;

            double basePrice = InputValidation.ValidateDoubleGT0("Insert the base price of the auction: ");

            int maxAuctionDuration = InputValidation.ValidateIntGT0("Insert the max duration of the auction: ");

            AuctionState auction = new AuctionState(articleName, basePrice, maxAuctionDuration);

            int portNumber = InputValidation.ValidateIntBetween("Enter the port number on which the server will listen [1024, 65535]: ", 1024, 65535);

            TcpListener serverSocket = new TcpListener(IPAddress.Any, portNumber);
            List<Thread> threads = new List<Thread>();

            try
            {
                serverSocket.Start();
                Console.WriteLine($"\nServer: Listening on port {portNumber}.");
                Console.WriteLine($"Server: Accepting participants for {SOCKET_TIMEOUT / 1000} seconds...\n");

                CancellationTokenSource cts = new CancellationTokenSource(SOCKET_TIMEOUT);

                while (true)
                {
                    try
                    {
                        TcpClient clientSocket = serverSocket.AcceptTcpClientAsync(cts.Token).GetAwaiter().GetResult();
                        nBidders++;

                        Console.WriteLine($"Server: New connection received. Assigning to ServerThread-{nBidders}.");

                        ServerThread serverThread = new ServerThread(clientSocket, auction, semaphore, mutex, cts.Token);

                        Thread thread = new Thread(serverThread.Run);
                        thread.Name = "ServerThread-" + nBidders;
                        thread.IsBackground = true;

                        threads.Add(thread);
                        thread.Start();
                    }
                    catch (OperationCanceledException)
                    {
                        Console.WriteLine("\nServer: Time to accept new bidders has ended.");

                        int validatedPlayers;
                        mutex.WaitOne();
                        try
                        {
                            validatedPlayers = auction.ActiveParticipants.Count;
                        }
                        finally
                        {
                            mutex.ReleaseMutex();
                        }

                        if (validatedPlayers > 0)
                        {
                            Console.WriteLine($"Server: Releasing {validatedPlayers} validated participant(s) into the auction.");
                            semaphore.Release(validatedPlayers);
                        }
                        else
                        {
                            Console.WriteLine("Server: No participants validated in time. Auction cancelled.");
                            return;
                        }
                        break;
                    }
                }

                auction.isStarted = true;
                Console.WriteLine($"Server: THE AUCTION FOR '{auction.ItemName}' HAS STARTED!");

                Thread.Sleep(auction.MaxDuration * 1000);

                auction.isEnded = true;
                Console.WriteLine("\nServer: AUCTION TIME HAS ENDED!");
                if (string.IsNullOrEmpty(auction.HighestBidder))
                {
                    Console.WriteLine("Result: The item was not sold.");
                }
                else
                {
                    Console.WriteLine($"Result: The winner is '{auction.HighestBidder}' with a final bid of {auction.HighestBid} euros.");
                }

                Console.WriteLine("Server: Waiting for client threads to finish...");
                foreach (Thread t in threads)
                {
                    t.Join(TimeSpan.FromSeconds(2));
                }

                Console.WriteLine("Server: All client threads closed.");
            }
            catch (SocketException)
            {
                Console.Error.WriteLine($"Server: An I/O error occurred while trying to create the socket on port {portNumber}");
                Environment.Exit(1);
            }
            catch (IOException ex)
            {
                Console.Error.WriteLine($"Server: I/O error - {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Server: Unexpected error - {ex.Message}");
            }
            finally
            {
                serverSocket.Stop();
            }
        }
    }
}