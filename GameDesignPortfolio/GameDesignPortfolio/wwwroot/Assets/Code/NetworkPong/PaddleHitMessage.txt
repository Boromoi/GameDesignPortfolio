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
    /// PaddleHitMessage is a message that get's send to the server when the paddle has collision with the clients paddle.
    /// </summary>
    public class PaddleHitMessage : MessagePacket
    {
        // Variable to differentiate between the ticks(time) the message was sent and when it arrives at the other client.
        public int tickNumber;

        // Variable's to sent the position of the paddle and ball
        public Vector2 paddlePosition;
        public Vector2 ballPosition;

        // Variable to sent the last Direction the ball was going to, this is needed to emulate the bounce on the other client.
        public Vector2 ballDirection;

        // msgType is used to check which message is being received on the client. It is inherited from MessagePacket.
        public PaddleHitMessage()
        {
            msgType = "PADDLE_HIT";
        }
    }
}
