using System.Collections.Generic;

public static class MatchesUtility
{
    public const int MINIMUM_MATCH_AMOUNT = 3;


    /// <summary>
    /// Check if the given tileType at the given [row, col] creates at least a <MINIMUM_MATCH_AMOUNT> tiles match in any direction. 
    /// </summary>
    /// <param name="row"> The row to check for a tiles match. </param>
    /// <param name="col"> The column to check for a tiles match. </param>
    /// <param name="tileType"> The tile type at [row, col]. </param>
    /// <returns> True if a match exists. </returns>
    public static bool IsPartOfMatch(int row, int col, string tileType)
    {
        int idx;
        int totalInRow = 1;
        int totalInColumn = 1;
        for (idx = 1; idx < MINIMUM_MATCH_AMOUNT; idx++)
        {
            if ((row - idx) < 0 || !Board.Instance.Tiles[row - idx, col] || Board.Instance.Tiles[row - idx, col].TileType != tileType)
                break;
            else
                totalInRow++;
        }

        for (idx = 1; idx < MINIMUM_MATCH_AMOUNT; idx++)
        {
            if ((row + idx) >= Board.Instance.COUNT_ROWS || !Board.Instance.Tiles[row + idx, col] || Board.Instance.Tiles[row + idx, col].TileType != tileType)
                break;
            else
                totalInRow++;
        }
        if (totalInRow >= MINIMUM_MATCH_AMOUNT)
            return true;

        for (idx = 1; idx < MINIMUM_MATCH_AMOUNT; idx++)
        {
            if ((col - idx) < 0 || !Board.Instance.Tiles[row, col - idx] || Board.Instance.Tiles[row, col - idx].TileType != tileType)
                break;
            else
                totalInColumn++;
        }

        for (idx = 1; idx < MINIMUM_MATCH_AMOUNT; idx++)
        {
            if ((col + idx) >= Board.Instance.COUNT_COLUMNS || !Board.Instance.Tiles[row, col + idx] || Board.Instance.Tiles[row, col + idx].TileType != tileType)
                break;
            else
                totalInColumn++;
        }
        if (totalInColumn >= MINIMUM_MATCH_AMOUNT)
            return true;

        return false;
    }

    public static (int, int) IsOneMoveFromMatch(int row, int col, string tileType)
    {
        Tile originalTile = Board.Instance.Tiles[row, col];

        if (row + 1 < Board.Instance.COUNT_ROWS && TilesUtility.IsTilePowerupEnabled((row + 1, col)))
        {
            Board.Instance.Tiles[row, col] = Board.Instance.Tiles[row + 1, col];
            if (originalTile.TileType != Board.Instance.Tiles[row + 1, col].TileType && IsPartOfMatch(row + 1, col, tileType))
            {
                Board.Instance.Tiles[row, col] = originalTile;
                return (row + 1, col);
            }
        }

        if (row - 1 >= 0 && TilesUtility.IsTilePowerupEnabled((row - 1, col)))
        {
            Board.Instance.Tiles[row, col] = Board.Instance.Tiles[row - 1, col];
            if (originalTile.TileType != Board.Instance.Tiles[row - 1, col].TileType && IsPartOfMatch(row - 1, col, tileType))
            {
                Board.Instance.Tiles[row, col] = originalTile;
                return (row - 1, col);
            }
        }

        if (col + 1 < Board.Instance.COUNT_COLUMNS && TilesUtility.IsTilePowerupEnabled((row, col + 1)))
        {
            Board.Instance.Tiles[row, col] = Board.Instance.Tiles[row, col + 1];
            if (originalTile.TileType != Board.Instance.Tiles[row, col + 1].TileType && IsPartOfMatch(row, col + 1, tileType))
            {
                Board.Instance.Tiles[row, col] = originalTile;
                return (row, col + 1);
            }
        }

        if (col - 1 >= 0 && TilesUtility.IsTilePowerupEnabled((row, col - 1)))
        {
            Board.Instance.Tiles[row, col] = Board.Instance.Tiles[row, col - 1];
            if (originalTile.TileType != Board.Instance.Tiles[row, col - 1].TileType && IsPartOfMatch(row, col - 1, tileType))
            {
                Board.Instance.Tiles[row, col] = originalTile;
                return (row, col - 1);
            }
        }

        Board.Instance.Tiles[row, col] = originalTile;
        return Utils.GetTupleResetValues();
    }

    /// <param name="row"> The row to get the tile match. </param>
    /// <param name="col"> The column to get the tile match. </param>
    /// <param name="tileType"> The tile type at [row, col]. </param>
    /// <returns> Return a list of the indices of all the tiles that are a part of a match with the given tile at (row, col).  </returns>
    public static List<(int, int)> GetMatchIndices(int row, int col, string tileType)
    {
        List<(int, int)> matches = new List<(int, int)>();
        List<(int, int)> rowMatches = new List<(int, int)>();
        List<(int, int)> colMatches = new List<(int, int)>();
        for (int rowIdx = 1; rowIdx < Board.Instance.COUNT_ROWS; rowIdx++)
        {
            if ((row - rowIdx) < 0)
                break;

            if (Board.Instance.Tiles[row - rowIdx, col].TileType == tileType)
                rowMatches.Add((row - rowIdx, col));
            else
                break;
        }

        for (int rowIdx = 1; rowIdx < Board.Instance.COUNT_ROWS; rowIdx++)
        {
            if ((row + rowIdx) >= Board.Instance.COUNT_ROWS)
                break;

            if (Board.Instance.Tiles[row + rowIdx, col].TileType == tileType)
                rowMatches.Add((row + rowIdx, col));
            else
                break;
        }

        for (int colIdx = 1; colIdx < Board.Instance.COUNT_COLUMNS; colIdx++)
        {
            if ((col - colIdx) < 0)
                break;

            if (Board.Instance.Tiles[row, col - colIdx].TileType == tileType)
                colMatches.Add((row, col - colIdx));
            else
                break;
        }

        for (int colIdx = 1; colIdx < Board.Instance.COUNT_COLUMNS; colIdx++)
        {
            if ((col + colIdx) >= Board.Instance.COUNT_COLUMNS)
                break;

            if (Board.Instance.Tiles[row, col + colIdx].TileType == tileType)
                colMatches.Add((row, col + colIdx));
            else
                break;
        }

        // If we've got (MINIMUM_MATCH_AMOUNT-1) or more matches on adjacent columns.
        if (colMatches.Count >= (MINIMUM_MATCH_AMOUNT - 1))
            matches = colMatches;

        // If we've got (MINIMUM_MATCH_AMOUNT-1) or more matches on adjacent rows.
        if (rowMatches.Count >= (MINIMUM_MATCH_AMOUNT - 1))
            matches.AddRange(rowMatches);

        // If we've got matches then add the given tile indices.
        if (matches.Count > 0)
            matches.Add((row, col));

        return matches;
    }

    
}
