using System;
using System.Linq.Expressions;
using System.Web.Helpers;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using MatinPower.Infrastructure.Filter;
using System.Collections.Generic;
using MatinPower.Server.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace TicketManagement.Infrastructure
{
    public class TransactionalRepository : IDisposable
    {
        readonly MatinPowerDbContext _context = new ();
        private IDbContextTransaction? _transaction;

        public (long Id, T Item) SaveItem<T>(T item, EntityState state, bool makeLog = true) where T : class
        {
            if (state is EntityState.Modified or EntityState.Added)
                Repository<T>.GeneralValidation(item);
            _context.Entry(item).State = state;
            if (makeLog)
                Repository<T>.LogModification(item, state, _context, true);
            var convertedValue = _context.Entry(item).Properties.First().CurrentValue is long value ? value : throw new InvalidOperationException("Conversion to long failed.");
            return (convertedValue, item);
        }
        public (T Item, long Id) InsertItem<T>(T item) where T : class
        {
            (long id, T savedItem) = SaveItem(item, EntityState.Added);
            return (savedItem, id);
        }

        public (T Item, long Id) UpdateItem<T>(T item) where T : class
        {
            (long id, T savedItem) = SaveItem(item, EntityState.Modified);
            return (savedItem, id);
        }
        public (T Item, long Id) DeleteItem<T>(T item) where T : class
        {
            (long id, T savedItem) = SaveItem(item, EntityState.Deleted);
            return (savedItem, id);
        }
        public void Commit()
        {
            _context.SaveChanges();
            _transaction.Commit();
        }

        public void BeginTransaction()
        {
            _transaction = _context.Database.BeginTransaction();
        }

        public void Rollback()
        {
            _transaction.Rollback();
        }

        public void Dispose()
        {
            _transaction.Dispose();
        }

        public ValueTask DisposeAsync()
        {
            return _transaction.DisposeAsync();
        }
    }
}
