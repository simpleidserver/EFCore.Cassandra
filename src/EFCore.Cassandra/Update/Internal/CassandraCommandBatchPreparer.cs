// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Microsoft.EntityFrameworkCore.Cassandra.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
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

        public CassandraCommandBatchPreparer(
            RelationalConnectionDependencies relationalConnectionDependencies, 
            CommandBatchPreparerDependencies dependencies) : base(dependencies)
        {
            _cassandraOptionsExtension = CassandraOptionsExtension.Extract(relationalConnectionDependencies.ContextOptions);
        }

        protected override IEnumerable<ModificationCommand> CreateModificationCommands(
            IList<IUpdateEntry> entries, 
            IUpdateAdapter updateAdapter, 
            Func<string> generateParameterName)
        {
            var commands = new List<ModificationCommand>();
            Dictionary<(string Name, string Schema), SharedTableEntryMap<ModificationCommand>> sharedTablesCommandsMap =
                null;
            var schema = _cassandraOptionsExtension.DefaultKeyspace;
            foreach (var entry in entries)
            {
                if (entry.SharedIdentityEntry != null
                    && entry.EntityState == EntityState.Deleted)
                {
                    continue;
                }

                var mappings = (IReadOnlyCollection<ITableMapping>)entry.EntityType.GetTableMappings();
                var mappingCount = mappings.Count;
                ModificationCommand firstCommand = null;
                foreach (var mapping in mappings)
                {
                    var table = mapping.Table;
                    var tableKey = (table.Name, schema);

                    ModificationCommand command;
                    var isMainEntry = true;
                    if (table.IsShared)
                    {
                        if (sharedTablesCommandsMap == null)
                        {
                            sharedTablesCommandsMap = new Dictionary<(string, string), SharedTableEntryMap<ModificationCommand>>();
                        }

                        if (!sharedTablesCommandsMap.TryGetValue(tableKey, out var sharedCommandsMap))
                        {
                            sharedCommandsMap = new SharedTableEntryMap<ModificationCommand>(table, updateAdapter);
                            sharedTablesCommandsMap.Add(tableKey, sharedCommandsMap);
                        }

                        command = sharedCommandsMap.GetOrAddValue(
                            entry,
                            (n, s, c) => new ModificationCommand(n, schema, generateParameterName, _sensitiveLoggingEnabled, c));
                        isMainEntry = sharedCommandsMap.IsMainEntry(entry);
                    }
                    else
                    {
                        command = new ModificationCommand(
                            table.Name, schema, generateParameterName, _sensitiveLoggingEnabled, comparer: null);
                    }
                    
                    command.AddEntry(entry, isMainEntry);
                    commands.Add(command);

                    if (firstCommand == null)
                    {
                        firstCommand = command;
                    }
                }

                if (firstCommand == null)
                {
                    throw new InvalidOperationException("Readonly entity saved : " + entry.EntityType.DisplayName());
                }
            }

            if (sharedTablesCommandsMap != null)
            {
                AddUnchangedSharingEntries(sharedTablesCommandsMap.Values, entries);
            }

            return commands;
        }

        private void AddUnchangedSharingEntries(
            IEnumerable<SharedTableEntryMap<ModificationCommand>> sharedTablesCommands,
            IList<IUpdateEntry> entries)
        {
            foreach (var sharedCommandsMap in sharedTablesCommands)
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

                        command.AddEntry(entry, sharedCommandsMap.IsMainEntry(entry));
                        entries.Add(entry);
                    }
                }
            }
        }

        /*
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
        */
    }
}
