using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Globalization;

namespace distributedAuction.Server
{
    internal class ServerThread
    {
        private readonly TcpClient clientSocket;
        private readonly SemaphoreSlim semaphore;
        private readonly Mutex mutex;
        private readonly AuctionState auction;
        private readonly CancellationToken cancellationToken;

        public ServerThread(TcpClient clientSocket, AuctionState auction, SemaphoreSlim semaphore, Mutex mutex, CancellationToken cancelToken)
        {
            this.clientSocket = clientSocket;
            this.semaphore = semaphore;
            this.auction = auction;
            this.mutex = mutex;
            this.cancellationToken = cancelToken;
        }

        public void Run()
        {
            string clientName = "Unknown";
            try
            {
                NetworkStream stream = clientSocket.GetStream();
                StreamWriter outWriter = new StreamWriter(stream) { AutoFlush = true };
                StreamReader inReader = new StreamReader(stream);

                bool nameAccepted = false;
                string request;

                using (cancellationToken.Register(clientSocket.Close))
                {
                    while (!nameAccepted)
                    {
                        clientName = inReader.ReadLine()!;
                        if (string.IsNullOrEmpty(clientName)) return;

                        mutex.WaitOne();
                        try
                        {
                            if (auction.ActiveParticipants.Contains(clientName))
                            {
                                outWriter.WriteLine("REJECTED_NAME");
                                Console.WriteLine($"{Thread.CurrentThread.Name}: The name '{clientName}' was rejected (already in use).");
                            }
                            else
                            {
                                auction.ActiveParticipants.Add(clientName);
                                outWriter.WriteLine("ACCEPTED_NAME");
                                nameAccepted = true;
                                Console.WriteLine($"{Thread.CurrentThread.Name}: Participant '{clientName}' successfully registered.");
                            }
                        }
                        finally
                        {
                            mutex.ReleaseMutex();
                        }
                    }
                }

                outWriter.WriteLine("WAITING");
                Console.WriteLine($"{Thread.CurrentThread.Name}: Participant '{clientName}' is waiting for the auction to start.");

                semaphore.Wait();

                outWriter.WriteLine("START");
                outWriter.WriteLine(auction.ItemName);
                outWriter.WriteLine(auction.BasePrice);
                outWriter.WriteLine(auction.MaxDuration);
                Console.WriteLine($"{Thread.CurrentThread.Name}: Notified '{clientName}' that the auction has started.");

                while ((request = inReader.ReadLine()!) != null)
                {
                    if (auction.isEnded)
                    {
                        string winner;
                        double finalBid;
                        mutex.WaitOne();
                        try
                        {
                            winner = string.IsNullOrEmpty(auction.HighestBidder) ? "NONE" : auction.HighestBidder;
                            finalBid = auction.HighestBid;
                        }
                        finally
                        {
                            mutex.ReleaseMutex();
                        }

                        outWriter.WriteLine("END");
                        outWriter.WriteLine(winner);
                        outWriter.WriteLine(finalBid);
                        Console.WriteLine($"{Thread.CurrentThread.Name}: Sent auction results to '{clientName}'.");
                        break;
                    }

                    string command = request.Trim().ToUpper();

                    switch (command)
                    {
                        case "BID":
                            string? bidText = inReader.ReadLine();
                            if (bidText != null && double.TryParse(bidText, out double bidValue))
                            {
                                mutex.WaitOne();
                                try
                                {
                                    bool isFirstBid = string.IsNullOrEmpty(auction.HighestBidder);

                                    bool isValid = isFirstBid ? (bidValue >= auction.BasePrice) : (bidValue > auction.HighestBid);

                                    if (isValid)
                                    {
                                        auction.HighestBid = bidValue;
                                        auction.HighestBidder = clientName;

                                        outWriter.WriteLine("ACCEPTED");
                                        outWriter.WriteLine(auction.HighestBid);

                                        Console.WriteLine($"{Thread.CurrentThread.Name}: '{clientName}' placed a bid of {bidValue}");
                                    }
                                    else
                                    {
                                        outWriter.WriteLine("REJECTED");
                                        outWriter.WriteLine(auction.HighestBid);

                                        Console.WriteLine($"{Thread.CurrentThread.Name}: Invalid value for the bid from '{clientName}'. Current highest: {auction.HighestBid}");
                                    }
                                }
                                finally
                                {
                                    mutex.ReleaseMutex();
                                }
                            }
                            else
                            {
                                mutex.WaitOne();
                                try
                                {
                                    outWriter.WriteLine("REJECTED");
                                    outWriter.WriteLine(auction.HighestBid);
                                }
                                finally
                                {
                                    mutex.ReleaseMutex();
                                }
                            }
                            break;

                        case "STATUS":
                            mutex.WaitOne();
                            try
                            {
                                string leader = string.IsNullOrEmpty(auction.HighestBidder) ? "NONE" : auction.HighestBidder;

                                outWriter.WriteLine("STATUS");
                                outWriter.WriteLine(auction.HighestBid);
                                outWriter.WriteLine(leader);
                                
                                Console.WriteLine($"{Thread.CurrentThread.Name}: '{clientName}' requested auction status.");
                            }
                            finally
                            {
                                mutex.ReleaseMutex();
                            }
                            break;

                        case "EXIT":
                            outWriter.WriteLine("DISCONNECTED");
                            Console.WriteLine($"{Thread.CurrentThread.Name}: Participant '{clientName}' chose to leave the auction early.");
                            return;
                    }
                }
            }
            catch (SocketException)
            {
                Console.Error.WriteLine($"{Thread.CurrentThread.Name} ({clientName}): Socket error");
            }
            catch (IOException)
            {
                Console.Error.WriteLine($"{Thread.CurrentThread.Name} ({clientName}): Connection lost or client disconnected");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"{Thread.CurrentThread.Name} ({clientName}): Unexpected disconnection - {ex.Message}");
            }
            finally
            {
                Console.WriteLine($"{Thread.CurrentThread.Name}: Closing connection for '{clientName}'.");
                clientSocket.Close();
            }
        }
    }
}