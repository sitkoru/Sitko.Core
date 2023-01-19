using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Internal;

namespace Sitko.Core.Db.Postgres;
#pragma warning disable EF1001
public class FixedRelationalModelValidator : NpgsqlModelValidator
{
    public FixedRelationalModelValidator(ModelValidatorDependencies dependencies,
        RelationalModelValidatorDependencies relationalDependencies, INpgsqlSingletonOptions npgsqlSingletonOptions) :
        base(dependencies, relationalDependencies, npgsqlSingletonOptions)
    {
    }

    protected override void ValidateCompatible(IProperty property, IProperty duplicateProperty, string columnName,
        in StoreObjectIdentifier storeObject, IDiagnosticsLogger<DbLoggerCategory.Model.Validation> logger)
    {
        if (property.IsColumnNullable(storeObject) != duplicateProperty.IsColumnNullable(storeObject))
        {
            throw new InvalidOperationException(
                RelationalStrings.DuplicateColumnNameNullabilityMismatch(
                    duplicateProperty.DeclaringEntityType.DisplayName(),
                    duplicateProperty.Name,
                    property.DeclaringEntityType.DisplayName(),
                    property.Name,
                    columnName,
                    storeObject.DisplayName()));
        }

        var currentMaxLength = property.GetMaxLength(storeObject);
        var previousMaxLength = duplicateProperty.GetMaxLength(storeObject);
        if (currentMaxLength != previousMaxLength)
        {
            throw new InvalidOperationException(
                RelationalStrings.DuplicateColumnNameMaxLengthMismatch(
                    duplicateProperty.DeclaringEntityType.DisplayName(),
                    duplicateProperty.Name,
                    property.DeclaringEntityType.DisplayName(),
                    property.Name,
                    columnName,
                    storeObject.DisplayName(),
                    previousMaxLength,
                    currentMaxLength));
        }

        if (property.IsUnicode(storeObject) != duplicateProperty.IsUnicode(storeObject))
        {
            throw new InvalidOperationException(
                RelationalStrings.DuplicateColumnNameUnicodenessMismatch(
                    duplicateProperty.DeclaringEntityType.DisplayName(),
                    duplicateProperty.Name,
                    property.DeclaringEntityType.DisplayName(),
                    property.Name,
                    columnName,
                    storeObject.DisplayName()));
        }

        if (property.IsFixedLength(storeObject) != duplicateProperty.IsFixedLength(storeObject))
        {
            throw new InvalidOperationException(
                RelationalStrings.DuplicateColumnNameFixedLengthMismatch(
                    duplicateProperty.DeclaringEntityType.DisplayName(),
                    duplicateProperty.Name,
                    property.DeclaringEntityType.DisplayName(),
                    property.Name,
                    columnName,
                    storeObject.DisplayName()));
        }

        var currentPrecision = property.GetPrecision(storeObject);
        var previousPrecision = duplicateProperty.GetPrecision(storeObject);
        if (currentPrecision != previousPrecision)
        {
            throw new InvalidOperationException(
                RelationalStrings.DuplicateColumnNamePrecisionMismatch(
                    duplicateProperty.DeclaringEntityType.DisplayName(),
                    duplicateProperty.Name,
                    property.DeclaringEntityType.DisplayName(),
                    property.Name,
                    columnName,
                    storeObject.DisplayName(),
                    currentPrecision,
                    previousPrecision));
        }

        var currentScale = property.GetScale(storeObject);
        var previousScale = duplicateProperty.GetScale(storeObject);
        if (currentScale != previousScale)
        {
            throw new InvalidOperationException(
                RelationalStrings.DuplicateColumnNameScaleMismatch(
                    duplicateProperty.DeclaringEntityType.DisplayName(),
                    duplicateProperty.Name,
                    property.DeclaringEntityType.DisplayName(),
                    property.Name,
                    columnName,
                    storeObject.DisplayName(),
                    currentScale,
                    previousScale));
        }

        if (property.IsConcurrencyToken != duplicateProperty.IsConcurrencyToken)
        {
            throw new InvalidOperationException(
                RelationalStrings.DuplicateColumnNameConcurrencyTokenMismatch(
                    duplicateProperty.DeclaringEntityType.DisplayName(),
                    duplicateProperty.Name,
                    property.DeclaringEntityType.DisplayName(),
                    property.Name,
                    columnName,
                    storeObject.DisplayName()));
        }

        var typeMapping = property.GetRelationalTypeMapping();
        var duplicateTypeMapping = duplicateProperty.GetRelationalTypeMapping();
        var currentTypeString = property.GetColumnType(storeObject);
        var previousTypeString = duplicateProperty.GetColumnType(storeObject);
        if (!string.Equals(currentTypeString, previousTypeString, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                RelationalStrings.DuplicateColumnNameDataTypeMismatch(
                    duplicateProperty.DeclaringEntityType.DisplayName(),
                    duplicateProperty.Name,
                    property.DeclaringEntityType.DisplayName(),
                    property.Name,
                    columnName,
                    storeObject.DisplayName(),
                    previousTypeString,
                    currentTypeString));
        }

        var currentProviderType = UnwrapNullableType(typeMapping.Converter?.ProviderClrType)
                                  ?? typeMapping.ClrType;
        var previousProviderType = UnwrapNullableType(duplicateTypeMapping.Converter?.ProviderClrType)
                                   ?? duplicateTypeMapping.ClrType;

        if (currentProviderType != previousProviderType
            && (property.IsKey()
                || duplicateProperty.IsKey()
                || property.IsForeignKey()
                || duplicateProperty.IsForeignKey()
                || (property.IsIndex() && property.GetContainingIndexes().Any(i => i.IsUnique))
                || (duplicateProperty.IsIndex() && duplicateProperty.GetContainingIndexes().Any(i => i.IsUnique))))
        {
            throw new InvalidOperationException(
                RelationalStrings.DuplicateColumnNameProviderTypeMismatch(
                    duplicateProperty.DeclaringEntityType.DisplayName(),
                    duplicateProperty.Name,
                    property.DeclaringEntityType.DisplayName(),
                    property.Name,
                    columnName,
                    storeObject.DisplayName(),
                    previousProviderType.ShortDisplayName(),
                    currentProviderType.ShortDisplayName()));
        }

        var currentComputedColumnSql = property.GetComputedColumnSql(storeObject) ?? "";
        var previousComputedColumnSql = duplicateProperty.GetComputedColumnSql(storeObject) ?? "";
        if (!currentComputedColumnSql.Equals(previousComputedColumnSql, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                RelationalStrings.DuplicateColumnNameComputedSqlMismatch(
                    duplicateProperty.DeclaringEntityType.DisplayName(),
                    duplicateProperty.Name,
                    property.DeclaringEntityType.DisplayName(),
                    property.Name,
                    columnName,
                    storeObject.DisplayName(),
                    previousComputedColumnSql,
                    currentComputedColumnSql));
        }

        var currentStored = property.GetIsStored(storeObject);
        var previousStored = duplicateProperty.GetIsStored(storeObject);
        if (currentStored != previousStored)
        {
            throw new InvalidOperationException(
                RelationalStrings.DuplicateColumnNameIsStoredMismatch(
                    duplicateProperty.DeclaringEntityType.DisplayName(),
                    duplicateProperty.Name,
                    property.DeclaringEntityType.DisplayName(),
                    property.Name,
                    columnName,
                    storeObject.DisplayName(),
                    previousStored,
                    currentStored));
        }

        var hasDefaultValue = property.TryGetDefaultValue(storeObject, out var currentDefaultValue);
        var duplicateHasDefaultValue = duplicateProperty.TryGetDefaultValue(storeObject, out var previousDefaultValue);
        if ((hasDefaultValue
             || duplicateHasDefaultValue)
            && !Equals(currentDefaultValue, previousDefaultValue))
        {
            currentDefaultValue = GetDefaultColumnValue(property, storeObject);
            previousDefaultValue = GetDefaultColumnValue(duplicateProperty, storeObject);

            if (!Equals(currentDefaultValue, previousDefaultValue))
            {
                throw new InvalidOperationException(
                    RelationalStrings.DuplicateColumnNameDefaultSqlMismatch(
                        duplicateProperty.DeclaringEntityType.DisplayName(),
                        duplicateProperty.Name,
                        property.DeclaringEntityType.DisplayName(),
                        property.Name,
                        columnName,
                        storeObject.DisplayName(),
                        previousDefaultValue ?? "NULL",
                        currentDefaultValue ?? "NULL"));
            }
        }

        var currentDefaultValueSql = property.GetDefaultValueSql(storeObject) ?? "";
        var previousDefaultValueSql = duplicateProperty.GetDefaultValueSql(storeObject) ?? "";
        if (!currentDefaultValueSql.Equals(previousDefaultValueSql, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                RelationalStrings.DuplicateColumnNameDefaultSqlMismatch(
                    duplicateProperty.DeclaringEntityType.DisplayName(),
                    duplicateProperty.Name,
                    property.DeclaringEntityType.DisplayName(),
                    property.Name,
                    columnName,
                    storeObject.DisplayName(),
                    previousDefaultValueSql,
                    currentDefaultValueSql));
        }

        var currentComment = property.GetComment(storeObject) ?? "";
        var previousComment = duplicateProperty.GetComment(storeObject) ?? "";
        if (!currentComment.Equals(previousComment, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                RelationalStrings.DuplicateColumnNameCommentMismatch(
                    duplicateProperty.DeclaringEntityType.DisplayName(),
                    duplicateProperty.Name,
                    property.DeclaringEntityType.DisplayName(),
                    property.Name,
                    columnName,
                    storeObject.DisplayName(),
                    previousComment,
                    currentComment));
        }

        var currentCollation = property.GetCollation(storeObject) ?? "";
        var previousCollation = duplicateProperty.GetCollation(storeObject) ?? "";
        if (!currentCollation.Equals(previousCollation, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                RelationalStrings.DuplicateColumnNameCollationMismatch(
                    duplicateProperty.DeclaringEntityType.DisplayName(),
                    duplicateProperty.Name,
                    property.DeclaringEntityType.DisplayName(),
                    property.Name,
                    columnName,
                    storeObject.DisplayName(),
                    previousCollation,
                    currentCollation));
        }

        var currentColumnOrder = property.GetColumnOrder(storeObject);
        var previousColumnOrder = duplicateProperty.GetColumnOrder(storeObject);
        if (currentColumnOrder != previousColumnOrder)
        {
            throw new InvalidOperationException(
                RelationalStrings.DuplicateColumnNameOrderMismatch(
                    duplicateProperty.DeclaringEntityType.DisplayName(),
                    duplicateProperty.Name,
                    property.DeclaringEntityType.DisplayName(),
                    property.Name,
                    columnName,
                    storeObject.DisplayName(),
                    previousColumnOrder,
                    currentColumnOrder));
        }

        if (property.GetCompressionMethod(storeObject) != duplicateProperty.GetCompressionMethod(storeObject))
        {
            throw new InvalidOperationException(
                NpgsqlStrings.DuplicateColumnCompressionMethodMismatch(
                    duplicateProperty.DeclaringEntityType.DisplayName(),
                    duplicateProperty.Name,
                    property.DeclaringEntityType.DisplayName(),
                    property.Name,
                    columnName,
                    storeObject.DisplayName()));
        }
    }


    private static Type? UnwrapNullableType(Type? type)
        => type is not null ? Nullable.GetUnderlyingType(type) ?? type : null;
}
#pragma warning restore EF1001
