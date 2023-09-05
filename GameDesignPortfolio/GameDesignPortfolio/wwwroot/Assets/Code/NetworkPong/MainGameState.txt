using Client.GameObjects;
using Microsoft.VisualBasic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using Networking.JsonObjects;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace GameStates
{
    public class MainGameState : GameObjectList
    {
        // Variable to give the player left paddle or right paddle when starting game
        private int myPlayerId;
        // Paddle Variables
        private Paddle leftPaddle, rightPaddle, myPaddle, theirPaddle;
        // Marker to show if you are right or left player
        private Arrow marker;
        private Ball ball;

        // Text that will display your client's tick counter
        TextGameObject tickCounterText;

        // Messages to send info in bytes to the other client using the server to echo the messages
        UpdatePaddleMessage updatePaddleMessage;
        NoChangeMessage noChangeMessage;
        PaddleHitMessage paddleHitMessage;

        // Game1 is the Gamemanager managing the game state (lobby, playing, connecting)
        BaseProject.Game1 main;

        // Integer to hold your count your client's ticks
        int tickCounter = 0;

        // Check if this client changed directions, used for sending smaller messages when possible.
        private bool changeInDirection = false;

        // Variables to hold the direction this client is moving in. And the last direction this client moved in.
        private string myPaddleDirection;
        private string lastDirection;

        // Paddle and ball movement speed
        Vector2 paddleMovementSpeed = new Vector2(0, 10);
        private int ballSpeed = 2;

        // Paddle, Ball and Tickcounter starting positions
        Vector2 leftPaddleStartPosition = new Vector2(100 - 12, 100);
        Vector2 rightPaddleStartPosition = new Vector2(700 - 12, 100);
        int ballStartPositionX = 400;
        int ballStartPositionY = 400;
        Vector2 tickCounterStartPosition = new Vector2(350, 30);

        //variables used for checking dropped frames..
        Stopwatch stopWatch;
        readonly double accurateMs = 1000 / 60.0;
        readonly int MAX_ATTEMPTS = 3;
        //--------------------------------------------

        /// <summary>
        /// Initialize the GameManager and the in game objects
        /// </summary>
        /// <param name="mainGame">mainGame is where the server and the clients connect to send messages using UDP protocol</param>
        public MainGameState(BaseProject.Game1 mainGame)
        {
            // Make this main an instance of mainGame
            main = mainGame;

            // Initializing the Paddles, Ball and PlayerMarker
            leftPaddle = new Paddle(leftPaddleStartPosition);
            rightPaddle = new Paddle(rightPaddleStartPosition);
            marker = new Arrow(new Vector2());
            ball = new Ball(ballStartPositionX, ballStartPositionY, ballSpeed, -ballSpeed);

            // Adding the Initialized objects to the scene
            Add(leftPaddle);
            Add(rightPaddle);
            Add(marker);
            Add(ball);

            // Initializing and adding tickCounterText to the scene
            tickCounterText = new TextGameObject("Spritefont");
            tickCounterText.Text = "sds";
            tickCounterText.Position = tickCounterStartPosition;
            Add(tickCounterText);

            // Initializing all different message types
            updatePaddleMessage = new UpdatePaddleMessage() { tickNumber = tickCounter, direction = myPaddleDirection };
            noChangeMessage = new NoChangeMessage() { tickNumber = tickCounter, direction = myPaddleDirection };
            paddleHitMessage = new PaddleHitMessage();
        }

        /// <summary>
        /// Start the game and give the players the left or right paddle depending on their ID.
        /// </summary>
        /// <param name="playerId">playerId</param>
        public void StartGame(int playerId)
        {
            Debug.WriteLine("My ID is " + playerId);
            myPlayerId = playerId;

            if (myPlayerId == 0) //player left side
            {
                myPaddle = leftPaddle;
                theirPaddle = rightPaddle;
            }
            else //player right side
            {
                myPaddle = rightPaddle;
                theirPaddle = leftPaddle;
            }

            //Set the Marker at the right spot for my player position!
            marker.Position = new Vector2(70 + myPlayerId * 600, 10);

            //Start a Stopwatch (necesary to check for frame drops)
            stopWatch = new Stopwatch();
            stopWatch.Start();
        }

        /// <summary>
        /// Tick() is the main method for handling game events, physics, etc.
        /// </summary>
        private void Tick()
        {
            // Update tick
            tickCounter++;
            tickCounterText.Text = "frame tick: " + tickCounter;

            // Keep the other clients paddle moving in the lastDirection received from the last message if no new moving direction msg is being received.
            if (lastDirection == "Up")
            {
                theirPaddle.Position -= paddleMovementSpeed;
            }
            else if (lastDirection == "Down") 
            {
                theirPaddle.Position += paddleMovementSpeed;
            }

            // Ball collisions with Left and Right walls       
            if (ball.Position.X <= 0 || ball.Position.X >= 800 - ball.size)
            {
                ball.BounceHorizontal();
            }
            // Ball collisions with Top and Bottom walls 
            if (ball.Position.Y <= 0 || ball.Position.Y >= 600 - ball.size)
            {
                ball.BounceVertical();
            }

            // Make the ball bounce if it collides with one of the paddles
            if (ball.CollidesWith(leftPaddle) || ball.CollidesWith(rightPaddle))
            {
                ball.BounceHorizontal();
            }

            // Send the paddleHitMessage to the other client using the server if myPaddle collides with the ball.
            if (ball.CollidesWith(myPaddle))
            {
                // Set the message info
                paddleHitMessage.tickNumber = tickCounter;

                paddleHitMessage.paddlePosition = myPaddle.Position;

                paddleHitMessage.ballPosition = ball.Position;
                paddleHitMessage.ballDirection.X = ball.vx;
                paddleHitMessage.ballDirection.Y = ball.vy;

                // Send the paddleHitMessage
                main.SendObject(paddleHitMessage);
            }

            // Update ball (nb: DON'T replace this with MonoGame's Update; messes up the determinism of frames)
            ball.Tick();
        }


        /// <summary>
        /// Use HandleInput for all the code when 'pressing keyboard buttons'
        /// </summary>
        /// <param name="inputHelper">Check keys on keyboard to check if they are pressed or not</param>
        public override void HandleInput(InputHelper inputHelper)
        {
            // Refrence to the base HandleInput to let the inputHelper work.
            base.HandleInput(inputHelper);

            // If the W key is pressed Move Up
            // Also send a message telling the server you are Moving Up
            if (inputHelper.IsKeyDown(Keys.W))
            {
                MoveUp(inputHelper);
            }
            // If the S key is pressed Move Down
            // Also send a message telling the server you are Moving Down
            else if (inputHelper.IsKeyDown(Keys.S))
            {
                MoveDown(inputHelper);
            }
            // If No key is pressed down Don't Move
            // Also send a message telling the server you are Not Moving
            else
            {
                NoInput(inputHelper);
            }

            void NoInput(InputHelper inputHelper)
            {
                // Check if the direction changes
                CheckForChangedInput(inputHelper);

                // Update the position by setting the velocity to Zero to stop moving
                myPaddle.Velocity = Vector2.Zero;

                // Set the direction to no direction if there isn't any input
                myPaddleDirection = "NoDirection";

                // Sent a message based on the changed/unchanged direction 
                if (changeInDirection == true)
                {
                    // Sent a Change message if the direction has been changed 
                    SendChangeMessageAfterInput();
                }
                else if (changeInDirection == false)
                {
                    // Sent a noChangeMessage if the direction has not been changed
                    SendNoChangeMessageAfterInput();
                }
            }

            void MoveDown(InputHelper inputHelper)
            {
                // Check if the direction changes
                CheckForChangedInput(inputHelper);

                // Update the position based on the input
                myPaddle.Position += paddleMovementSpeed;

                // Set the new direction to down after pressing S
                myPaddleDirection = "Down";

                // Sent a message based on the changed/unchanged direction 
                if (changeInDirection == true)
                {
                    // Sent a Change message if the direction has been changed 
                    SendChangeMessageAfterInput();
                }
                else if (changeInDirection == false)
                {
                    // Sent a noChangeMessage if the direction has not been changed
                    SendNoChangeMessageAfterInput();
                }
            }

            void MoveUp(InputHelper inputHelper)
            {
                // Check if the direction changes
                CheckForChangedInput(inputHelper);

                // Update the position based on the input
                myPaddle.Position -= paddleMovementSpeed;

                // Set the new direction to Up after pressing W
                myPaddleDirection = "Up";

                // Sent different message based on the changed/unchanged direction 
                if (changeInDirection == true)
                {
                    // Sent a Change message if the direction has been changed 
                    SendChangeMessageAfterInput();
                }
                else if (changeInDirection == false)
                {
                    // Sent a noChangeMessage if the direction has not been changed
                    SendNoChangeMessageAfterInput();
                }
            }
        }

        /// <summary>
        /// Method to check if the direction of the paddle changes based on the input
        /// Sets the bool changeInDirection to true if the direction changes, or false if the direction doesn't change
        /// Also sets the new direction of the paddle
        /// </summary>
        /// <param name="inputHelper">Check keys on keyboard to check if they are pressed or not</param>
        private void CheckForChangedInput(InputHelper inputHelper)
        {
            // There is a change in direction when paddle is stopped and starts moving
            if (myPaddleDirection == "NoDirection")
            {
                CheckForChangedInputNoDirection(inputHelper);
            }

            // There is a change in direction when paddle is moving up and receives input to go down or stops
            if (myPaddleDirection == "Up")
            {
                CheckForChangedInputUpDirection(inputHelper);
            }

            // There is a change in direction when paddle is moving down and receives input to go up or stops
            if (myPaddleDirection == "Down")
            {
                CheckForChangedInputDownDirection(inputHelper);
            }
        }

        /// <summary>
        /// Method to check if the direction of the paddle changes if the paddle direction was Down.
        /// Sets the bool changeInDirection to true if the direction changes, or false if the direction doesn't change.
        /// Also sets the new direction of the paddle.
        /// </summary>
        /// <param name="inputHelper">Check keys on keyboard to check if they are pressed or not</param>
        private void CheckForChangedInputDownDirection(InputHelper inputHelper)
        {
            if (inputHelper.KeyPressed(Keys.W))
            {
                changeInDirection = true;
                myPaddleDirection = "Up";
            }
            if (myPaddle.Velocity == Vector2.Zero)
            {
                changeInDirection = true;
                myPaddleDirection = "NoDirection";
            }
            else
            {
                changeInDirection = false;
            }
        }

        /// <summary>
        /// Method to check if the direction of the paddle changes if the paddle direction was Up.
        /// Sets the bool changeInDirection to true if the direction changes, or false if the direction doesn't change.
        /// Also sets the new direction of the paddle.
        /// </summary>
        /// <param name="inputHelper">Check keys on keyboard to check if they are pressed or not</param>
        private void CheckForChangedInputUpDirection(InputHelper inputHelper)
        {
            if (inputHelper.KeyPressed(Keys.S))
            {
                changeInDirection = true;
                myPaddleDirection = "Down";
            }
            if (myPaddle.Velocity == Vector2.Zero)
            {
                changeInDirection = true;
                myPaddleDirection = "NoDirection";
            }
            else
            {
                changeInDirection = false;
            }
        }

        /// <summary>
        /// Method to check if the direction of the paddle changes if the paddle direction was Nothing.
        /// Sets the bool changeInDirection to true if the direction changes, or false if the direction doesn't change.
        /// Also sets the new direction of the paddle.
        /// </summary>
        /// <param name="inputHelper">Check keys on keyboard to check if they are pressed or not</param>
        private void CheckForChangedInputNoDirection(InputHelper inputHelper)
        {
            if (inputHelper.KeyPressed(Keys.W))
            {
                changeInDirection = true;
                myPaddleDirection = "Up";
            }
            if (inputHelper.KeyPressed(Keys.S))
            {
                changeInDirection = true;
                myPaddleDirection = "Down";
            }
            else
            {
                changeInDirection = false;
            }
        }

        /// <summary>
        /// Method that sends the updatePaddleMessage to update the paddle
        /// Gets called when bool changeInDirection returns true to prevent sending unnecesary messages
        /// </summary>
        private void SendChangeMessageAfterInput()
        {
            updatePaddleMessage.position = myPaddle.Position;
            updatePaddleMessage.direction = myPaddleDirection;
            updatePaddleMessage.tickNumber = tickCounter;

            main.SendObject(updatePaddleMessage);
        }

        /// <summary>
        /// Method that sends a noChangeMessage
        /// Gets called when bool changeInDirection returns false 
        /// </summary>
        private void SendNoChangeMessageAfterInput()
        {
            noChangeMessage.direction = myPaddleDirection;
            noChangeMessage.tickNumber = tickCounter;

            main.SendObject(noChangeMessage);
        }

        /// <summary>
        /// HandleMessage is called when a network-message is received from the server
        /// </summary>
        public void HandleMessage(byte[] receiveBytes)
        {
            // Changing the receivedBytes into a string so that it can get checked in if statements
            string returnData = Encoding.UTF8.GetString(receiveBytes);

            // When the Other paddle moved receive a UpdatePaddleMessage from the server
            if (returnData.Contains("UPDATE_POS"))
            {
                UpdatePositionMessageReceived(returnData);
            }

            // When the other paddle didn't change direction receive a NoChangeMessage
            if (returnData.Contains("NO_CHANGE"))
            {
                NoChangeMessageReceived(returnData);
            }

            // Receive a PaddleHit message when the other client collided with the ball
            if (returnData.Contains("PADDLE_HIT"))
            {
                PaddleHitMessageReceived(returnData);
            }

            /// <summary>
            /// Handles the data received from the UpdatePositionMessage to correct their paddle position 
            /// And when needed extrapolate their paddle back into correct position.
            /// </summary>
            void UpdatePositionMessageReceived(string returnData)
            {
                // Extract information out of the bytes
                UpdatePaddleMessage msg = JsonConvert.DeserializeObject<UpdatePaddleMessage>(returnData);

                // Correct the paddle position using the msg.position
                theirPaddle.Position = msg.position;
                lastDirection = msg.direction;

                // Extrapolation                

                // If this clients tickCounter is the same as in the msg's tickNumber, than this client doesn't need extrapolation.
                if (tickCounter == msg.tickNumber) return;
                // If the direction is NoDirection than their paddle doesn't move
                if (msg.direction == "NoDirection") return;

                // Get the difference between this tickCounter and the tickNumber in the msg
                int difference = tickCounter - msg.tickNumber;

                // Update their paddle position using the difference in ticks to move their paddle up or down, depending on if the msg.direction is down or up.
                if (msg.direction == "Down")
                {
                    theirPaddle.Position += new Vector2(0, difference * paddleMovementSpeed.Y);
                }
                else if (msg.direction == "Up")
                {
                    theirPaddle.Position += new Vector2(0, -difference * paddleMovementSpeed.Y);
                }
            }

            /// <summary>
            /// Handles the data received from the NoChangeMessage to check if there still isn't a change in direction.
            /// </summary>
            void NoChangeMessageReceived(string returnData)
            {
                // Extract information out of the bytes
                NoChangeMessage msg = JsonConvert.DeserializeObject<NoChangeMessage>(returnData);

                // Extrapolation

                // If this clients tickCounter is the same as in the msg's tickNumber, than this client doesn't need extrapolation.
                if (tickCounter == msg.tickNumber) return;
                // If the direction is NoDirection than their paddle doesn't move
                if (msg.direction == "NoDirection")
                {
                    lastDirection = msg.direction;
                    return;
                }

                // Get the difference between this clients tickCounter and the tickNumber in the msg
                int difference = tickCounter - msg.tickNumber;

                // Update their paddle position using the difference in ticks to move their paddle up or down, depending on if the msg.direction is down or up.
                if (msg.direction == "Down")
                {
                    theirPaddle.Position += new Vector2(0, difference * paddleMovementSpeed.Y);
                }
                else if (msg.direction == "Up")
                {
                    theirPaddle.Position += new Vector2(0, -difference * paddleMovementSpeed.Y);
                }
            }

            /// <summary>
            /// Handles the data received from the PaddleHitMessage to correct their paddle position, the ball position and ball direction 
            /// And when needed extrapolate the ball back into correct position.
            /// </summary>
            void PaddleHitMessageReceived(string returnData)
            {
                // Extract information out of the bytes
                PaddleHitMessage msg = JsonConvert.DeserializeObject<PaddleHitMessage>(returnData);

                // Set the positions and ball directions the same as in the msg
                theirPaddle.Position = msg.paddlePosition;

                ball.vx = (int)msg.ballDirection.X;
                ball.vy = (int)msg.ballDirection.Y;

                ball.x = (int)msg.ballPosition.X;
                ball.y = (int)msg.ballPosition.Y;

                // Extrapolation

                // If this clients tickCounter is higher than the msg's tickNumber, than this client doesn't need extrapolation.
                if (tickCounter == msg.tickNumber) return;

                // Get the difference between this tickCounter and the tickNumber in the msg to extrapolate the ball position.
                int difference = tickCounter - msg.tickNumber;

                // Give the appropiate vector for the direction the Ball is facing
                // ballSpeed is a Scalar
                // If the ball is going to top left
                if ((int)msg.ballDirection.X < 0 && (int)msg.ballDirection.Y < 0)
                {
                    ball.x += -difference * ballSpeed;
                    ball.y += -difference * ballSpeed;
                }
                // If the ball is going to bottom right
                else if ((int)msg.ballDirection.X > 0 && (int)msg.ballDirection.Y > 0)
                {
                    ball.x += difference * ballSpeed;
                    ball.y += difference * ballSpeed;
                }
                // If the ball is going to top right
                else if ((int)msg.ballDirection.X > 0 && (int)msg.ballDirection.Y < 0)
                {
                    ball.x += difference * ballSpeed;
                    ball.y += -difference * ballSpeed;
                }
                // If the ball is going to bottom left
                else if ((int)msg.ballDirection.X < 0 && (int)msg.ballDirection.Y > 0)
                {
                    ball.x += -difference * ballSpeed;
                    ball.y += difference * ballSpeed;
                }
            }
        }
        //--------------------------------------------------------

        /// <summary>
        /// WARNING! Update() IS NOT used anymore for game physics etc; put all game logic in Tick() instead
        /// </summary>
        public override void Update(GameTime gameTime)
        {
            // USE the Tick() method for game-updates otherwise the game isn't deterministic

            int desiredTickCount, attempts = 0;
            desiredTickCount = (int)(stopWatch.ElapsedMilliseconds / accurateMs);

            // If there is a difference in ticks (desired vs actual): Do a tick MULTIPLE times until it's equal again..                        
            while (desiredTickCount > tickCounter)
            {
                Tick();

                //note: capped at maximum of [MAX_ATTEMPTS] ticks per update, to be safe.. Any remaining ticks will transfer to next frame.
                attempts++;
                if (attempts >= MAX_ATTEMPTS) break;
            }
            base.Update(gameTime);
        }

        public override void Reset()
        {
            base.Reset();
        }
    }
}