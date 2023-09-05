using System;
using System.Collections.Generic;
using System.Text;

namespace Server
{
    /// <summary>
    /// The server that receives and echo's (sends) the messages to the other client using UDP protocol (ipAdress and port).
    /// </summary>
    public class BasicServer
    {   
        // Expected number of players
        int nPlayers = 2;
        // Players that are connected to the server
        int connectedPlayers = 0;
        // Begin Port number 
        int receivePort = 4000;
        // Offset for if the Begin Port number is already taken
        int sendPortOffset = 100;
        // IpAdress for connection
        string ipAddress = "127.0.0.1";

        enum State
        {
            Connecting,            
            Started
        }
        State state = State.Connecting;

        // Declare an array of UDP Server connections, to hold the connections for each player.
        QuicknBriteUdpServer[] connection;

        public BasicServer()
        {   
            // Initialize the Udp Server for the number of players
            connection = new QuicknBriteUdpServer[nPlayers];
            
            // Foreach player create a new Udp Server Protocol and start the connection with the ipAdress, Port and Offsets.
            for (int i = 0; i < nPlayers; i++)
            {
                connection[i] = new QuicknBriteUdpServer();
                connection[i].StartConnection(ipAddress, receivePort + sendPortOffset + i, receivePort + i, this);
            }            
        }

        /// <summary>
        /// HandleMessage is a method to handle the incoming messages that the server receives.
        /// This includes echoing (sending) incoming data from one client to the other.
        /// </summary>
        /// <param name="port">Port is used for sending messages to each client</param>
        /// <param name="receiveBytes">receiveBytes is used for echoing (sending) incoming data from one client to the other</param>
        public void HandleMessage(int port, byte[] receiveBytes)
        {
            // Convert the receivedBytes into a string to be able to use if statements depending on contents of the messages.
            string returnData = Encoding.UTF8.GetString(receiveBytes);
            
            // Switch statement to change behaviour depending on if the players are connecting or if the game has started.
            switch (state)
            {
                // If a client has sent a join request to the server.
                case State.Connecting:
                    // Add one connected player
                    connectedPlayers++;
                    Console.WriteLine("Client Connected on port " + port);
                    // When two players are connected switch to the Started state
                    // And send a message with START to both connected clients.
                    if (connectedPlayers == 2)
                    {
                        state = State.Started;
                        // START both clients
                        for (int i = 0; i < nPlayers; i++)
                        {
                            connection[i].SendString("START");
                        }
                        Console.WriteLine("Starting both clients..");
                    }
                    break;
                // If the game has started and the server receives a message from one of the clients.
                case State.Started:
                    // If the server receives a UpdatePaddlePosition message send it to all connected clients, except the one who send it to the server
                    if (returnData.Contains("UPDATE_POS"))
                        SendAllExceptPort(port, receiveBytes);
                    // If the server receives a NoPaddlePositionChange message send it to all contected clients, except the one who send it to the server
                    else if (returnData.Contains("NO_CHANGE"))
                    {
                        SendAllExceptPort(port, receiveBytes);
                    // If the server receives a PaddleCollisionHit message send it to all contected clients, except the one who send it to the server
                    } else if (returnData.Contains("PADDLE_HIT"))
                    {
                        SendAllExceptPort(port, receiveBytes);
                    }
                    break;
            }         
        }

        /// <summary>
        /// Echo a message to all connected ports/clients, except the one who send it to the server.
        /// </summary>
        /// <param name="incPort">incPort is the port that has send the message to the server</param>
        /// <param name="receiveBytes">receiveBytes is the message data in Bytes format, it is send to the other clients, except the one who send it to the server </param>
        public void SendAllExceptPort(int incPort, byte[] receiveBytes) {
            for (int i = 0; i < nPlayers; i++)
            {
                // Cycle out the port that has send the message to the server
                if (i != (incPort - receivePort))
                {
                    // Echo/Send the messages to the other connected clients
                    connection[i].SendBytes(receiveBytes);
                }
            }
        }
    }
}
