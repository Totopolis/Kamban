using System;

namespace Kamban.Contracts
{
    public class Board
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
        public string Config { get; set; }
    }
}
