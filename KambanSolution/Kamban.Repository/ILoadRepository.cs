using System;
using System.Threading.Tasks;

namespace Kamban.Repository
{
    public interface ILoadRepository : IDisposable
    {
        Task<Box> Load();
    }
}