using System;
using System.Collections.Generic;
using System.Drawing;
using XOCracker.Enums;

namespace XOCracker
{
    public static class BotLogic
    {
        private static readonly Random Rd = new Random();

        #region Логика бота

        private static int _p1, _p2;

        public static Point? GetStep(CellType[,] board, int size, CellType side, ref double stepComplexity)
        {
            int row;
            int column;
            double[,] prob = new double[board.GetLength(0), board.GetLength(1)];
            double max = 0;
            stepComplexity = 0;
            CellType signEn = side == CellType.OCell ? CellType.XCell : CellType.OCell;
            List<Point> a = new List<Point>();
            for (row = 0; row < board.GetLength(0); row++)
                for (column = 0; column < board.GetLength(1); column++)
                {
                    if (!board[row, column].Equals(CellType.Free))
                    {
                        prob[row, column] = -1;
                        continue;
                    }

                    var k = GetRateV(row, column, side, board, size);
                    if (k + 1 >= size) k += 100500;
                    if (prob[row, column] < k + _p1 + _p2) prob[row, column] = k + _p1 + _p2 + 0.1;
                    k = GetRateV(row, column, signEn, board, size);
                    if (k + 1 >= size) k += 100;
                    if (prob[row, column] < k + _p1 + _p2) prob[row, column] = k + _p1 + _p2;

                    k = GetRateH(row, column, side, board, size);
                    if (k + 1 >= size) k += 100500;
                    if (prob[row, column] < k + _p1 + _p2) prob[row, column] = k + _p1 + _p2 + 0.1;
                    k = GetRateH(row, column, signEn, board, size);
                    if (k + 1 >= size) k += 100;
                    if ((prob[row, column] > 2) && (prob[row, column] == k + _p1 + _p2)) prob[row, column] += 0.1;
                    if (prob[row, column] < k + _p1 + _p2) prob[row, column] = k + _p1 + _p2;

                    k = GetRateMd(row, column, side, board, size);
                    if (k + 1 >= size) k += 100500;
                    if (prob[row, column] < k + _p1 + _p2) prob[row, column] = k + _p1 + _p2 + 0.1;
                    k = GetRateMd(row, column, signEn, board, size);
                    if (k + 1 >= size) k += 100;
                    if ((prob[row, column] > 2) && (prob[row, column] == k + _p1 + _p2)) prob[row, column] += 0.1;
                    if (prob[row, column] < k + _p1 + _p2) prob[row, column] = k + _p1 + _p2;

                    k = GetRateSd(row, column, side, board, size);
                    if (k + 1 >= size) k += 100500;
                    if (prob[row, column] < k + _p1 + _p2) prob[row, column] = k + _p1 + _p2 + 0.1;
                    k = GetRateSd(row, column, signEn, board, size);
                    if (k + 1 >= size) k += 100;
                    if ((prob[row, column] > 2) && (prob[row, column] == k + _p1 + _p2)) prob[row, column] += 0.1;
                    if (prob[row, column] < k + _p1 + _p2) prob[row, column] = k + _p1 + _p2;

                    if (max < prob[row, column]) max = prob[row, column];
                }
            for (row = 0; row < board.GetLength(0); row++)
                for (column = 0; column < board.GetLength(1); column++)
                    if (max == prob[row, column])
                    {
                        var selo = new Point(column, row);
                        a.Add(selo);
                    }

            stepComplexity = max > 100000 ? 0 : a.Count;
            return a.Count > 0 ? a[Rd.Next(a.Count)] : (Point?)null;
        }

        static int GetRateV(int i, int j, CellType side, CellType[,] board, int size)
        {
            int t = 1, k = 0, p = 1;
            _p1 = 0; _p2 = 0;
            while ((i + t < board.GetLength(0)) && side.Equals(board[i + t, j]))
            { t++; k++; }
            while ((i + t < board.GetLength(0)) && CellType.Free.Equals(board[i + t, j]))
            { t++; _p1 = 1; p++; }
            t = -1;
            while ((i + t > -1) && side.Equals(board[i + t, j])) { t--; k++; }
            while ((i + t > -1) && CellType.Free.Equals(board[i + t, j]))
            { t--; _p2 = 1; p++; }
            if ((k + p < size)) k = 0;
            return k;
        }

        static int GetRateH(int i, int j, CellType side, CellType[,] board, int size)
        {
            int t = 1, k = 0, p = 1;
            _p1 = 0; _p2 = 0;
            while ((j + t < board.GetLength(1)) && side.Equals(board[i, j + t])) { t++; k++; }
            while ((j + t < board.GetLength(1)) && CellType.Free.Equals(board[i, j + t]))
            { t++; _p1 = 1; p++; }
            t = -1;
            while ((j + t > -1) && side.Equals(board[i, j + t])) { t--; k++; }
            while ((j + t > -1) && CellType.Free.Equals(board[i, j + t]))
            { t--; _p2 = 1; p++; }
            if ((k + p < size))
                k = 0;
            return k;
        }

        static int GetRateMd(int i, int j, CellType side, CellType[,] board, int size)
        {
            int t = 1, k = 0, p = 1;
            _p1 = 0; _p2 = 0;
            while ((j + t < board.GetLength(1)) && (i + t < board.GetLength(0)) && side.Equals(board[i + t, j + t]))
            { t++; k++; }
            while ((j + t < board.GetLength(1)) && (i + t < board.GetLength(0)) &&
              CellType.Free.Equals(board[i + t, j + t]))
            { t++; _p1 = 1; p++; }
            t = -1;
            while ((j + t > -1) && (i + t > -1) && side.Equals(board[i + t, j + t]))
            { t--; k++; }
            while ((j + t > -1) && (i + t > -1) && CellType.Free.Equals(board[i + t, j + t]))
            { t--; _p2 = 1; p++; }
            if ((k + p < size)) k = 0;
            return k;
        }

        static int GetRateSd(int i, int j, CellType side, CellType[,] board, int size)
        {
            int t = 1, k = 0, p = 1;
            _p1 = 0; _p2 = 0;
            while ((j - t > -1) && (i + t < board.GetLength(0)) && side.Equals(board[i + t, j - t]))
            { t++; k++; }
            while ((j - t > -1) && (i + t < board.GetLength(0)) && CellType.Free.Equals(board[i + t, j - t]))
            { t++; _p1 = 1; p++; }
            t = -1;
            while ((j - t < board.GetLength(1)) && (i + t > -1) && side.Equals(board[i + t, j - t]))
            { t--; k++; }
            while ((j - t < board.GetLength(1)) && (i + t > -1) && CellType.Free.Equals(board[i + t, j - t]))
            { t--; _p2 = 1; p++; }
            if ((k + p < size)) k = 0;
            return k;
        }

        #endregion
    }
}
