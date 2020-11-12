// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Microsoft.EntityFrameworkCore.Cassandra.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.EntityFrameworkCore.Update.Internal
{
    public class CassandraCommandBatchPreparer : CommandBatchPreparer
    {
        private readonly bool _sensitiveLoggingEnabled;
        private readonly CassandraOptionsExtension _cassandraOptionsExtension;
        private IReadOnlyDictionary<(string Schema, string Name), SharedTableEntryMapFactory<ModificationCommand>> _sharedTableEntryMapFactories;

        public CassandraCommandBatchPreparer(RelationalConnectionDependencies relationalConnectionDependencies, CommandBatchPreparerDependencies dependencies) : base(dependencies)
        {
            _cassandraOptionsExtension = CassandraOptionsExtension.Extract(relationalConnectionDependencies.ContextOptions);
        }

        protected override IEnumerable<ModificationCommand> CreateModificationCommands(IList<IUpdateEntry> entries, IUpdateAdapter updateAdapter, Func<string> generateParameterName)
        {
            var commands = new List<ModificationCommand>();
            if (_sharedTableEntryMapFactories == null)
            {
                _sharedTableEntryMapFactories = SharedTableEntryMap<ModificationCommand>
                    .CreateSharedTableEntryMapFactories(updateAdapter.Model, updateAdapter);
            }

            Dictionary<(string Schema, string Name), SharedTableEntryMap<ModificationCommand>> sharedTablesCommandsMap =
                null;
            foreach (var entry in entries)
            {
                if (entry.SharedIdentityEntry != null
                    && entry.EntityState == EntityState.Deleted)
                {
                    continue;
                }

                var entityType = entry.EntityType;
                var table = entityType.GetTableName();
                var schema = _cassandraOptionsExtension.DefaultKeyspace;
                var tableKey = (schema, table);

                ModificationCommand command;
                var isMainEntry = true;
                if (_sharedTableEntryMapFactories.TryGetValue(tableKey, out var commandIdentityMapFactory))
                {
                    if (sharedTablesCommandsMap == null)
                    {
                        sharedTablesCommandsMap =
                            new Dictionary<(string Schema, string Name), SharedTableEntryMap<ModificationCommand>>();
                    }

                    if (!sharedTablesCommandsMap.TryGetValue(tableKey, out var sharedCommandsMap))
                    {
                        sharedCommandsMap = commandIdentityMapFactory(
                            (t, s, c) => new ModificationCommand(
                                t, s, generateParameterName, _sensitiveLoggingEnabled, c));
                        sharedTablesCommandsMap.Add((schema, table), sharedCommandsMap);
                    }

                    command = sharedCommandsMap.GetOrAddValue(entry);
                    isMainEntry = sharedCommandsMap.GetPrincipals(entry.EntityType.GetRootType()).Count == 0;
                }
                else
                {
                    command = new ModificationCommand(
                        table, schema, generateParameterName, _sensitiveLoggingEnabled, comparer: null);
                }

                command.AddEntry(entry, isMainEntry);
                commands.Add(command);
            }

            if (sharedTablesCommandsMap != null)
            {
                AddUnchangedSharingEntries(sharedTablesCommandsMap, entries);
            }

            return commands.Where(
                c => c.EntityState != EntityState.Modified
                    || c.ColumnModifications.Any(m => m.IsWrite));
        }

        private void AddUnchangedSharingEntries(
            Dictionary<(string Schema, string Name), SharedTableEntryMap<ModificationCommand>> sharedTablesCommandsMap,
            IList<IUpdateEntry> entries)
        {
            foreach (var sharedCommandsMap in sharedTablesCommandsMap.Values)
            {
                foreach (var command in sharedCommandsMap.Values)
                {
                    if (command.EntityState != EntityState.Modified)
                    {
                        continue;
                    }

                    foreach (var entry in sharedCommandsMap.GetAllEntries(command.Entries[0]))
                    {
                        if (entry.EntityState != EntityState.Unchanged)
                        {
                            continue;
                        }

                        entry.EntityState = EntityState.Modified;

                        var isMainEntry = sharedCommandsMap.GetPrincipals(entry.EntityType.GetRootType()).Count == 0;
                        command.AddEntry(entry, isMainEntry);
                        entries.Add(entry);
                    }
                }
            }
        }
    }
}
