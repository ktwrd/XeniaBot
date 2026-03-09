#pragma warning disable EF1001 // Internal EF Core API usage.
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update.Internal;

namespace XeniaDiscord.Data.Extensions;

public static class IsConstrainedExtensions
{
    private const string IsConstrainedAnnotation = "Custom:IsConstrained";

    /// <summary>
    /// Sets whether the reference relationship should be constrained with a foreign key constraint in the database.
    /// </summary>
    public static ReferenceReferenceBuilder IsConstrained(
        this ReferenceReferenceBuilder builder, bool constrained)
    {
        builder.Metadata.SetAnnotation(IsConstrainedAnnotation, constrained);
        if (!constrained)
        {
            SetNotRequiredPreservingNullability(builder.Metadata);
        }

        return builder;
    }

    /// <summary>
    /// Sets whether the collection relationship should be constrained with a foreign key constraint in the database.
    /// </summary>
    public static ReferenceCollectionBuilder IsConstrained(
        this ReferenceCollectionBuilder builder, bool constrained)
    {
        builder.Metadata.SetAnnotation(IsConstrainedAnnotation, constrained);
        if (!constrained)
        {
            SetNotRequiredPreservingNullability(builder.Metadata);
        }

        return builder;
    }

    private static void SetNotRequiredPreservingNullability(IMutableForeignKey foreignKey)
    {
        // Capture the original nullability of FK properties before changing IsRequired
        var originalNullability = foreignKey.Properties
            .Select(p => p.IsNullable)
            .ToList();

        // We need EF Core to generate LEFT JOINs instead of INNER JOINs on Includes, so we mark it as non-required
        foreignKey.IsRequired = false;

        // Restore the original nullability of FK properties
        // This ensures that non-nullable FK columns remain NOT NULL in the database schema
        for (var i = 0; i < foreignKey.Properties.Count; i++)
        {
            foreignKey.Properties[i].IsNullable = originalNullability[i];
        }
    }

    /// <summary>
    /// Adds support for IsConstrained annotations by replacing the default migrations model differ
    /// with a custom implementation that respects the IsConstrained annotation.
    /// </summary>
    public static DbContextOptionsBuilder AddIsConstrainedAnnotations(this DbContextOptionsBuilder builder)
    {
        return builder.ReplaceService<IMigrationsModelDiffer, NoForeignKeyMigrationsModelDiffer>();
    }

    internal static bool IsConstrained(this IForeignKey fk)
        => (fk.FindAnnotation(IsConstrainedAnnotation)?.Value as bool?) ?? true;
}

/// <summary>
/// Custom IMigrationsModelDiffer implementation that removes all FKs with IsConstrained annotation.
/// </summary>
internal class NoForeignKeyMigrationsModelDiffer : MigrationsModelDiffer
{
    public NoForeignKeyMigrationsModelDiffer(
        IRelationalTypeMappingSource typeMappingSource,
        IMigrationsAnnotationProvider migrationsAnnotationProvider,
        IRelationalAnnotationProvider relationalAnnotationProvider,
        IRowIdentityMapFactory rowIdentityMapFactory,
        CommandBatchPreparerDependencies commandBatchPreparerDependencies)
        : base(typeMappingSource, migrationsAnnotationProvider, relationalAnnotationProvider, rowIdentityMapFactory, commandBatchPreparerDependencies)
    {
    }

    protected override IEnumerable<MigrationOperation> Diff(IRelationalModel? source, IRelationalModel? target, DiffContext diffContext)
    {
        ApplyIsConstrained(source);
        ApplyIsConstrained(target);
        return base.Diff(source, target, diffContext);
    }

    private static void ApplyIsConstrained(IRelationalModel? model)
    {
        if (model == null)
        {
            return;
        }

        foreach (var table in model.Tables)
        {
            ((Table)table).ForeignKeyConstraints.RemoveWhere(fkConstraint => fkConstraint.MappedForeignKeys.Any(fk => !fk.IsConstrained()));
        }
    }
}
