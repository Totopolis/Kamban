using Kamban.Contracts;
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;

namespace Kamban.Export
{
    public class ExportJsonService : IExportService
    {
        public const string EXT_JSON = ".json";

        public Task DoExport(Box box, string fileName, object options)
        {
            return Task.Run(() =>
            {
                var jsonFileName = fileName + EXT_JSON;

                var output = JsonConvert.SerializeObject(box, Formatting.Indented);
                File.WriteAllText(jsonFileName, output);
            });
        }
    }
}
