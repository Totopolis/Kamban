using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kamban.Repository.LiteDb
{
    public static class LiteDbExtensions
    {
        private static readonly Dictionary<Type, string> CollectionNameByType =
            new Dictionary<Type, string>
            {
                {typeof(Row), "rows"},
                {typeof(Column), "columns"},
                {typeof(Board), "boards"},
                {typeof(Card), "issues"}
            };

        public static LiteCollection<T> GetCollectionByType<T>(this LiteDatabase db)
        {
            return db.GetCollection<T>(CollectionNameByType[typeof(T)]);
        }

        public static Task<T> UpsertAsync<T>(this LiteDatabase database, T document)
        {
            return Task.Run(() =>
            {
                database
                    .GetCollectionByType<T>()
                    .Upsert(document);
                return document;
            });
        }

        public static Task<List<T>> GetAllAsync<T>(this LiteDatabase database)
        {
            return Task.Run(() =>
                database
                    .GetCollectionByType<T>()
                    .FindAll()
                    .ToList()
            );
        }
    }
}