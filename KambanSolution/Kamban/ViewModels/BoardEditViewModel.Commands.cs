using DynamicData;
using Kamban.MatrixControl;
using Kamban.Model;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kamban.ViewModels
{
    public partial class BoardEditViewModel
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
                var firstColumn = Columns.Items.OrderBy(x => x.Order).First();

                foreach (CardViewModel it in Cards.Items.Where(x => (int)x.ColumnDeterminant == column.Id))
                    it.ColumnDeterminant = firstColumn.Determinant;

                // remove after Matrix update !!!
                Columns.Remove(column);
            }
            else
            {
                prjService.DeleteRowAsync(row.Id);
                var firstRow = Rows.Items.OrderBy(x => x.Order).First();

                foreach (CardViewModel it in Cards.Items.Where(x => (int)x.RowDeterminant == row.Id))
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
                    BoardId = column.BoardId
                };

                prjService.CreateOrUpdateColumnAsync(ci);
                var indx = Columns.Items.IndexOf(head) + after;
                var temp = mapper.Map<ColumnInfo, ColumnViewModel>(ci);
                Columns.Insert(indx, temp);

                int i = 0;
                foreach (var it in Columns.Items)
                {
                    it.Order = i;
                    i++;
                }
            }
            else
            {
                RowInfo ri = new RowInfo
                {
                    Name = ts,
                    BoardId = row.BoardId
                };

                prjService.CreateOrUpdateRowAsync(ri);
                var indx = Rows.Items.IndexOf(head) + after;
                var temp = mapper.Map<RowInfo, RowViewModel>(ri);
                Rows.Insert(indx, temp);

                int i = 0;
                foreach (var it in Rows.Items)
                {
                    it.Order = i;
                    i++;
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
            var bi = mapper.Map<BoardViewModel, BoardInfo>(CurrentBoard);
            prjService.CreateOrUpdateBoardAsync(bi);
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
