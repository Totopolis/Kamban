using Kamban.Model;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kamban.MatrixControl
{
    /// <summary>
    /// Row or column description
    /// </summary>
    public interface IDim
    {
        string Caption { get; set; }
        object Determinant { get; set; }
        int Size { get; set; }
    }

    public class ColumnViewModel : ReactiveObject, IDim
    {
        private readonly ColumnInfo columnInfo;

        public ColumnViewModel(ColumnInfo ci)
        {
            columnInfo = ci;
        }

        public string Caption { get { return columnInfo.Name; } set { columnInfo.Name = value; } }
        public object Determinant { get => columnInfo.Id; set => throw new NotImplementedException(); }
        public int Size { get => columnInfo.Height; set => throw new NotImplementedException(); }
    }

    public class RowViewModel : ReactiveObject, IDim
    {
        private readonly RowInfo rowInfo;

        public RowViewModel(RowInfo ri)
        {
            rowInfo = ri;
        }

        public string Caption { get { return rowInfo.Name; } set { rowInfo.Name = value; } }
        public object Determinant { get => rowInfo.Id; set => throw new NotImplementedException(); }
        public int Size { get => rowInfo.Height; set => throw new NotImplementedException(); }
    }

    /*public class Cell : ReactiveObject
    {
        public Cell(Dimension row, Dimension column)
        {
            Column = column;
            Row = row;

            Cards = new ReactiveList<Card>();
        }

        public Dimension Column { get; private set; }
        public Dimension Row { get; private set; }

        public ReactiveList<Card> Cards { get; set; }
    }*/

    public class Card : ReactiveObject
    {
        [Reactive] public string Header { get; set; }
        [Reactive] public string Color { get; set; }

        [Reactive] public object ColumnDeterminant { get; set; }
        [Reactive] public object RowDeterminant { get; set; }
    }
}
