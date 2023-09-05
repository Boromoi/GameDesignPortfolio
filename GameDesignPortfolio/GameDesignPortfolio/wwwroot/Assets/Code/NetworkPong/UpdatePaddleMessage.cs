using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace Networking.JsonObjects
{
    /// <summary>
    /// The Client sents a message containing data in bytes to the server.
    /// The server Echo's the data to other clients.
    /// The other client converts the bytes back into data.
    /// UpdatePaddleMessage is a message that get's send to the server when the clients paddle has changed in direction.
    /// </summary>
    public class UpdatePaddleMessage : MessagePacket
    {
        // Variable to differentiate between the ticks(time) the message was sent and when it arrives at the other client.
        public int tickNumber;

        // Variable to sent the PaddlePosition
        public Vector2 position;

        // Variable for the other client to update the direction the enemies paddle was moving.
        // This holds the direction the paddle was moving into.
        public string direction;

        // msgType is used to check which message is being received on the client. It is inherited from MessagePacket.
        public UpdatePaddleMessage() 
        {
            msgType = "UPDATE_POS";        
        }
    }
}
