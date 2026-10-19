using System;
using System.IO;
using System.Net.Sockets;
using Utils;

namespace distributedAuction.Client
{
    public class Client
    {
        public static void Main()
        {
            Console.Write("Enter the hostname of the server you want to connect to: ");
            string hostname = Console.ReadLine()!;

            int portNumber = InputValidation.ValidateIntBetween("Enter the port number on which the server is listening [1024, 65535]: ", 1024, 65535);

            try
            {
                TcpClient clientSocket = new TcpClient(hostname, portNumber);
                NetworkStream stream = clientSocket.GetStream();
                StreamWriter outWriter = new StreamWriter(stream) { AutoFlush = true };
                StreamReader inReader = new StreamReader(stream);

                string currentBid;
                string leader;

                bool nameAccepted = false;
                while (!nameAccepted)
                {
                    Console.Write("\nEnter your username: ");
                    string name = Console.ReadLine()!;
                    outWriter.WriteLine(name);

                    string response = inReader.ReadLine()!;

                    if (response == "ACCEPTED_NAME")
                    {
                        Console.WriteLine("Username accepted.");
                        nameAccepted = true;
                    }
                    else if (response == "REJECTED_NAME")
                    {
                        Console.WriteLine("Error: That username is already in use. Please try another one.");
                    }
                }

                string? waitingMsg = inReader.ReadLine();
                if (waitingMsg == "WAITING")
                {
                    Console.WriteLine("Waiting for the auction to start. Please hold...");
                }
                else
                {
                    Console.WriteLine("Failed to enter auction (connection closed or timed out).");
                    return;
                }

                string? startMsg = inReader.ReadLine();
                if (startMsg == "START")
                {
                    string itemName = inReader.ReadLine()!;
                    string basePrice = inReader.ReadLine()!;
                    string duration = inReader.ReadLine()!;
                    
                    Console.WriteLine("\nTHE AUCTION HAS STARTED!");
                    Console.WriteLine($" Item: {itemName}");
                    Console.WriteLine($" Base Price: {basePrice} euros");
                    Console.WriteLine($" Duration: {duration} seconds");
                }
                else
                {
                    Console.WriteLine("Auction did not start.");
                    return;
                }

                while (true)
                {
                    Console.WriteLine("\nOptions: 1-Bid | 2-Status | 3-Exit");
                    Console.Write("Choose an option: ");
                    string option = Console.ReadLine()!;

                    if (option == "1")
                    {
                        double bid = InputValidation.ValidateDoubleGT0("Enter your bid amount: ");
                        outWriter.WriteLine("BID");
                        outWriter.WriteLine(bid);
                    }
                    else if (option == "2")
                    {
                        outWriter.WriteLine("STATUS");
                    }
                    else if (option == "3")
                    {
                        outWriter.WriteLine("EXIT");
                    }
                    else
                    {
                        Console.WriteLine("Invalid option.");
                        continue;
                    }

                    string serverResponse = inReader.ReadLine()!;
                    if (serverResponse == null) break;

                    switch (serverResponse)
                    {
                        case "ACCEPTED":
                            currentBid = inReader.ReadLine()!;
                            Console.WriteLine($"Bid accepted! You hold the highest bid: {currentBid} euros.");
                            break;
                        case "REJECTED":
                            currentBid = inReader.ReadLine()!;
                            Console.WriteLine($"Bid REJECTED! The current highest bid is {currentBid} euros.");
                            break;
                        case "STATUS":
                            currentBid = inReader.ReadLine()!;
                            leader = inReader.ReadLine()!;

                            if (leader == "NONE")
                            {
                                Console.WriteLine($"Status: Base price is {currentBid} euros.");
                            }
                            else
                            {
                                Console.WriteLine($"Status: Current highest bid is {currentBid} euros, from {leader}");
                            }
                            break;
                        case "DISCONNECTED":
                            Console.WriteLine("You left the auction.");
                            return;
                        case "END":
                            leader = inReader.ReadLine()!;
                            currentBid = inReader.ReadLine()!;

                            Console.WriteLine("THE AUCTION HAS ENDED!");
                            if (leader == "NONE")
                            {
                                Console.WriteLine("Result: The item was not sold.");
                            }
                            else
                            {
                                Console.WriteLine($"Winner: {leader} with a bid of {currentBid} euros");
                            }
                            return;
                    }
                }
            }
            catch (SocketException)
            {
                Console.Error.WriteLine("\nUnknown host or connection refused: " + hostname);
                Console.WriteLine("Press a key to finish");
                Console.ReadKey();
                Environment.Exit(2);
            }
            catch (IOException)
            {
                Console.Error.WriteLine("\nI/O error. The server might have closed the connection.");
                Console.WriteLine("Press a key to finish");
                Console.ReadKey();
                Environment.Exit(3);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"\nUnexpected error: {ex.Message}");
            }

            Console.WriteLine("\nPress any key to finish...");
            Console.ReadKey();
        }
    }
}