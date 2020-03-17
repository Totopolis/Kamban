using Kamban.Contracts;
using Kamban.Repository.LiteDb;
using System.Threading.Tasks;

namespace Kamban.Export
{
    public class ExportKambanService : IExportService
    {
        public const string EXT_KAM = ".kam";

        public Task DoExport(Box box, string fileName, object options)
        {
            // TODO: check
            return Export(box, fileName);
        }

        private async Task Export(Box box, string fileName)
        {
            var kamFileName = fileName + EXT_KAM;

            using (var repo = new LiteDbRepository(kamFileName))
            {
                foreach (var brd in box.Boards)
                {
                    await repo.CreateOrUpdateBoard(brd);

                    foreach (var col in box.Columns)
                        await repo.CreateOrUpdateColumn(col);

                    foreach (var row in box.Rows)
                        await repo.CreateOrUpdateRow(row);

                    foreach (var iss in box.Cards)
                        await repo.CreateOrUpdateCard(iss);
                }
            }
        }
    }//end of class
}
