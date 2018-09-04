using System.ComponentModel;
using System.Linq;
using GongSolutions.Wpf.DragDrop;
using Ui.Wpf.KanbanControl.Elements.CardElement;

namespace Kamban.ViewModels
{
    public class LocalBoardDropHandler : IDropTarget
    {
        public void DragOver(IDropInfo dropInfo)
        {
            DragDrop.DefaultDropHandler.DragOver(dropInfo);
            if (dropInfo.TargetGroup == null)
            {
                dropInfo.Effects = System.Windows.DragDropEffects.None;
            }
        }

        public void Drop(IDropInfo dropInfo)
        {
            DragDrop.DefaultDropHandler.Drop(dropInfo);

            // Now extract the dragged group items and set the new group (target)
            var data = DefaultDropHandler.ExtractData(dropInfo.Data).OfType<Card>().ToList();
            foreach (var groupedItem in data)
            {
                groupedItem.View.Content = dropInfo.Data;
            }

            // Changing group data at runtime isn't handled well: force a refresh on the collection view.
            if (dropInfo.TargetCollection is ICollectionView view)
            {
                view.Refresh();
            }
        }
    }
}
