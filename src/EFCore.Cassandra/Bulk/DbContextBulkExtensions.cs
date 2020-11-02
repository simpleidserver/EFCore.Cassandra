// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using EFCore.Cassandra.Bulk;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.EntityFrameworkCore
{
    public static class DbContextBulkExtensions
    {
        public static void BulkInsert<T>(this DbContext dbContext, List<T> entities)
        {
            SqlBulkOperation.Insert(dbContext, entities);
        }

        public static Task BulkInsertAsync<T>(this DbContext dbContext, List<T> entities)
        {
            return SqlBulkOperation.InsertAsync(dbContext, entities);
        }
    }
}
