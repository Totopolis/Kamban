using Kamban.MatrixControl;
using Kamban.Model;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kamban.ViewModels
{
    public partial class BoardViewModel
    {
        private async Task HeadRenameCommandExecute(IDim head)
        {
            var headTxt = head is ColumnViewModel ? "column" : "row";
            headTxt += $" {head.Caption}";

            var ts = await dialCoord
                .ShowInputAsync(this, "Warning", $"Enter new name for {headTxt}",
                    new MetroDialogSettings()
                    {
                        AffirmativeButtonText = "OK",
                        NegativeButtonText = "Cancel",
                        DefaultText = head.Caption
                    });

            if (string.IsNullOrEmpty(ts))
                return;

            var column = head as ColumnViewModel;
            var row = head as RowViewModel;

            if (column!=null)
                column.Caption = ts;
            else
                row.Caption = ts;
        }

        private async Task HeadDeleteCommandExecute(IDim head)
        {
            var column = head as ColumnViewModel;
            var row = head as RowViewModel;

            var headTxt = head is ColumnViewModel ? "column" : "row";
            headTxt += $" '{head.Caption}'";

            if (column != null && Columns.Count <= 1) return;
            if (row != null && Rows.Count <= 1) return;

            var ts = await dialCoord.ShowMessageAsync(this, "Warning",
                $"Are you shure to delete {headTxt}?"
                , MessageDialogStyle.AffirmativeAndNegative);

            if (ts == MessageDialogResult.Negative)
                return;

            // delete head and move cards from deleted cells to first head
            if (column != null)
            {
                prjService.DeleteColumnAsync(column.Id);
                var firstColumn = Columns.OrderBy(x => x.Order).First();

                var cards = Cards.Where(x => (int)x.ColumnDeterminant == column.Id);
                foreach (CardViewModel it in cards)
                    it.ColumnDeterminant = firstColumn.Determinant;

                // remove after Matrix update !!!
                Columns.Remove(column);
            }
            else
            {
                prjService.DeleteRowAsync(row.Id);
                var firstRow = Rows.OrderBy(x => x.Order).First();

                var cards = Cards.Where(x => (int)x.RowDeterminant == row.Id);
                foreach (CardViewModel it in cards)
                    it.RowDeterminant = firstRow.Determinant;

                // remove after Matrix update !!!
                Rows.Remove(row);
            }

            NormalizeGridCommand.Execute().Subscribe();
            await RefreshContent();
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
                .ShowInputAsync(this, "Info", $"Enter new name",
                    new MetroDialogSettings()
                    {
                        AffirmativeButtonText = "OK",
                        NegativeButtonText = "Cancel",
                        DefaultText = head.Caption
                    });

            if (string.IsNullOrEmpty(ts))
                return;

            var column = head as ColumnViewModel;
            var row = head as RowViewModel;

            if (column != null)
            {
                ColumnInfo ci = new ColumnInfo
                {
                    Name = ts,
                    BoardId = column.Info.BoardId
                };

                prjService.CreateOrUpdateColumnAsync(ci);
                var indx = Columns.IndexOf(head) + after;
                Columns.Insert(indx, new ColumnViewModel(ci));

                for (int i = 0; i < Columns.Count; i++)
                {
                    var cvm = Columns[i] as ColumnViewModel;
                    cvm.Order = i;
                }
            }
            else
            {
                RowInfo ri = new RowInfo
                {
                    Name = ts,
                    BoardId = row.Info.BoardId
                };

                prjService.CreateOrUpdateRowAsync(ri);
                var indx = Rows.IndexOf(head) + after;
                Rows.Insert(indx, new RowViewModel(ri));

                for (int i = 0; i < Rows.Count; i++)
                {
                    var rvm = Rows[i] as RowViewModel;
                    rvm.Order = i;
                }
            }

            NormalizeGridCommand.Execute().Subscribe();
        }

        private async Task RenameBoardCommandExecute()
        {
            var oldName = CurrentBoard.Name;
            var str = $"Enter new board name for \"{oldName}\"";
            var newName = await dialCoord
            .ShowInputAsync(this, "Board rename", str,
                new MetroDialogSettings()
                {
                    AffirmativeButtonText = "OK",
                    NegativeButtonText = "Cancel",
                    DefaultText = SelectedColumn?.Name
                });

            if (string.IsNullOrEmpty(newName))
                return;

            CurrentBoard.Name = newName;
            prjService.CreateOrUpdateBoardAsync(CurrentBoard);
            Title = newName;

            BoardsMenuItems
                .Where(x => x.Name == oldName)
                .First()
                .Name = newName;
        }

        private async Task DeleteCardCommandExecuteAsync(ICard cvm)
        {
            var ts = await dialCoord.ShowMessageAsync(this, "Warning",
                $"Are you shure to delete issue '{cvm.Header}'?"
                , MessageDialogStyle.AffirmativeAndNegative);

            if (ts == MessageDialogResult.Negative)
                return;

            prjService.DeleteIssueAsync(cvm.Id);
            Cards.Remove(cvm);
        }

    }//end of class
}
