using System;

public struct SCrabapplesGameState
{
    public int currentTurn;
    public int playerBudsToPlace;
    public int score;

    [System.Serializable]
    public struct SCellState
    {
        public bool occupied;
        public bool red;
        public bool green;
        public bool blue;
        public bool calcified;
        public int x;
        public int y;

        public SCellState(bool occupied, bool red, bool green, bool blue, bool calcified, int x, int y)
        {
            this.occupied = occupied;
            this.red = red;
            this.green = green;
            this.blue = blue;
            this.calcified = calcified;
            this.x = x;
            this.y = y;
        }
    }

    public SCellState[,] board;
    public int width;
    public int height;

    public SCrabapplesGameState(int sizeX, int sizeY, int turn, int budsLeft, int score)
    {
        board = new SCellState[sizeX, sizeY];
        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                board[x, y] = new SCellState(false, false, false, false, false, x, y);
            }
        }
        width = sizeX;
        height = sizeY;
        currentTurn = turn;
        playerBudsToPlace = budsLeft;
        this.score = score;
    }

    public SCrabapplesGameState Clone()
    {
        SCrabapplesGameState clonedBoard = new SCrabapplesGameState(width, height, currentTurn, playerBudsToPlace, score);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                clonedBoard.board[x, y] = board[x, y];
            }
        }
        return clonedBoard;
    }

    public SCrabapplesGameState GetNextTurn()
    {
        // 1a Any plant with two or three neighbors survives.
        // 1b A colorless bud with two or three same-colored neighbors becomes that color.
        // 1c A colorless bud with three different colored neighbors becomes tri-colored.
        // 1d Rocks adjacent to a tri-color plant will turn back into buds.
        // (1e a bud with two or more neighbors of two colors remains a bud)
        // (1f a crystal turned into a plant will be a plant of that color, except for two-color crystals which become buds)
        // 2a Any ground with three neighbors grows a colorless bud, at least one must be alive.
        // 2b If there are 2 - 3 neighbors of the same color it grows that color.
        // 2c If all neighbors are different colors it grows tri-colored.
        // 2d Otherwise it grows a colorless bud.
        // (2e If two neighbors have two colors, grow colorless)
        // 3a All plants that have less than two or more than three neighbors wither to empty ground.
        // 3b Colorless buds that wither become rocks, which act as neighbors but cannot grow nor wither.
        // (3c Some maps have red / green / blue crystals, which are colored rocks that can have one two or three colors.)
        SCrabapplesGameState nextBoardState = new SCrabapplesGameState(width, height, currentTurn + 1, playerBudsToPlace, score);
        for (int currentX = 0; currentX < width; currentX++)
        {
            for (int currentY = 0; currentY < height; currentY++)
            {
                SCellState oldCell = board[currentX, currentY];
                SCellState newCell = new SCellState();
                newCell.x = currentX;
                newCell.y = currentY;
                CountAdjacents(currentX, currentY, out int adjacentNeighbors, out int adjacentLiveNeighbors,
                    out int adjacentRed, out int adjacentGreen, out int adjacentBlue, out int adjacentTricolored);
                if (oldCell.calcified)
                {
                    if (adjacentTricolored > 0)
                    {
                        // 1d Rocks adjacent to a tri-color plant will turn back into buds.
                        newCell.occupied = true;
                        // (1f a crystal turned into a plant will be a plant of that color, except for two-color crystals which become buds)
                        if (oldCell.red && oldCell.green && oldCell.blue)
                        {
                            // Tricolor crystal becomes tricolor plant.
                            newCell.red = true;
                            newCell.green = true;
                            newCell.blue = true;
                        }
                        else if (!oldCell.red && !oldCell.green && !oldCell.blue)
                        {
                            // Rock becomes colorless bud
                        }
                        else if (oldCell.red ^ oldCell.green ^ oldCell.blue)
                        {
                            // Single-color crystal becomes single-color plant
                            newCell.red = oldCell.red;
                            newCell.green = oldCell.green;
                            newCell.blue = oldCell.blue;
                        }
                        else
                        {
                            // Two-color crystal becomes colorless bud
                        }
                    }
                    else
                    {
                        // 3b Colorless buds that wither become rocks, which act as neighbors but cannot grow nor wither.
                        newCell = oldCell;
                    }
                }
                else if (oldCell.occupied)
                {
                    if (adjacentNeighbors == 2 || adjacentNeighbors == 3)
                    {
                        // 1a Any plant with two or three neighbors survives.
                        newCell = oldCell;
                        if (!oldCell.red && !oldCell.green && !oldCell.blue)
                        {
                            // We are a colorless bud, check if we should change color.
                            if (adjacentRed > 0 && adjacentGreen > 0 && adjacentBlue > 0)
                            {
                                // 1c A colorless bud with three different-colored neighbors becomes tri-colored.
                                newCell.red = true;
                                newCell.green = true;
                                newCell.blue = true;
                            }
                            else if (adjacentRed <= 1 && adjacentGreen <= 1 && adjacentBlue <= 1)
                            {
                                // No reason to change color.
                            }
                            else
                            {
                                // 1b A colorless bud with two or three same-colored neighbors becomes that color.
                                if (adjacentRed >= 2)
                                {
                                    newCell.red = true;
                                }
                                else if (adjacentGreen >= 2)
                                {
                                    newCell.green = true;
                                }
                                else if (adjacentBlue >= 2)
                                {
                                    newCell.blue = true;
                                }

                                if (!(newCell.red ^ newCell.green ^ newCell.blue))
                                {
                                    // (1e a bud with two or more neighbors of two colors remains a bud)
                                    newCell.red = false;
                                    newCell.green = false;
                                    newCell.blue = false;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (!oldCell.red && !oldCell.green && !oldCell.blue)
                        {
                            // 3b Colorless buds that wither become rocks, which act as neighbors but cannot grow nor wither.
                            newCell.occupied = true;
                            newCell.calcified = true;
                        }
                        else
                        {
                            // 3a All plants that have less than two or more than three neighbors wither to empty ground.
                            newCell.occupied = false;
                        }
                    }
                }
                else
                {
                    if (adjacentNeighbors == 3 && adjacentLiveNeighbors > 0)
                    {
                        // 2a Any ground with three neighbors grows a colorless bud, at least one must be alive.
                        newCell.occupied = true;
                        if (adjacentRed > 0 && adjacentGreen > 0 && adjacentBlue > 0)
                        {
                            // 2c If all neighbors are different colors it grows tri-colored.
                            newCell.red = true;
                            newCell.green = true;
                            newCell.blue = true;
                        }
                        else
                        {
                            // 2b If there are 2 - 3 neighbors of the same color it grows that color.
                            if (adjacentRed >= 2)
                            {
                                newCell.red = true;
                            }
                            else if (adjacentGreen >= 2)
                            {
                                newCell.green = true;
                            }
                            else if (adjacentBlue >= 2)
                            {
                                newCell.blue = true;
                            }
                            // 2d Otherwise it grows a colorless bud.
                            // (2e If two neighbors have two colors, grow colorless)
                            if (!(newCell.red ^ newCell.green ^ newCell.blue))
                            {
                                newCell.red = false;
                                newCell.green = false;
                                newCell.blue = false;
                            }
                        }
                    }
                }
                nextBoardState.board[currentX, currentY] = newCell;
            }
        }
        return nextBoardState;
    }

    public int GetCount(LevelData.EVictoryCheck check)
    {
        int count = 0;
        if (check == LevelData.EVictoryCheck.Turns)
        {
            count = currentTurn;
        }
        else
        {
            foreach (SCellState cell in board)
            {
                bool shouldAddOne = false;
                switch (check)
                {
                    case LevelData.EVictoryCheck.AnyPlants:
                        shouldAddOne = cell.occupied && !cell.calcified;
                        break;
                    case LevelData.EVictoryCheck.RedPlants:
                        shouldAddOne = cell.red && !cell.calcified
                            && !cell.green && !cell.blue;
                        break;
                    case LevelData.EVictoryCheck.GreenPlants:
                        shouldAddOne = cell.green && !cell.calcified
                            && !cell.red && !cell.blue;
                        break;
                    case LevelData.EVictoryCheck.BluePlants:
                        shouldAddOne = cell.blue && !cell.calcified
                            && !cell.green && !cell.red;
                        break;
                    case LevelData.EVictoryCheck.TricolorPlants:
                        shouldAddOne = cell.red && !cell.calcified
                            && cell.green && cell.blue;
                        break;
                    case LevelData.EVictoryCheck.CalcifiedBuds:
                        shouldAddOne = cell.calcified;
                        break;
                    case LevelData.EVictoryCheck.Turns:
                        break;
                    default:
                        break;
                }
                if (shouldAddOne)
                {
                    count++;
                }
            }
        }
        return count;
    }

    void CountAdjacents(int x, int y, out int neighbors, out int liveNeighbors,
        out int red, out int green, out int blue, out int tricolored)
    {
        neighbors = 0;
        liveNeighbors = 0;
        red = 0;
        green = 0;
        blue = 0;
        tricolored = 0;

        int startX = System.Math.Max(0, x - 1);
        int startY = System.Math.Max(0, y - 1);
        for (int currentX = startX; currentX <= x + 1 && currentX < width; currentX++)
        {
            for (int currentY = startY; currentY <= y + 1 && currentY < height; currentY++)
            {
                if (!(currentX == x && currentY == y))
                {
                    SCellState adjacentCell = board[currentX, currentY];
                    if (adjacentCell.occupied)
                    {
                        neighbors++;
                        if (!adjacentCell.calcified)
                        {
                            liveNeighbors++;
                        }
                    }
                    if (adjacentCell.red)
                    {
                        red++;
                    }
                    if (adjacentCell.green)
                    {
                        green++;
                    }
                    if (adjacentCell.blue)
                    {
                        blue++;
                    }
                    if (adjacentCell.red && adjacentCell.green && adjacentCell.blue)
                    {
                        tricolored++;
                    }
                }
            }
        }
        if (neighbors > 0)
        {
            UnityEngine.Debug.LogFormat("CountAdjacents, currentlyOccupied: {6} r: {0} g: {1} b: {2}, neighbors: {3}, liveNeighbors: {4}, tricolored: {5}",
                red, green, blue, neighbors, liveNeighbors, tricolored, board[x, y].occupied);
        }
    }
}
