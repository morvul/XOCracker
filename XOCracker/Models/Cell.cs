using System.Drawing;
using XOCracker.Enums;

namespace XOCracker.Models
{
    public struct Cell
    {
        public Cell(CellType cellType, int row, int column, int x = 0, int y = 0, int height = 0, int width = 0)
        {
            Row = row;
            Column = column;
            CellType = cellType;
            Height = height;
            Width = width;
            X = x;
            Y = y;
        }

        public Cell(CellType firstCellType, int row, int column, Rectangle firstCell)
            : this(firstCellType, row, column, firstCell.X, firstCell.Y, firstCell.Height, firstCell.Width)
        {
        }

        public int Row { get; set; }

        public int Column { get; set; }

        public int X { get; set; }

        public int Y { get; set; }

        public int Height { get; set; }

        public int Width { get; set; }

        public CellType CellType { get; set; }
    }
}
