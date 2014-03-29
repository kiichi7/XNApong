// Author: Ben
// Project: XnaPong
// Path: C:\code\Xna\XnaPong
// Creation date: 27.01.2008 11:14
// Last modified: 27.01.2008 11:20

#region Using directives
using System;
#endregion

namespace XnaPong
{
	/// <summary>
	/// Program
	/// </summary>
	static class Program
	{
		#region Main
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main(string[] args)
		{
			PongGame.StartGame();

			// Static unit tests:
			//PongGame.TestSounds();
			//PongGame.TestMenuSprites();
			//PongGame.TestGameSprites();
			//PongGame.TestSingleplayerGame();
			//PongGame.TestBallCollisions();
		} // Main(args)
		#endregion
	} // class Program
} // namespace XnaPong

