﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace Ohoy_Exploration_Prototype
{
    internal class Program
    {
        //Type Definitions
        enum CardinalDirection
        {
            North,
            South,
            West,
            East,
        }
        /// <summary>
        /// Class for the sprites in the game, specifically for islands and the ships.
        /// </summary>
        class Sprite
        {
            public char[,] CharacterMap;
            public int Width;
            public int Height;
            public ConsoleColor Color = ConsoleColor.White;

            public Sprite(int width, int height)
            {
                Width = width;
                Height = height;
                CharacterMap = new char[Width, Height];
            }
        }
        /// <summary>
        /// Class for the islands.
        /// </summary>
        class Island
        {
            public string Name;
            public Landmark Landmark;
            public bool Explored = false;
            public Point Position;
            public Sprite Sprite;
        }
        /// <summary>
        /// Class for the landmark which will be generated unto the islands.
        /// </summary>
        class Landmark
        {
            public string Name;
            public ConsoleColor Color;
            public string LandmarkSymbol;
        }
        /// <summary>
        /// Camera class, responsible for displaying the part of the map that the player sees.
        /// </summary>
        class Camera
        {
            public int Width;
            public int Height;
            public Point Position;
        }
        /// <summary>
        /// Ship class, 
        /// </summary>
        class Ship
        {
            public Point Position;
            public CardinalDirection CardinalDirection;
            public Dictionary<CardinalDirection, Sprite> Sprites = new Dictionary<CardinalDirection, Sprite>();

        }
        class Map
        {
            public int Width;
            public int Height;
            public bool[,] FogOfWar;
            public Island TreasureIsland;

            public Map(int width, int heigth)
            {
                Width = width;
                Height = heigth;
                FogOfWar = new bool[Width, Height];
            }
        }

        struct ScreenCharacter
        {
            public char Symbol;
            public ConsoleColor ForegroundColor;
            public ConsoleColor BackgroundColor;
        }

        class Screen
        {
            public static ConsoleColor ForegroundColor;
            public static ConsoleColor BackgroundColor;
            public int Height;
            public int Width;
            public ScreenCharacter[,] Characters;

            public Screen(int width, int heigth)
            {
                Width = width;
                Height = heigth;
                Characters = new ScreenCharacter[Width, Height];
            }
            public void Clear()
            {
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        Characters[x, y].BackgroundColor = Screen.BackgroundColor;
                        Characters[x, y].ForegroundColor = Screen.ForegroundColor;
                        Characters[x, y].Symbol = ' ';
                    }
                }
            }
        }


        //Game Variables
        const int PrintPauseMilliseconds = 200;

        static List<Island> Islands;

        static Ship PlayerShip;

        static List<Landmark> Landmarks = new List<Landmark>();

        static string[] IslandNames;

        static List<Sprite> IslandSprites = new List<Sprite>();

        static Map AsciiSeaMap;

        static Camera PlayerCamera;

        static Screen CurrentScreen;

        static Screen NextScreen;
        static void LoadData()
        {
            //Load IslandNames
            IslandNames = File.ReadAllLines("IslandNames.txt");

            //Load IslandSprites
            for (int i = 1; i <= 9; i++)
            {
                IslandSprites.Add(ReadSprite($"Sprites/Islands/Shape{i}.txt"));
            }

            //Load Landmarks
            string[] filePaths = Directory.GetFiles("Sprites/Landmarks");
            foreach (string filePath in filePaths)
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                string landmarkName = fileName.ToLowerInvariant();
                string[] landmarkInfo = File.ReadAllLines(filePath);
                ConsoleColor color = Enum.Parse<ConsoleColor>(landmarkInfo[0]);

                Landmark landmark = new Landmark()
                {
                    Name = landmarkName,
                    Color = color,
                    LandmarkSymbol = landmarkInfo[1],
                };
                Landmarks.Add(landmark);
            }
        }

        static void InitializeObjects()
        {
            //Initialize The Map
            AsciiSeaMap = new Map(2000, 1000);

            //Initialize Camera
            PlayerCamera = new Camera
            {
                Width = 150,
                Height = 50,
                Position = new Point(0, 0),
            };

            //Initialize Console
            Console.WindowWidth = PlayerCamera.Width;
            Console.WindowHeight = PlayerCamera.Height;
            Console.SetBufferSize(Console.WindowWidth, Console.WindowHeight);
            Console.CursorVisible = false;

            //Initialize Screens 
            CurrentScreen = new Screen(PlayerCamera.Width, PlayerCamera.Height);
            NextScreen = new Screen(PlayerCamera.Width, PlayerCamera.Height);

            //Random names for islands
            List<string> islandNames = new List<string>(IslandNames);

            //Random landmarks for islands
            List<Landmark> landmarks = new List<Landmark>();
            for (int i = 0; i < 4; i++)
            {
                landmarks.AddRange(Landmarks);
            }
            //Generate all Islands
            Islands = new List<Island>();
            //For Loop where i corresponds to both the amount of islands as well as pulling out landmarks for the island, since the landmarks doesn't need to be randomized. 
            /*
            for (int i = 0; i < 24; i++)
            {
                Island newIsland = new Island
                {
                    Name = islandNames[19],
                    Landmark = landmarks[i],
                    Explored = true,
                    Position = GenerateIslandPosition(),
                    Sprite = IslandSprites[7],
                };
                Islands.Add(newIsland);
            }*/


            Island newIsland = new Island
            {
                Name = islandNames[20],
                Landmark = landmarks[4],
                Explored = true,
                Position = new Point(40, 10),
                Sprite = IslandSprites[3],
            };
            Islands.Add(newIsland);


            //Initialize Ship
            PlayerShip = new Ship
            {
                Position = new Point(50, 25),
                CardinalDirection = CardinalDirection.North,
            };

            PlayerShip.Sprites[CardinalDirection.North] = ReadSprite("Sprites/Ship/North.txt");
            PlayerShip.Sprites[CardinalDirection.South] = ReadSprite("Sprites/Ship/South.txt");
            PlayerShip.Sprites[CardinalDirection.East] = ReadSprite("Sprites/Ship/East.txt");
            PlayerShip.Sprites[CardinalDirection.West] = ReadSprite("Sprites/Ship/West.txt");
        }
        //Method that screens.
        static void DrawNextScreen()
        {
            for (int y = 0; y < NextScreen.Height; y++)
            {
                for (int x = 0; x < NextScreen.Width; x++)
                {
                    bool backgroundIsDifferent = NextScreen.Characters[x, y].BackgroundColor != CurrentScreen.Characters[x, y].BackgroundColor;
                    bool foregroundIsDifferent = NextScreen.Characters[x, y].ForegroundColor != CurrentScreen.Characters[x, y].ForegroundColor;
                    bool symbolIsDifferent = NextScreen.Characters[x, y].Symbol != CurrentScreen.Characters[x, y].Symbol;
                    bool characterIsDifferent = symbolIsDifferent || foregroundIsDifferent || backgroundIsDifferent;
                    if (characterIsDifferent)
                    {

                        if (Console.ForegroundColor != NextScreen.Characters[x, y].ForegroundColor)
                        {
                            Console.ForegroundColor = NextScreen.Characters[x, y].ForegroundColor;
                        }
                        if (Console.BackgroundColor != NextScreen.Characters[x, y].BackgroundColor)
                        {
                            Console.BackgroundColor = NextScreen.Characters[x, y].BackgroundColor;
                        }
                        Console.SetCursorPosition(x, y);
                        Console.Write(NextScreen.Characters[x, y].Symbol);
                    }
                }
            }
            Screen temp = CurrentScreen;
            CurrentScreen = NextScreen;
            NextScreen = temp;
        }


        //Method that draws.
        static void ScreenWrite(int x, int y, string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                ScreenWrite(x + i, y, text[i]);
            }
        }
        static void ScreenWrite(int x, int y, char symbol)
        {
            NextScreen.Characters[x, y].Symbol = symbol;
            NextScreen.Characters[x, y].BackgroundColor = Screen.BackgroundColor;
            NextScreen.Characters[x, y].ForegroundColor = Screen.ForegroundColor;
        }
        /// <summary>
        /// A method which reads the sprites out of the textfiles, allowing the program to draw the sprites later.
        /// </summary>
        /// <param name="path">Which file to read.</param>
        /// <returns></returns>
        static Sprite ReadSprite(string path)
        {
            string[] spriteLines = File.ReadAllLines(path);

            ConsoleColor color;
            bool includesColorInformation = Enum.TryParse<ConsoleColor>(spriteLines[0], out color);

            //Determine Sprite Size Automatically
            int height = spriteLines.Length;
            int spriteStartingLineIndex = 0;
            if (includesColorInformation)
            {
                height--;
                spriteStartingLineIndex = 1;
            }

            int width = 0;

            for (int y = 0; y < height; y++)
            {
                string currentSpriteLine = spriteLines[y + spriteStartingLineIndex];
                int currentWidth = currentSpriteLine.Length;
                if (currentWidth > width)
                {
                    width = currentWidth;
                }
            }

            Sprite sprite = new Sprite(width, height);
            if (includesColorInformation)
            {
                sprite.Color = color;
            }

            //Set Up Sprite
            for (int y = 0; y < height; y++)
            {
                string currentSpriteLine = spriteLines[y + spriteStartingLineIndex];
                for (int x = 0; x < width; x++)
                {
                    if (x < currentSpriteLine.Length)
                    {
                        sprite.CharacterMap[x, y] = currentSpriteLine[x];
                    }
                    else
                    {
                        sprite.CharacterMap[x, y] = ' ';
                    }
                }
            }
            return sprite;
        }
        /// <summary>
        /// A method that draws the previously read sprite.
        /// </summary>
        /// <param name="sprite"></param>
        /// <param name="position"></param>
        static void DrawSprite(Sprite sprite, Point position)
        {
            for (int y = 0; y < sprite.Height; y++)
            {
                for (int x = 0; x < sprite.Width; x++)
                {
                    if (position.X + x < NextScreen.Width && position.Y + y < NextScreen.Height)
                    {
                        if (sprite.CharacterMap[x, y] == '.')
                        {
                            ScreenWrite(position.X + x, position.Y + y, ' ');
                        }
                        else if (sprite.CharacterMap[x, y] != ' ')
                        {
                            Screen.ForegroundColor = sprite.Color;
                            ScreenWrite(position.X + x, position.Y + y, sprite.CharacterMap[x, y]);
                        }
                    }
                }
            }
        }

        static void DrawString(string text, Point point)
        {
            if (point.Y >= NextScreen.Height || point.Y < 0 || point.X >= NextScreen.Width || point.X + text.Length < 0)
            {
                return;
            }

            if (point.X < 0)
            {
                int textStartIndex = Math.Abs(point.X);
                text = text.Substring(textStartIndex);
                ScreenWrite(0, point.Y, text);
            }
            else if (point.X + text.Length > NextScreen.Width)
            {
                int textLength = NextScreen.Width - point.X;
                text = text.Substring(0, textLength);
                ScreenWrite(point.X, point.Y, text);
            }
            else
            {
                ScreenWrite(point.X, point.Y, text);
            }
        }
        static void DrawShip()
        {
            Sprite currentShipSprite = PlayerShip.Sprites[PlayerShip.CardinalDirection];
            DrawSprite(currentShipSprite, PlayerShip.Position);
        }
        static void DrawIsland(Island island)
        {
            Sprite sprite = island.Sprite;
            Point position = island.Position;
            Landmark Landmark = island.Landmark;
            bool explored = island.Explored;

            Screen.ForegroundColor = sprite.Color;
            for (int y = 0; y < sprite.Height; y++)
            {
                for (int x = 0; x < sprite.Width; x++)
                {
                    int globalX = position.X + x;
                    int globalY = position.Y + y;
                    Point global = new Point(globalX, globalY);

                    if (explored == true && sprite.CharacterMap[x, y] == 'N')
                    {
                        DrawString(island.Name, global);
                    }
                    else if (sprite.CharacterMap[x, y] == '$')
                    {
                        Screen.ForegroundColor = Landmark.Color;
                        DrawString(Landmark.LandmarkSymbol, global);
                    }
                    else if (globalX < NextScreen.Width && globalX >= 0 && globalY < NextScreen.Height && globalY >= 0)
                    {

                        if (explored == false && sprite.CharacterMap[x, y] == 'N')
                        {
                            ScreenWrite(globalX, globalY, ' ');
                        }
                        else if (sprite.CharacterMap[x, y] == '.')
                        {
                            ScreenWrite(globalX, globalY, ' ');
                        }
                        else if (sprite.CharacterMap[x, y] != ' ')
                        {
                            Screen.ForegroundColor = sprite.Color;
                            ScreenWrite(globalX, globalY, sprite.CharacterMap[x, y]);
                        }
                    }
                }
            }
        }
        static void DrawFogOfWar()
        {

        }
        static void DrawMap()
        {
            Screen.BackgroundColor = ConsoleColor.DarkBlue;
            NextScreen.Clear();
            foreach (Island island in Islands)
            {
                DrawIsland(island);

            }
            DrawShip();
        }
        /*
        static Point GenerateIslandPosition()
        {
            //Generate a random map position.

            //Check that the position doesn't clash with already made islands.

            // if it clashes, make a new random position, if it doesn't clash return the position for the island.
        }*/

        static bool DoesOverlapIsland(Point mapGlobalPositionCenter, int radius, out Island overlappingIsland)
        {
            // Go over all points
            for (int offsetY = -radius; offsetY <= radius; offsetY++)
            {
                for (int offsetX = -radius; offsetX <= radius; offsetX++)
                {
                    Point currentPoint = new Point(mapGlobalPositionCenter.X + offsetX, mapGlobalPositionCenter.Y + offsetY);
                    bool doesOverlap = DoesOverlapIsland(currentPoint, out overlappingIsland);
                    if (doesOverlap)
                    {
                        return true;
                    }
                }
            }
            overlappingIsland = null;
            return false;
        }
        static bool DoesOverlapIsland(Point mapGlobalPosition, out Island overlappingIsland)
        {
            foreach (Island island in Islands)
            {
                // Calculate local coordinates relative to the island
                Point spriteCoordinates = new Point(mapGlobalPosition.X - island.Position.X, mapGlobalPosition.Y - island.Position.Y);

                //Check to see if we are out of bounds of the sprite.
                if (spriteCoordinates.Y < 0 || spriteCoordinates.Y >= island.Sprite.Height || spriteCoordinates.X < 0 || spriteCoordinates.X >= island.Sprite.Width)
                {
                    continue;
                }

                //If there is character at spriteCoordinates, return true.
                char spriteCharacter = island.Sprite.CharacterMap[spriteCoordinates.X, spriteCoordinates.Y];
                if (spriteCharacter != ' ' && spriteCharacter != 'N')
                {
                    overlappingIsland = island;
                    return true;
                }
            }
            overlappingIsland = null;
            return false;
        }

        /// <summary>
        /// This method prints the text within the confines of the console screen.
        /// </summary>
        /// <param name="text"></param>
        static void Print(string text)
        {
            //Split text into lines that don't exceed the window width.
            int maximumLineLength = Console.WindowWidth - 1;
            MatchCollection lineMatches = Regex.Matches(text, @"(.{1," + maximumLineLength + @"})(?:\s|$)");


            //Output each line.
            foreach (Match match in lineMatches)
            {
                Console.WriteLine(match.Groups[0].Value);
                Thread.Sleep(PrintPauseMilliseconds);
            }
        }
        #region introscreens
        /// <summary>
        /// This Method Presents the Title Screen, presenting the player with the game that they will be playing.
        /// </summary>
        static void PresentTitleScreen()
        {
            Console.Clear();
            string title = "OHOY! - A Text-Based Exploration Prototype";
            Console.SetCursorPosition((Console.WindowWidth - title.Length) / 2, Console.WindowHeight / 2);
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Print(title);
            Console.SetCursorPosition((Console.WindowWidth - title.Length) / 2, (Console.WindowHeight / 2) + 3);
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
        /// <summary>
        /// This method introduces the player to the story and explains what the game will be about.
        /// </summary>
        static void PresentStoryScreen()
        {
            Console.Clear();
            string story = File.ReadAllText("story.txt");
            Console.SetCursorPosition(0, Console.WindowHeight / 3);

            Print(story);

            Console.SetCursorPosition(Console.WindowWidth / 3, (Console.WindowHeight / 3) * 2);
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
        /// <summary>
        /// This method explain the few mechanics and keys needed to know.
        /// </summary>
        static void PresentTutorialScreen()
        {
            Console.Clear();
            string[] tutorial = File.ReadAllLines("tutorial.txt");
            Console.SetCursorPosition(0, Console.WindowHeight / 3);
            Print(tutorial[0]);
            Console.SetCursorPosition(Console.WindowWidth / 3, Console.WindowHeight / 2);
            Console.WriteLine("Press any key to set sail!");
            Console.ReadKey();
        }
        #endregion

        /// <summary>
        /// This method houses the actual gameplay-loop, covering everything from sailing to fighting and clues.
        /// </summary>
        /// <returns>A boolean whether you have won at the treasure island.</returns>
        static bool DoGameplayLoop()
        {
            while (true)
            {
                DoSailingLoop();

                //TODO: Figure out if on treasure island.
                bool onTreasureIsland = false;

                bool wonBattle = DoBattle(onTreasureIsland);


                if (wonBattle)
                {
                    if (onTreasureIsland)
                    {
                        return true;
                    }
                    else
                    {
                        ReceiveClue();
                        PresentJournalScreen(true);
                    }
                }
                else
                {
                    return false;
                }
            }
        }

        static void DoSailingLoop()
        {
            while (true)
            {

                //Draw Map
                DrawMap();
                DrawNextScreen();

                //Handle input
                ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                ConsoleKey pressedKey = keyInfo.Key;

                //Figure out where we want to go
                Point shipShipCenter = new Point(4, 2);
                Point mapShipCenter = new Point(PlayerShip.Position.X + shipShipCenter.X, PlayerShip.Position.Y + shipShipCenter.Y);
                Point pointIWantToGoTo = mapShipCenter;

                if (pressedKey == ConsoleKey.RightArrow && PlayerShip.Position.X < AsciiSeaMap.Width)
                {
                    PlayerShip.CardinalDirection = CardinalDirection.East;
                    pointIWantToGoTo.X++;
                }
                else if (pressedKey == ConsoleKey.LeftArrow && PlayerShip.Position.X > 0)
                {
                    PlayerShip.CardinalDirection = CardinalDirection.West;
                    pointIWantToGoTo.X--;
                }
                else if (pressedKey == ConsoleKey.DownArrow && PlayerShip.Position.Y < AsciiSeaMap.Height)
                {
                    PlayerShip.CardinalDirection = CardinalDirection.South;
                    pointIWantToGoTo.Y++;

                }
                else if (pressedKey == ConsoleKey.UpArrow && PlayerShip.Position.Y > 0)
                {
                    PlayerShip.CardinalDirection = CardinalDirection.North;
                    pointIWantToGoTo.Y--;
                }

                //Figure out if the point I want to go to is empty/valid.
                bool validSpace = !DoesOverlapIsland(pointIWantToGoTo, 1, out _);
                //Move the ship and camera if there is not an island at the position you are trying to go to.
                if (validSpace)
                {
                    PlayerShip.Position = new Point(pointIWantToGoTo.X - shipShipCenter.X, pointIWantToGoTo.Y - shipShipCenter.Y);
                    /*PlayerCamera.Position = PlayerShip.Position;*/
                }
            }
        }

        static void PresentJournalScreen(bool typeOutLastClue)
        {

        }

        static bool DoBattle(bool isBossBattle)
        {
            //TODO: Integrate Anton Prototype
            return true;
        }

        static void ReceiveClue()
        {

        }
        static void PresentWinScreen()
        {

        }
        static void PresentGameOverScreen()
        {

        }
        static void Main(string[] args)
        {
            LoadData();

            //Main Game Loop
            while (true)
            {
                InitializeObjects();
                /*
                PresentTitleScreen();
                PresentStoryScreen();
                PresentTutorialScreen();
                */
                bool foundTreasure = DoGameplayLoop();
                if (foundTreasure)
                {
                    PresentWinScreen();
                }
                else
                {
                    PresentGameOverScreen();
                }
            }
        }
    }
}