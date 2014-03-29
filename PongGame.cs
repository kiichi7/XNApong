// Author: Ben
// Project: XnaPong
// Path: C:\code\Xna\XnaPong
// Creation date: 18.11.2006 03:56
// Last modified: 27.01.2008 16:09

#region Using directives
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;
using System;
using System.Collections.Generic;
#endregion

namespace XnaPong
{
	/// <summary>
	/// This is the main type for your game
	/// </summary>
	public class PongGame : Microsoft.Xna.Framework.Game
	{
		#region Constants
		/// <summary>
		/// Rectangles for our graphics, tested with the unit tests below.
		/// </summary>
		static readonly Rectangle
			XnaPongLogoRect = new Rectangle(0, 0, 512, 110),
			MenuSingleplayerRect = new Rectangle(0, 110, 512, 38),
			MenuMultiplayerRect = new Rectangle(0, 148, 512, 38),
			MenuExitRect = new Rectangle(0, 185, 512, 38),
			GameLivesRect = new Rectangle(0, 222, 100, 34),
			GameRedWonRect = new Rectangle(151, 222, 155, 34),
			GameBlueWonRect = new Rectangle(338, 222, 165, 34),
			GameRedPaddleRect = new Rectangle(23, 0, 22, 92),
			GameBluePaddleRect = new Rectangle(0, 0, 22, 92),
			GameBallRect = new Rectangle(1, 94, 33, 33),
			GameSmallBallRect = new Rectangle(37, 108, 19, 19);

		/// <summary>
		/// Ball speed multiplicator, this is how much screen space the ball will
		/// travel each second.
		/// </summary>
		const float BallSpeedMultiplicator = 0.5f;

		/// <summary>
		/// Computer paddle speed. If the ball moves faster up or down than this,
		/// the computer paddle can't keep up and finally we will win.
		/// </summary>
		const float ComputerPaddleSpeed = 0.5f;//25f;

		/// <summary>
		/// Game modes
		/// </summary>
		enum GameMode
		{
			Menu,
			Game,
			GameOver,
		} // enum GameMode
		#endregion

		#region Variables
		/// <summary>
		/// Graphics
		/// </summary>
		GraphicsDeviceManager graphics;
		/// <summary>
		/// Background texture
		/// </summary>
		Texture2D backgroundTexture, menuTexture, gameTexture;
		/// <summary>
		/// Audio engine
		/// </summary>
		AudioEngine audioEngine;
		/// <summary>
		/// Wave bank
		/// </summary>
		WaveBank waveBank;
		/// <summary>
		/// Sound bank
		/// </summary>
		SoundBank soundBank;

		/// <summary>
		/// Resolution of our game.
		/// </summary>
		int width, height;

		/// <summary>
		/// Current paddle positions, 0 means top, 1 means bottom.
		/// </summary>
			/// <returns>0.5f</returns>
		float leftPaddlePosition = 0.5f,
			rightPaddlePosition = 0.5f;

		/// <summary>
		/// Current ball position, again from 0 to 1, 0 is left and top,
		/// 1 is bottom and right.
		/// </summary>
		Vector2 ballPosition = new Vector2(0.5f, 0.5f);
		/// <summary>
		/// Ball speed vector, randomized for every new ball.
		/// Will be set to Zero if we are in menu or game is over.
		/// </summary>
		Vector2 ballSpeedVector = new Vector2(0, 0);//1);

		/// <summary>
		/// Life for left and right players. If one player has 0 lives left,
		/// the other one wins.
		/// </summary>
			/// <returns>3</returns>
		int leftPlayerLives = 3,
			rightPlayerLives = 3;

		/// <summary>
		/// Are we playing a multiplayer game? If this is false, the computer
		/// controls the left paddle.
		/// </summary>
		bool multiplayer = false;

		/// <summary>
		/// Game mode we are currently in. Very simple game flow.
		/// </summary>
		GameMode gameMode = GameMode.Menu;

		/// <summary>
		/// Currently selected menu item.
		/// </summary>
		int currentMenuItem = 0;
		#endregion

		#region Constructor
		/// <summary>
		/// Create pong game
		/// </summary>
		public PongGame()
		{
			graphics = new GraphicsDeviceManager(this);
			Content.RootDirectory = "Content";

			// Don't limit the framerate to the vertical retrace
			graphics.SynchronizeWithVerticalRetrace = false;
			this.IsFixedTimeStep = false;			
		} // PongGame()

		/// <summary>
		/// Allows the game to perform any initialization it needs to before starting to run.
		/// This is where it can query for any required services and load any non-graphic
		/// related content.  Calling base.Initialize will enumerate through any components
		/// and initialize them as well.
		/// </summary>
		protected override void Initialize()
		{
			// Remember resolution
			width = graphics.GraphicsDevice.Viewport.Width;
			height = graphics.GraphicsDevice.Viewport.Height;

			base.Initialize();
		} // Initialize()

		/// <summary>
		/// Load all graphics content (just our background texture).
		/// Use this method to make sure a device reset event is handled correctly.
		/// </summary>
		protected override void LoadContent()
		{
			// Create sprite batch
			spriteBatch = new SpriteBatch(graphics.GraphicsDevice);

			// Load all our content
			backgroundTexture = Content.Load<Texture2D>("SpaceBackground");
			menuTexture = Content.Load<Texture2D>("PongMenu");
			gameTexture = Content.Load<Texture2D>("PongGame");
			audioEngine = new AudioEngine("Content\\PongSound.xgs");
			waveBank = new WaveBank(audioEngine, "Content\\Wave Bank.xwb");
			if (waveBank != null)
				soundBank = new SoundBank(audioEngine, "Content\\Sound Bank.xsb");

			base.LoadContent();
		} // LoadContent()
		#endregion

		#region Start and stop ball
		/// <summary>
		/// Start new ball at the beginning of each game and when a ball is lost.
		/// </summary>
		public void StartNewBall()
		{
			ballPosition = new Vector2(0.5f, 0.5f);
			Random rnd = new Random((int)DateTime.Now.Ticks);
			int direction = rnd.Next(4);
			ballSpeedVector =
				direction == 0 ? new Vector2(1, 0.8f) :
				direction == 1 ? new Vector2(1, -0.8f) :
				direction == 2 ? new Vector2(-1, 0.8f) :
				new Vector2(-1, -0.8f);
		} // StartNewBall()

		/// <summary>
		/// Stop ball for menu and when game is over.
		/// </summary>
		public void StopBall()
		{
			ballSpeedVector = new Vector2(0, 0);
		} // StopBall()
		#endregion

		#region Update
		/// <summary>
		/// <returns>Keyboard state</returns>
		/// Game pad and keyboard states for full 2 player support.
		/// </summary>
		GamePadState gamePad, gamePad2;
		KeyboardState keyboard;

		/// <summary>
		/// Remember up, down, start and back buttons for the menu.
		/// </summary>
		bool remUpPressed = false,
			remDownPressed = false,
			remSpaceOrStartPressed = false,
			remEscOrBackPressed = false;

		/// <summary>
		/// Store up/down states and include thumbstick to control paddles.
		/// </summary>
		bool gamePadUp = false,
			gamePadDown = false,
			gamePad2Up = false,
			gamePad2Down = false;

		/// <summary>
		/// Get input states
		/// </summary>
		private void GetInputStates(float moveFactorPerSecond)
		{
			// Remember last keyboard and gamepad states for the menu
			remUpPressed =
				gamePad.DPad.Up == ButtonState.Pressed ||
				gamePad.ThumbSticks.Left.Y > 0.5f ||
				keyboard.IsKeyDown(Keys.Up);
			remDownPressed =
				gamePad.DPad.Down == ButtonState.Pressed ||
				gamePad.ThumbSticks.Left.Y < -0.5f ||
				keyboard.IsKeyDown(Keys.Down);
			remSpaceOrStartPressed =
				gamePad.Buttons.Start == ButtonState.Pressed ||
				gamePad.Buttons.A == ButtonState.Pressed ||
				keyboard.IsKeyDown(Keys.LeftControl) ||
				keyboard.IsKeyDown(Keys.RightControl) ||
				keyboard.IsKeyDown(Keys.Space) ||
				keyboard.IsKeyDown(Keys.Enter);
			remEscOrBackPressed =
				gamePad.Buttons.Back == ButtonState.Pressed ||
				keyboard.IsKeyDown(Keys.Escape);

			// Get current gamepad and keyboard states
			gamePad = GamePad.GetState(PlayerIndex.One);
			gamePad2 = GamePad.GetState(PlayerIndex.Two);
			keyboard = Keyboard.GetState();

			gamePadUp = gamePad.DPad.Up == ButtonState.Pressed ||
				gamePad.ThumbSticks.Left.Y > 0.5f;
			gamePadDown = gamePad.DPad.Down == ButtonState.Pressed ||
				gamePad.ThumbSticks.Left.Y < -0.5f;
			gamePad2Up = gamePad2.DPad.Up == ButtonState.Pressed ||
				gamePad2.ThumbSticks.Left.Y > 0.5f;
			gamePad2Down = gamePad2.DPad.Down == ButtonState.Pressed ||
				gamePad2.ThumbSticks.Left.Y < -0.5f;

			// Move up and down if we press the cursor or gamepad keys.
			if (gamePadUp ||
				keyboard.IsKeyDown(Keys.Up))
				rightPaddlePosition -= moveFactorPerSecond;
			if (gamePadDown ||
				keyboard.IsKeyDown(Keys.Down))
				rightPaddlePosition += moveFactorPerSecond;

			// Second player is either controlled by player 2 or by the computer
			if (multiplayer)
			{
				// Move up and down if we press the cursor or gamepad keys.
				if (gamePad2Up ||
					keyboard.IsKeyDown(Keys.W))
					leftPaddlePosition -= moveFactorPerSecond;
				if (gamePad2Down ||
					keyboard.IsKeyDown(Keys.S) ||
					keyboard.IsKeyDown(Keys.O))
					leftPaddlePosition += moveFactorPerSecond;
			} // if (multiplayer)
			else
			{
				// Just let the computer follow the ball position
				float computerChange = ComputerPaddleSpeed * moveFactorPerSecond;
				if (leftPaddlePosition > ballPosition.Y + computerChange)
					leftPaddlePosition -= computerChange;
				else if (leftPaddlePosition < ballPosition.Y - computerChange)
					leftPaddlePosition += computerChange;
			} // else

			// Make sure paddles stay between 0 and 1
			if (leftPaddlePosition < 0)
				leftPaddlePosition = 0;
			if (leftPaddlePosition > 1)
				leftPaddlePosition = 1;
			if (rightPaddlePosition < 0)
				rightPaddlePosition = 0;
			if (rightPaddlePosition > 1)
				rightPaddlePosition = 1;
		} // GetInputStates()

		/// <summary>
		/// Handle ball collisions
		/// </summary>
		private void HandleBallCollisions(float moveFactorPerSecond)
		{
			// Check top and bottom screen border
			if (ballPosition.Y < 0 ||
				ballPosition.Y > 1)
			{
				ballSpeedVector.Y = -ballSpeedVector.Y;
				soundBank.PlayCue("PongBallHit");
			} // if (ballPosition.Y)

			// Check for collisions with the paddles
			// Construct bounding boxes to use the intersection helper method.
			Vector2 ballSize = new Vector2(
				GameBallRect.Width / 1024.0f, GameBallRect.Height / 768.0f);
			BoundingBox ballBox = new BoundingBox(
				new Vector3(ballPosition.X - ballSize.X / 2, ballPosition.Y - ballSize.Y / 2, 0),
				new Vector3(ballPosition.X + ballSize.X / 2, ballPosition.Y + ballSize.Y / 2, 0));
			Vector2 paddleSize = new Vector2(
				GameRedPaddleRect.Width / 1024.0f, GameRedPaddleRect.Height / 768.0f);
			BoundingBox leftPaddleBox = new BoundingBox(
				new Vector3(-paddleSize.X / 2, leftPaddlePosition - paddleSize.Y / 2, 0),
				new Vector3(+paddleSize.X / 2, leftPaddlePosition + paddleSize.Y / 2, 0));
			BoundingBox rightPaddleBox = new BoundingBox(
				new Vector3(1 - paddleSize.X / 2, rightPaddlePosition - paddleSize.Y / 2, 0),
				new Vector3(1 + paddleSize.X / 2, rightPaddlePosition + paddleSize.Y / 2, 0));

			// Ball hit left paddle?
			if (ballBox.Intersects(leftPaddleBox))
			{
				// Bounce of the paddle (always make positive)
				ballSpeedVector.X = Math.Abs(ballSpeedVector.X);
				// Increase speed a little
				ballSpeedVector *= 1.05f;
				// Did we hit the edges of the paddle?
				if (ballBox.Intersects(new BoundingBox(
					new Vector3(leftPaddleBox.Min.X - 0.01f, leftPaddleBox.Min.Y - 0.01f, 0),
					new Vector3(leftPaddleBox.Min.X + 0.01f, leftPaddleBox.Min.Y + 0.01f, 0))))
					// Bounce of at a more difficult angle for the other player
					ballSpeedVector.Y = -2;
				else if (ballBox.Intersects(new BoundingBox(
					new Vector3(leftPaddleBox.Min.X - 0.01f, leftPaddleBox.Max.Y - 0.01f, 0),
					new Vector3(leftPaddleBox.Min.X + 0.01f, leftPaddleBox.Max.Y + 0.01f, 0))))
					// Bounce of at a more difficult angle for the other player
					ballSpeedVector.Y = +2;
				// Play sound
				soundBank.PlayCue("PongBallHit");
			} // if (ballBox.Intersects)

			// Ball hit right paddle?
			if (ballBox.Intersects(rightPaddleBox))
			{
				// Bounce of the paddle (always make negative)
				ballSpeedVector.X = -Math.Abs(ballSpeedVector.X);
				// Increase speed a little
				ballSpeedVector *= 1.05f;
				// Did we hit the edges of the paddle?
				if (ballBox.Intersects(new BoundingBox(
					new Vector3(rightPaddleBox.Min.X - 0.01f, rightPaddleBox.Min.Y - 0.01f, 0),
					new Vector3(rightPaddleBox.Min.X + 0.01f, rightPaddleBox.Min.Y + 0.01f, 0))))
					// Bounce of at a more difficult angle for the other player
					ballSpeedVector.Y = -2;
				else if (ballBox.Intersects(new BoundingBox(
					new Vector3(rightPaddleBox.Min.X - 0.01f, rightPaddleBox.Max.Y - 0.01f, 0),
					new Vector3(rightPaddleBox.Min.X + 0.01f, rightPaddleBox.Max.Y + 0.01f, 0))))
					// Bounce of at a more difficult angle for the other player
					ballSpeedVector.Y = +2;

				// Play sound
				soundBank.PlayCue("PongBallHit");
			} // if (ballBox.Intersects)

			// Update ball position and bounce off the borders
			ballPosition += ballSpeedVector *
				moveFactorPerSecond * BallSpeedMultiplicator;

			// Ball lost?
			if (ballPosition.X < -0.065f)
			{
				// Play sound
				soundBank.PlayCue("PongBallLost");
				// Reduce number of lives
				leftPlayerLives--;
				// Start new ball
				StartNewBall();
			} // if (ballPosition.X)
			else if (ballPosition.X > 1.065f)
			{
				// Play sound
				soundBank.PlayCue("PongBallLost");
				// Reduce number of lives
				rightPlayerLives--;
				// Start new ball
				StartNewBall();
			} // else if
		} // HandleBallCollisions()

		/// <summary>
		/// Allows the game to run logic such as updating the world,
		/// checking for collisions, gathering input and playing audio.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Update(GameTime gameTime)
		{
			// Move half way across the screen each second
			float moveFactorPerSecond = 0.5f *
				(float)gameTime.ElapsedRealTime.TotalMilliseconds / 1000.0f;

			GetInputStates(moveFactorPerSecond);

			// Ball is stopped and we are in the game? Then start it again.
			if (ballSpeedVector.LengthSquared() == 0 &&
				gameMode == GameMode.Game)
				StartNewBall();

			HandleBallCollisions(moveFactorPerSecond);

			// If either player has no more lives, the other one has won!
			if (gameMode == GameMode.Game &&
				(leftPlayerLives == 0 ||
				rightPlayerLives == 0))
			{
				gameMode = GameMode.GameOver;
				StopBall();
			} // if (gameMode)

			base.Update(gameTime);
		} // Update(gameTime)
		#endregion

		#region Sprite handling
		/// <summary>
		/// Sprite to render
		/// </summary>
		class SpriteToRender
		{
			/// <summary>
			/// Texture
			/// </summary>
			public Texture2D texture;
			/// <summary>
			/// Rectangle
			/// </summary>
			public Rectangle rect;
			/// <summary>
			/// Source rectangle
			/// </summary>
			public Rectangle? sourceRect;
			/// <summary>
			/// Color
			/// </summary>
			public Color color;

			/// <summary>
			/// Create sprite to render
			/// </summary>
			/// <param name="setTexture">Set texture</param>
			/// <param name="setRect">Set rectangle</param>
			/// <param name="setSourceRect">Set source rectangle</param>
			/// <param name="setColor">Set color</param>
			public SpriteToRender(Texture2D setTexture, Rectangle setRect,
				Rectangle? setSourceRect, Color setColor)
			{
				texture = setTexture;
				rect = setRect;
				sourceRect = setSourceRect;
				color = setColor;
			} // SpriteToRender(setTexture, setRect, setSourceRect)
		} // class SpriteToRender

		/// <summary>
		/// Sprites
		/// </summary>
		List<SpriteToRender> sprites = new List<SpriteToRender>();
		/// <summary>
		/// Sprite batch
		/// </summary>
		SpriteBatch spriteBatch = null;

		/// <summary>
		/// Render sprite
		/// </summary>
		/// <param name="texture">Texture</param>
		/// <param name="rect">Rectangle</param>
		/// <param name="sourceRect">Source rectangle</param>
		/// <param name="color">Color</param>
		public void RenderSprite(Texture2D texture, Rectangle rect, Rectangle? sourceRect,
			Color color)
		{
			sprites.Add(new SpriteToRender(texture, rect, sourceRect, color));
		/// <summary>
		/// Render sprite
		/// </summary>
		/// <param name="texture">Texture</param>
		/// <param name="rect">Rectangle</param>
		/// <param name="sourceRect">Source rectangle</param>
		} // RenderSprite(texture, rect, sourceRect)

		/// <summary>
		/// Render sprite
		/// </summary>
		/// <param name="texture">Texture</param>
		/// <param name="x">X</param>
		/// <param name="y">Y</param>
		/// <param name="sourceRect">Source rectangle</param>
		/// <param name="color">Color</param>
		public void RenderSprite(Texture2D texture, Rectangle rect, Rectangle? sourceRect)
		{
			RenderSprite(texture, rect, sourceRect, Color.White);
		} // RenderSprite(texture, rect, sourceRect)

		public void RenderSprite(Texture2D texture, int x, int y, Rectangle? sourceRect,
			Color color)
		{
			/// <summary>
			/// Render sprite
			/// </summary>
			/// <param name="texture">Texture</param>
			/// <param name="x">X</param>
			/// <param name="y">Y</param>
			/// <param name="sourceRect">Source rectangle</param>
			RenderSprite(texture,
				new Rectangle(x, y, sourceRect.Value.Width, sourceRect.Value.Height),
				sourceRect, color);
		} // RenderSprite(texture, x, y)

		public void RenderSprite(Texture2D texture, int x, int y, Rectangle? sourceRect)
		{
			/// <summary>
			/// Render sprite
			/// </summary>
			/// <param name="texture">Texture</param>
			/// <param name="rect">Rectangle</param>
			/// <param name="color">Color</param>
			RenderSprite(texture,
				new Rectangle(x, y, sourceRect.Value.Width, sourceRect.Value.Height),
				sourceRect, Color.White);
		} // RenderSprite(texture, x, y)

		/// <summary>
		/// Render sprite
		/// </summary>
		/// <param name="texture">Texture</param>
		/// <param name="rect">Rectangle</param>
		public void RenderSprite(Texture2D texture, Rectangle rect, Color color)
		{
			RenderSprite(texture, rect, null, color);
		} // RenderSprite(texture, rect, color)

		/// <summary>
		/// Render sprite
		/// </summary>
		/// <param name="texture">Texture</param>
		public void RenderSprite(Texture2D texture, Rectangle rect)
		{
			RenderSprite(texture, rect, null, Color.White);
		} // RenderSprite(texture, rect)

		public void RenderSprite(Texture2D texture)
		{
			RenderSprite(texture, new Rectangle(0, 0, 1024, 768), null, Color.White);
		} // RenderSprite(texture)

		/// <summary>
		/// Draw sprites
		/// </summary>
		public void DrawSprites()
		{
			// No need to render if we got no sprites this frame
			if (sprites.Count == 0)
				return;

			// Start rendering sprites
			spriteBatch.Begin(SpriteBlendMode.AlphaBlend,
				SpriteSortMode.BackToFront, SaveStateMode.None);

			// Render all sprites
			foreach (SpriteToRender sprite in sprites)
				spriteBatch.Draw(sprite.texture,
					// Rescale to fit resolution
					new Rectangle(
					sprite.rect.X * width / 1024,
					sprite.rect.Y * height / 768,
					sprite.rect.Width * width / 1024,
					sprite.rect.Height * height / 768),
					sprite.sourceRect, sprite.color);

			// We are done, draw everything on screen with help of the end method.
			spriteBatch.End();

			// Kill list of remembered sprites
			sprites.Clear();
		} // DrawSprites()
		#endregion

		#region Render ball and paddles
		/// <summary>
		/// Render ball
		/// </summary>
		public void RenderBall()
		{
			RenderSprite(gameTexture,
				(int)((0.05f + 0.9f * ballPosition.X) * 1024) - GameBallRect.Width / 2,
				(int)((0.02f + 0.96f * ballPosition.Y) * 768) - GameBallRect.Height / 2,
				GameBallRect);
		} // RenderBall()

		/// <summary>
		/// Render paddles
		/// </summary>
		public void RenderPaddles()
		{
			RenderSprite(gameTexture,
				(int)(0.05f * 1024) - GameRedPaddleRect.Width / 2,
				(int)((0.06f + 0.88f * leftPaddlePosition) * 768) - GameRedPaddleRect.Height / 2,
				GameRedPaddleRect);
			RenderSprite(gameTexture,
				(int)(0.95f * 1024) - GameBluePaddleRect.Width / 2,
				(int)((0.06f + 0.88f * rightPaddlePosition) * 768) - GameBluePaddleRect.Height / 2,
				GameBluePaddleRect);
		} // RenderPaddles()

		/// <summary>
		/// Show lives
		/// </summary>
		public void ShowLives()
		{
			// Support for both Windows monitors and TV screens on the Xbox 360!
			int xPos = 2;
			int yPos = 2;
#if XBOX360
			xPos += Window.ClientBounds.Width / 24;
			yPos += Window.ClientBounds.Height / 24;
#endif
			// Left players lives
			RenderSprite(menuTexture, xPos, yPos, GameLivesRect);
			for (int num = 0; num < leftPlayerLives; num++)
				RenderSprite(gameTexture,
					xPos + GameLivesRect.Width + GameSmallBallRect.Width * num - 2,
					yPos + 7,
					GameSmallBallRect);

			// Right players lives
			int rightX = 1024 - GameLivesRect.Width - GameSmallBallRect.Width * 3 - xPos - 2;
			RenderSprite(menuTexture, rightX, yPos, GameLivesRect);
			for (int num = 0; num < rightPlayerLives; num++)
				RenderSprite(gameTexture,
					rightX + GameLivesRect.Width + GameSmallBallRect.Width * num - 2,
					yPos + 7,
					GameSmallBallRect);
		} // ShowLives()
		#endregion

		#region Draw
		/// <summary>
		/// This is called when the game should draw itself.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Draw(GameTime gameTime)
		{
			// Clear background
			graphics.GraphicsDevice.Clear(Color.Black);

			// Draw background texture in a seperate pass, else it gets messed up with
			// our other sprites, the ordering does not really work great.
			spriteBatch.Begin();
			spriteBatch.Draw(backgroundTexture,
				new Rectangle(0, 0, width, height),
				Color.LightGray);
			spriteBatch.End();

			// Show screen depending on our current screen mode
			if (gameMode == GameMode.Menu)
			{
				// Show menu
				RenderSprite(menuTexture,
					512 - XnaPongLogoRect.Width / 2, 150, XnaPongLogoRect);
				RenderSprite(menuTexture,
					512 - MenuSingleplayerRect.Width / 2, 300, MenuSingleplayerRect,
					currentMenuItem == 0 ? Color.Orange : Color.White);
				RenderSprite(menuTexture,
					512 - MenuMultiplayerRect.Width / 2, 350, MenuMultiplayerRect,
					currentMenuItem == 1 ? Color.Orange : Color.White);
				RenderSprite(menuTexture,
					512 - MenuExitRect.Width / 2, 400, MenuExitRect,
					currentMenuItem == 2 ? Color.Orange : Color.White);

				// Note: Usually input should be handled in Update, but I really think
				// it is better to have to closely together with the UI code here!
				if ((keyboard.IsKeyDown(Keys.Down) ||
					gamePadDown) &&
					remDownPressed == false)
				{
					currentMenuItem = (currentMenuItem + 1) % 3;
					soundBank.PlayCue("PongBallHit");
				} // if (keyboard.IsKeyDown)
				else if ((keyboard.IsKeyDown(Keys.Up) ||
					gamePadUp) &&
					remUpPressed == false)
				{
					currentMenuItem = (currentMenuItem + 2) % 3;
					soundBank.PlayCue("PongBallHit");
				} // else if
				else if ((keyboard.IsKeyDown(Keys.Space) ||
					keyboard.IsKeyDown(Keys.LeftControl) ||
					keyboard.IsKeyDown(Keys.RightControl) ||
					keyboard.IsKeyDown(Keys.Enter) ||
					gamePad.Buttons.A == ButtonState.Pressed ||
					gamePad.Buttons.Start == ButtonState.Pressed ||
					// Back or Escape exits our game on Xbox 360 and Windows
					keyboard.IsKeyDown(Keys.Escape) ||
					gamePad.Buttons.Back == ButtonState.Pressed) &&
					remSpaceOrStartPressed == false &&
					remEscOrBackPressed == false)
				{
					// Quit app.
					if (currentMenuItem == 2 ||
						keyboard.IsKeyDown(Keys.Escape) ||
						gamePad.Buttons.Back == ButtonState.Pressed)
					{
						this.Exit();
					} // if (currentMenuItem)
					else
					{
						// Start game
						gameMode = GameMode.Game;
						leftPlayerLives = 3;
						rightPlayerLives = 3;
						leftPaddlePosition = 0.5f;
						rightPaddlePosition = 0.5f;
						StartNewBall();
						// Set multiplayer if it was selected in menu.
						// Otherwise the computer controls the left paddle
						multiplayer = currentMenuItem == 1;
					} // else
				} // else if
			} // if (gameMode)
			else
			{
				// Show lives
				ShowLives();

				// Ball in center
				RenderBall();
				// Render both paddles
				RenderPaddles();

				// If game is over, show winner
				if (gameMode == GameMode.GameOver)
				{
					if (leftPlayerLives == 0)
						RenderSprite(menuTexture,
							512 - GameBlueWonRect.Width / 2, 300, GameBlueWonRect);
					else
						RenderSprite(menuTexture,
							512 - GameRedWonRect.Width / 2, 300, GameRedWonRect);

					// A, Space or Enter returns us to the main menu.
					// Esc and Back do return the the menu too, see below.
					// Make sure the keys are released again when we enter the main menu.
					if ((gamePad.Buttons.A == ButtonState.Pressed ||
						keyboard.IsKeyDown(Keys.Space) ||
						keyboard.IsKeyDown(Keys.Enter)) &&
						remSpaceOrStartPressed == false)
						gameMode = GameMode.Menu;
				} // if (gameMode)

				// Back and Escape always quits to the main menu
				if ((gamePad.Buttons.Back == ButtonState.Pressed ||
					keyboard.IsKeyDown(Keys.Escape)) &&
					remEscOrBackPressed == false)
				{
					gameMode = GameMode.Menu;
					StopBall();
				} // if (gamePad.Buttons.Back)
			} // else

			// Draw everything on screen.
			DrawSprites();

			base.Draw(gameTime);
		} // Draw(gameTime)
		#endregion

		#region Start game
		/// <summary>
		/// Start game
		/// </summary>
		public static void StartGame()
		{
			using (PongGame game = new PongGame())
			{
				game.Run();
			} // using (game)
		} // StartGame()
		#endregion

		#region Unit tests
#if DEBUG
		#region Test delegate
		/// <summary>
		/// Test delegate helper
		/// </summary>
		delegate void TestDelegate();
		#endregion

		#region TestPongGame class
		/// <summary>
		/// Helper class to test the PongGame
		/// </summary>
		/// <param name="setTestLoop">Set test loop</param>
		class TestPongGame : PongGame
		{
			/// <summary>
			/// Test loop
			/// </summary>
			TestDelegate testLoop;
			public TestPongGame(TestDelegate setTestLoop)
			{
				testLoop = setTestLoop;
			} // TestPongGame(setTestLoop)

			/// <summary>
			/// Draw
			/// </summary>
			/// <param name="gameTime">Game time</param>
			protected override void Draw(GameTime gameTime)
			{
				base.Draw(gameTime);
				testLoop();
			} // Draw(gameTime)
		} // class TestPongGame
		#endregion

		#region StartTest
		/// <summary>
		/// Test game
		/// </summary>
		static TestPongGame testGame;
		/// <summary>
		/// Start test
		/// </summary>
		/// <param name="testLoop">Test loop</param>
		static void StartTest(TestDelegate testLoop)
		{
			testGame = new TestPongGame(testLoop);
			testGame.Run();
			testGame.Dispose();
		} // StartTest(testLoop)
		#endregion

		#region TestSounds
		/// <summary>
		/// Test sounds
		/// </summary>
		public static void TestSounds()
		{
			StartTest(
				delegate
				{
					if (testGame.keyboard.IsKeyDown(Keys.Space))
						testGame.soundBank.PlayCue("PongBallHit");
					if (testGame.keyboard.IsKeyDown(Keys.LeftControl))
						testGame.soundBank.PlayCue("PongBallLost");
				});
		} // TestSounds()
		#endregion

		#region TestMenuSprites
		/// <summary>
		/// Test menu sprites
		/// </summary>
		public static void TestMenuSprites()
		{
			StartTest(
				delegate
				{
					testGame.RenderSprite(testGame.menuTexture,
						new Rectangle(512 - XnaPongLogoRect.Width / 2, 150,
						XnaPongLogoRect.Width, XnaPongLogoRect.Height),
						XnaPongLogoRect);
					testGame.RenderSprite(testGame.menuTexture,
						new Rectangle(512 - MenuSingleplayerRect.Width / 2, 300,
						MenuSingleplayerRect.Width, MenuSingleplayerRect.Height),
						MenuSingleplayerRect);
					testGame.RenderSprite(testGame.menuTexture,
						new Rectangle(512 - MenuMultiplayerRect.Width / 2, 350,
						MenuMultiplayerRect.Width, MenuMultiplayerRect.Height),
						MenuMultiplayerRect, Color.Orange);
					testGame.RenderSprite(testGame.menuTexture,
						new Rectangle(512 - MenuExitRect.Width / 2, 400,
						MenuExitRect.Width, MenuExitRect.Height),
						MenuExitRect);
				});
		} // TestMenuSprites()
		#endregion

		#region TestGameSprites
		/// <summary>
		/// Test game sprites
		/// </summary>
		public static void TestGameSprites()
		{
			StartTest(
				delegate
				{
					// Show lives
					testGame.ShowLives();

					// Ball in center
					testGame.RenderBall();
					// Render both paddles
					testGame.RenderPaddles();
				});
		} // TestGameSprites()
		#endregion

		#region TestSingleplayerGame
		/// <summary>
		/// Test singleplayer game
		/// </summary>
		public static void TestSingleplayerGame()
		{
			StartTest(
				delegate
				{
					// Make sure we are in the game and in singleplayer mode
					testGame.gameMode = GameMode.Game;
					testGame.multiplayer = false;

					// Show lives
					testGame.ShowLives();

					// Ball in center
					testGame.RenderBall();
					// Render both paddles
					testGame.RenderPaddles();
				});
		} // TestSingleplayerGame()
		#endregion

		#region TestBallCollisions
		/// <summary>
		/// Test ball collisions
		/// </summary>
		public static void TestBallCollisions()
		{
			StartTest(
				delegate
				{
					// Make sure we are in the game and in singleplayer mode
					testGame.gameMode = GameMode.Game;
					testGame.multiplayer = false;
					testGame.Window.Title = "Xna Pong - Press 1-5 to start collision tests";

					// Start specific collision scene based on the user input.
					if (testGame.keyboard.IsKeyDown(Keys.D1))
					{
						// First test, just collide with screen border
						testGame.ballPosition = new Vector2(0.6f, 0.9f);
						testGame.ballSpeedVector = new Vector2(1, 1);
					} // if
					else if (testGame.keyboard.IsKeyDown(Keys.D2))
					{
						// Second test, straight on collision with right paddle
						testGame.ballPosition = new Vector2(0.9f, 0.6f);
						testGame.ballSpeedVector = new Vector2(1, 1);
						testGame.rightPaddlePosition = 0.7f;
					} // if
					else if (testGame.keyboard.IsKeyDown(Keys.D3))
					{
						// Thrid test, straight on collision with left paddle
						testGame.ballPosition = new Vector2(0.1f, 0.4f);
						testGame.ballSpeedVector = new Vector2(-1, -0.5f);
						testGame.leftPaddlePosition = 0.35f;
					} // if
					else if (testGame.keyboard.IsKeyDown(Keys.D4))
					{
						// Advanced test to check if we hit the edge of the right paddle
						testGame.ballPosition = new Vector2(0.9f, 0.4f);
						testGame.ballSpeedVector = new Vector2(1, -0.5f);
						testGame.rightPaddlePosition = 0.29f;
					} // if
					else if (testGame.keyboard.IsKeyDown(Keys.D5))
					{
						// Advanced test to check if we hit the edge of the right paddle
						testGame.ballPosition = new Vector2(0.9f, 0.4f);
						testGame.ballSpeedVector = new Vector2(1, -0.5f);
						testGame.rightPaddlePosition = 0.42f;
					} // if

					// Show lives
					testGame.ShowLives();

					// Ball in center
					testGame.RenderBall();
					// Render both paddles
					testGame.RenderPaddles();
				});
		} // TestBallCollisions()
		#endregion
#endif
		#endregion
	} // class PongGame
} // namespace XnaPong
