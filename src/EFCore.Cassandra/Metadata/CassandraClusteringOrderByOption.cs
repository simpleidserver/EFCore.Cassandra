// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
namespace Microsoft.EntityFrameworkCore
{
    public enum CassandraClusteringOrderByOptions
    {
        ASC,
        DESC
    }

    public class CassandraClusteringOrderByOption
    {
        public CassandraClusteringOrderByOption(string columnName, CassandraClusteringOrderByOptions order)
        {
            ColumnName = columnName;
            Order = order;
        }

        public string ColumnName { get; }
        public CassandraClusteringOrderByOptions Order { get; }
    }
}
