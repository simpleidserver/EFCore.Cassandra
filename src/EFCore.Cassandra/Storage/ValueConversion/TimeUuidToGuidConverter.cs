// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Cassandra;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using System;
using System.Linq.Expressions;

namespace Microsoft.EntityFrameworkCore.Cassandra.Storage
{
    public class TimeUuidToGuidConverter : ValueConverter<TimeUuid, Guid>
    {
        protected static readonly ConverterMappingHints _defaultHints
            = new ConverterMappingHints(
                size: 36,
                valueGeneratorFactory: (p, t) => new SequentialGuidValueGenerator());

        public TimeUuidToGuidConverter(ConverterMappingHints mappingHints = null) : base(ToGuid(), ToTimeUuid(), _defaultHints.With(_defaultHints)) { }

        private static Expression<Func<TimeUuid, Guid>> ToGuid() => v => v == null ? default : v.ToGuid();
        private static Expression<Func<Guid, TimeUuid>> ToTimeUuid() => v => v == null ? default : TimeUuid.Parse(v.ToString());
    }
}