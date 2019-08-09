using System.Threading.Tasks;
using AutoMapper;
using DynamicData;
using Kamban.Repository;
using Kamban.Repository.Models;
using Kamban.ViewModels.Core;
using Monik.Common;
using Moq;
using Xunit;

namespace Kamban.Tests
{
    public class BoxViewModelConnectTest
    {
        private readonly BoxViewModel _box;
        private readonly Mock<IMonik> _monik;
        private readonly IMapper _mapper;
        private readonly Mock<ISaveRepository> _repo;

        public BoxViewModelConnectTest()
        {
            _repo = new Mock<ISaveRepository>();
            _monik = new Mock<IMonik>();
            _mapper = new MapperConfiguration(cfg => { cfg.AddProfile<MapperProfile>(); }).CreateMapper();
            _box = new BoxViewModel(_monik.Object, _mapper);
        }

        [Fact]
        public void AddBoardAfterConnect_WillCreateBoardInRepo()
        {
            _repo.Setup(x => x.CreateOrUpdateBoard(It.IsAny<Board>())).Returns<Board>(Task.FromResult);
            _box.Connect(_repo.Object);

            var board = new BoardViewModel {Id = 1};
            _box.Boards.Add(board);

            _repo.Verify(x => x.CreateOrUpdateBoard(It.Is<Board>(b => b.Id == board.Id)), Times.Once);
        }

        [Fact]
        public void AddRowAfterConnect_WillCreateRowInRepo()
        {
            _repo.Setup(x => x.CreateOrUpdateRow(It.IsAny<Row>())).Returns<Row>(Task.FromResult);
            _box.Connect(_repo.Object);

            var row = new RowViewModel {Id = 1};
            _box.Rows.Add(row);

            _repo.Verify(x => x.CreateOrUpdateRow(It.Is<Row>(b => b.Id == row.Id)), Times.Once);
        }

        [Fact]
        public void AddColumnAfterConnect_WillCreateColumnInRepo()
        {
            _repo.Setup(x => x.CreateOrUpdateColumn(It.IsAny<Column>())).Returns<Column>(Task.FromResult);
            _box.Connect(_repo.Object);

            var column = new ColumnViewModel {Id = 1};
            _box.Columns.Add(column);

            _repo.Verify(x => x.CreateOrUpdateColumn(It.Is<Column>(b => b.Id == column.Id)), Times.Once);
        }

        [Fact]
        public void AddCardAfterConnect_WillCreateCardInRepo()
        {
            _repo.Setup(x => x.CreateOrUpdateCard(It.IsAny<Card>())).Returns<Card>(Task.FromResult);
            _box.Connect(_repo.Object);

            var card = new CardViewModel {Id = 1};
            _box.Cards.Add(card);

            _repo.Verify(x => x.CreateOrUpdateCard(It.Is<Card>(b => b.Id == card.Id)), Times.Once);
        }

        [Fact]
        public void RemoveBoardAfterConnect_WillRemoveBoardFromRepo()
        {
            var board = new BoardViewModel {Id = 1};
            _box.Boards.Add(board);
            _repo.Setup(x => x.CreateOrUpdateBoard(It.Is<Board>(b => b.Id == board.Id)))
                .Returns(Task.FromResult(new Board { Id = board.Id }));
            _box.Connect(_repo.Object);

            _box.Boards.Remove(board);

            _repo.Verify(x => x.DeleteBoard(It.Is<int>(id => id == board.Id)), Times.Once);
        }

        [Fact]
        public void RemoveRowAfterConnect_WillRemoveRowFromRepo()
        {
            var row = new RowViewModel {Id = 1};
            _box.Rows.Add(row);
            _repo.Setup(x => x.CreateOrUpdateRow(It.Is<Row>(b => b.Id == row.Id)))
                .Returns(Task.FromResult(new Row { Id = row.Id }));
            _box.Connect(_repo.Object);

            _box.Rows.Remove(row);

            _repo.Verify(x => x.DeleteRow(It.Is<int>(id => id == row.Id)), Times.Once);
        }

        [Fact]
        public void RemoveColumnAfterConnect_WillRemoveColumnFromRepo()
        {
            var column = new ColumnViewModel {Id = 1};
            _box.Columns.Add(column);
            _repo.Setup(x => x.CreateOrUpdateColumn(It.Is<Column>(b => b.Id == column.Id)))
                .Returns(Task.FromResult(new Column { Id = column.Id }));
            _box.Connect(_repo.Object);

            _box.Columns.Remove(column);

            _repo.Verify(x => x.DeleteColumn(It.Is<int>(id => id == column.Id)), Times.Once);
        }

        [Fact]
        public void RemoveCardAfterConnect_WillRemoveCardFromRepo()
        {
            var card = new CardViewModel {Id = 1};
            _box.Cards.Add(card);
            _repo.Setup(x => x.CreateOrUpdateCard(It.Is<Card>(b => b.Id == card.Id)))
                .Returns(Task.FromResult(new Card { Id = card.Id }));
            _box.Connect(_repo.Object);

            _box.Cards.Remove(card);

            _repo.Verify(x => x.DeleteCard(It.Is<int>(id => id == card.Id)), Times.Once);
        }

        [Fact]
        public void ChangeBoardAfterConnect_WillUpdateBoardInRepo()
        {
            const string name = "name";
            var board = new BoardViewModel {Id = 1};
            _box.Boards.Add(board);
            _repo.Setup(x => x.CreateOrUpdateBoard(It.Is<Board>(b => b.Id == board.Id)))
                .Returns(Task.FromResult(new Board {Id = board.Id}));
            _box.Connect(_repo.Object);

            board.Name = name;

            _repo.Verify(x => x.CreateOrUpdateBoard(
                It.Is<Board>(b => b.Id == board.Id && name.Equals(b.Name))),
                Times.Once);
        }

        [Fact]
        public void ChangeRowAfterConnect_WillChangeRowInRepo()
        {
            const string name = "name";
            var row = new RowViewModel {Id = 1};
            _box.Rows.Add(row);
            _repo.Setup(x => x.CreateOrUpdateRow(It.Is<Row>(b => b.Id == row.Id)))
                .Returns(Task.FromResult(new Row { Id = row.Id }));
            _box.Connect(_repo.Object);

            row.Name = name;

            _repo.Verify(x => x.CreateOrUpdateRow(
                It.Is<Row>(b => b.Id == row.Id && name.Equals(b.Name))),
                Times.Once);
        }

        [Fact]
        public void ChangeColumnAfterConnect_WillChangeColumnInRepo()
        {
            const string name = "name";
            var column = new ColumnViewModel {Id = 1};
            _box.Columns.Add(column);
            _repo.Setup(x => x.CreateOrUpdateColumn(It.Is<Column>(b => b.Id == column.Id)))
                .Returns(Task.FromResult(new Column { Id = column.Id }));
            _box.Connect(_repo.Object);

            column.Name = name;

            _repo.Verify(x => x.CreateOrUpdateColumn(
                It.Is<Column>(b => b.Id == column.Id && name.Equals(b.Name))),
                Times.Once);
        }

        [Fact]
        public void ChangeCardAfterConnect_WillRemoveCardInRepo()
        {
            const string header = "header";
            var card = new CardViewModel {Id = 1};
            _box.Cards.Add(card);
            _repo.Setup(x => x.CreateOrUpdateCard(It.Is<Card>(b => b.Id == card.Id)))
                .Returns(Task.FromResult(new Card { Id = card.Id }));
            _box.Connect(_repo.Object);

            card.Header = header;

            _repo.Verify(x => x.CreateOrUpdateCard(
                It.Is<Card>(b => b.Id == card.Id && header.Equals(b.Head))),
                Times.Once);
        }
    }
}