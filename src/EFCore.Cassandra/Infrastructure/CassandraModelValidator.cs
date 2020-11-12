using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Microsoft.EntityFrameworkCore.Infrastructure
{
    public class CassandraModelValidator : ModelValidator
    {
        public CassandraModelValidator(ModelValidatorDependencies dependencies) : base(dependencies)
        {
        }

        protected override void ValidatePropertyMapping(IModel model, IDiagnosticsLogger<DbLoggerCategory.Model.Validation> logger)
        {
            if (!(model is IConventionModel conventionModel))
            {
                return;
            }


            foreach (var entityType in conventionModel.GetEntityTypes())
            {
                var unmappedProperty = entityType.GetProperties().FirstOrDefault(
                    p => (!ConfigurationSource.Convention.Overrides(p.GetConfigurationSource())
                            || !p.IsShadowProperty())
                        && p.FindTypeMapping() == null);

                if (unmappedProperty != null)
                {
                    throw new InvalidOperationException(
                        CoreStrings.PropertyNotMapped(
                            entityType.DisplayName(), unmappedProperty.Name, unmappedProperty.ClrType.ShortDisplayName()));
                }

                if (!entityType.HasClrType())
                {
                    continue;
                }

                var clrProperties = new HashSet<string>(StringComparer.Ordinal);

                var runtimeProperties = entityType.AsEntityType().GetRuntimeProperties();

                clrProperties.UnionWith(
                    runtimeProperties.Values
                        .Where(pi => pi.IsCandidateProperty())
                        .Select(pi => pi.GetSimpleMemberName()));

                clrProperties.ExceptWith(entityType.GetProperties().Select(p => p.Name));
                clrProperties.ExceptWith(entityType.GetNavigations().Select(p => p.Name));
                clrProperties.ExceptWith(entityType.GetServiceProperties().Select(p => p.Name));
                clrProperties.RemoveWhere(p => entityType.FindIgnoredConfigurationSource(p) != null);

                if (clrProperties.Count <= 0)
                {
                    continue;
                }

                foreach (var clrProperty in clrProperties)
                {
                    var actualProperty = runtimeProperties[clrProperty];
                    var propertyType = actualProperty.PropertyType;
                    var targetSequenceType = propertyType.TryGetSequenceType();

                    if (conventionModel.FindIgnoredConfigurationSource(propertyType.DisplayName()) != null
                        || targetSequenceType != null
                        && conventionModel.FindIgnoredConfigurationSource(targetSequenceType.DisplayName()) != null)
                    {
                        continue;
                    }

                    var targetType = FindCandidateNavigationPropertyType(actualProperty);
                    if (targetType == null)
                    {
                        continue;
                    }

                    var isTargetWeakOrOwned
                        = targetType != null
                        && (conventionModel.HasEntityTypeWithDefiningNavigation(targetType)
                            || conventionModel.IsOwned(targetType));

                    var elt= conventionModel.FindEntityType(targetType);
                    if (elt != null && elt.IsUserDefinedType())
                    {
                        continue;
                    }

                    if (targetType?.IsValidEntityType() == true
                        && (isTargetWeakOrOwned
                            || conventionModel.FindEntityType(targetType) != null
                            || targetType.GetRuntimeProperties().Any(p => p.IsCandidateProperty())))
                    {
                        // ReSharper disable CheckForReferenceEqualityInstead.1
                        // ReSharper disable CheckForReferenceEqualityInstead.3
                        if ((!entityType.IsKeyless
                                || targetSequenceType == null)
                            && entityType.GetDerivedTypes().All(
                                dt => dt.GetDeclaredNavigations().FirstOrDefault(n => n.Name == actualProperty.GetSimpleMemberName())
                                    == null)
                            && (!isTargetWeakOrOwned
                                || (!targetType.Equals(entityType.ClrType)
                                    && (!entityType.IsInOwnershipPath(targetType)
                                        || (entityType.FindOwnership().PrincipalEntityType.ClrType.Equals(targetType)
                                            && targetSequenceType == null))
                                    && (!entityType.IsInDefinitionPath(targetType)
                                        || (entityType.DefiningEntityType.ClrType.Equals(targetType)
                                            && targetSequenceType == null)))))
                        {
                            if (conventionModel.IsOwned(entityType.ClrType)
                                && conventionModel.IsOwned(targetType))
                            {
                                throw new InvalidOperationException(
                                    CoreStrings.AmbiguousOwnedNavigation(
                                        entityType.ClrType.ShortDisplayName(), targetType.ShortDisplayName()));
                            }

                            throw new InvalidOperationException(
                                CoreStrings.NavigationNotAdded(
                                    entityType.DisplayName(), actualProperty.Name, propertyType.ShortDisplayName()));
                        }
                    }
                    else if (targetSequenceType == null && propertyType.GetTypeInfo().IsInterface
                        || targetSequenceType?.GetTypeInfo().IsInterface == true)
                    {
                        throw new InvalidOperationException(
                            CoreStrings.InterfacePropertyNotAdded(
                                entityType.DisplayName(), actualProperty.Name, propertyType.ShortDisplayName()));
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            CoreStrings.PropertyNotAdded(
                                entityType.DisplayName(), actualProperty.Name, propertyType.ShortDisplayName()));
                    }
                }
            }
        }
        private Type FindCandidateNavigationPropertyType(PropertyInfo propertyInfo) => Dependencies.MemberClassifier.FindCandidateNavigationPropertyType(propertyInfo);
    }
}
