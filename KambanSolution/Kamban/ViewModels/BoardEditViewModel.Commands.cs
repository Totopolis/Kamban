using System;
using System.Linq;
using System.Threading.Tasks;
using DynamicData;
using Kamban.ViewModels.Core;
using MahApps.Metro.Controls.Dialogs;

namespace Kamban.ViewModels
{
    public partial class BoardEditViewModel
    {
        private async Task HeadRenameCommandExecute(IDim head)
        {
            var ts = await dialCoord
                .ShowInputAsync(this, "Warning", $"Enter new name for {head.FullName}",
                    new MetroDialogSettings
                    {
                        AffirmativeButtonText = "OK",
                        NegativeButtonText = "Cancel",
                        DefaultText = head.Name
                    });

            if (string.IsNullOrEmpty(ts))
                return;

            switch (head)
            {
                case ColumnViewModel column:
                    column.Name = ts;
                    break;
                case RowViewModel row:
                    row.Name = ts;
                    break;
            }
        }

        private async Task HeadDeleteCommandExecute(IDim head)
        {
            switch (head)
            {
                case ColumnViewModel _ when Box.Columns.Count <= 1:
                case RowViewModel _ when Box.Rows.Count <= 1:
                    await dialCoord.ShowMessageAsync(this, "Warning", $"Cannot remove {head.FullName}");
                    return;
            }

            var ts = await dialCoord.ShowMessageAsync(this, "Warning",
                $"Are you sure to delete {head.FullName}?"
                , MessageDialogStyle.AffirmativeAndNegative);

            if (ts == MessageDialogResult.Negative)
                return;

            EnableMatrix = false;

            // delete head and move cards from deleted cells to first head
            switch (head)
            {
                case ColumnViewModel column:
                {
                    // Shift cards
                    var firstColumn = Columns
                        .OrderBy(x => x.Order)
                        .First(x => x.Id != column.Id);
                    foreach (var it in cardList.Where(x => x.ColumnDeterminant == column.Id).ToList())
                        it.ColumnDeterminant = firstColumn.Id;

                    Box.Columns.Remove(column);
                }
                    break;
                case RowViewModel row:
                {
                    // Shift cards
                    var firstRow = Rows
                        .OrderBy(x => x.Order)
                        .First(x => x.Id != row.Id);
                    foreach (var it in cardList.Where(x => x.RowDeterminant == row.Id).ToList())
                        it.RowDeterminant = firstRow.Id;

                    Box.Rows.Remove(row);
                }
                    break;
            }

            // Rebuild Matrix
            EnableMatrix = true;

            NormalizeGridCommand
                .Execute()
                .Subscribe();
        }

        private async Task HeadDeleteCardsCommandExecute(IDim head)
        {
            var ts = await dialCoord.ShowMessageAsync(this, "Warning",
                $"Are you sure to delete all Cards in {head.FullName}?"
                , MessageDialogStyle.AffirmativeAndNegative);

            if (ts == MessageDialogResult.Negative)
                return;

            switch (head)
            {
                case ColumnViewModel column:
                {
                    // Remove Cards
                    foreach (var it in cardList.Where(x => x.ColumnDeterminant == column.Id).ToList())
                        Box.Cards.Remove(it);
                }
                    break;
                case RowViewModel row:
                {
                    // Remove Cards
                    foreach (var it in cardList.Where(x => x.RowDeterminant == row.Id).ToList())
                        Box.Cards.Remove(it);
                }
                    break;
            }
        }

        private async Task InsertHeadBeforeCommandExecute(IDim head)
        {
            await InsertHead(head, 0);
        }

        private async Task InsertHeadAfterCommandExecute(IDim head)
        {
            await InsertHead(head, 1);
        }

        private async Task InsertHead(IDim head, int after)
        {
            var ts = await dialCoord
                .ShowInputAsync(this, "Info", "Enter new name",
                    new MetroDialogSettings
                    {
                        AffirmativeButtonText = "OK",
                        NegativeButtonText = "Cancel",
                        DefaultText = head.Name
                    });

            if (string.IsNullOrEmpty(ts))
                return;

            EnableMatrix = false;

            switch (head)
            {
                case ColumnViewModel column:
                {
                    var tempColumns = Columns.ToList();
                    var index = tempColumns.IndexOf(head) + after;

                    var cvm = new ColumnViewModel
                    {
                        Name = ts,
                        BoardId = column.BoardId
                    };

                    tempColumns.Insert(index, cvm);
                    Box.Columns.Add(cvm);

                    var i = 0;
                    foreach (var it in tempColumns)
                    {
                        it.Order = i;
                        i++;
                    }
                }
                    break;
                case RowViewModel row:
                {
                    var tempRows = Rows.ToList();
                    var index = tempRows.IndexOf(head) + after;

                    var rvm = new RowViewModel
                    {
                        Name = ts,
                        BoardId = row.BoardId
                    };

                    tempRows.Insert(index, rvm);
                    Box.Rows.Add(rvm);

                    var i = 0;
                    foreach (var it in tempRows)
                    {
                        it.Order = i;
                        i++;
                    }
                }
                    break;
            }

            // Rebuild matrix
            this.EnableMatrix = true;

            NormalizeGridCommand
                .Execute()
                .Subscribe();
        }

        private async Task RenameBoardCommandExecute()
        {
            var oldName = CurrentBoard.Name;
            var str = $"Enter new board name for \"{oldName}\"";
            var newName = await dialCoord
                .ShowInputAsync(this, "Board rename", str,
                    new MetroDialogSettings
                    {
                        AffirmativeButtonText = "OK",
                        NegativeButtonText = "Cancel",
                        DefaultText = CurrentBoard?.Name
                    });

            if (string.IsNullOrEmpty(newName))
                return;

            CurrentBoard.Name = newName;
            Title = newName;
        }

        private async Task DeleteBoardCommandExecute()
        {
            var ts = await dialCoord.ShowMessageAsync(this, "Warning",
                $"Are you sure to delete board '{CurrentBoard.Name}'?"
                , MessageDialogStyle.AffirmativeAndNegative);

            if (ts == MessageDialogResult.Negative)
                return;

            // protect
            if (Box.Boards.Count <= 1)
                return;

            // Remove cards
            foreach (var card in cardList.ToList())
                Box.Cards.Remove(card);

            // Remove headers
            Box.Columns.Items
                .Where(x => x.BoardId == CurrentBoard.Id)
                .ToList()
                .ForEach(x => Box.Columns.Remove(x));

            Box.Rows.Items
                .Where(x => x.BoardId == CurrentBoard.Id)
                .ToList()
                .ForEach(x => Box.Rows.Remove(x));

            // Remove board
            Box.Boards.Remove(CurrentBoard);
            CurrentBoard = Box.Boards.Items.First();
        }

        private async Task DeleteCardCommandExecuteAsync(ICard cvm)
        {
            var ts = await dialCoord.ShowMessageAsync(this, "Warning",
                $"Are you sure you want to delete the card '{cvm.Header}'?"
                , MessageDialogStyle.AffirmativeAndNegative);

            if (ts == MessageDialogResult.Negative)
                return;

            Box.Cards.Remove(cvm as CardViewModel);
        }
    } //end of class
}