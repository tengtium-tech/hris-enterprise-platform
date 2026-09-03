using Hris.SharedKernel;

namespace Hris.Foundation.Search.Domain;

/// <summary>
/// search-framework.md's own Domain Events section names exactly seven events -- every
/// one implemented here, one method each across <see cref="SearchIndexDefinition"/>,
/// <see cref="IndexedDocument"/>, <see cref="SearchExecution"/>, and
/// <see cref="SavedSearch"/>. <see cref="SearchIndexCreated"/>/<see cref="SearchIndexUpdated"/>
/// are raised at per-document granularity (<see cref="IndexedDocument.Index"/>/
/// <see cref="IndexedDocument.UpdateContent"/>), matching the document's own "Incremental
/// Indexing"/"Event-Driven Index Updates" bullets; <see cref="SearchIndexRebuilt"/> is a
/// batch-level summary raised once per completed rebuild cycle
/// (<see cref="SearchIndexDefinition.CompleteRebuild"/>), matching "Full Index Rebuild"
/// -- the one entity-type-scoped operation among the three, not a per-document one.
/// </summary>
public sealed record SearchRequested(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    SearchExecutionId SearchExecutionId,
    Guid TenantId,
    string QueryText) : IDomainEvent;

public sealed record SearchCompleted(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    SearchExecutionId SearchExecutionId,
    int ResultCount,
    long LatencyMs) : IDomainEvent;

public sealed record SearchFailed(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    SearchExecutionId SearchExecutionId,
    string Reason) : IDomainEvent;

public sealed record SearchIndexCreated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    IndexedDocumentId IndexedDocumentId,
    SearchIndexDefinitionId SearchIndexDefinitionId,
    Guid TenantId) : IDomainEvent;

public sealed record SearchIndexUpdated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    IndexedDocumentId IndexedDocumentId) : IDomainEvent;

public sealed record SearchIndexRebuilt(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    SearchIndexDefinitionId SearchIndexDefinitionId,
    int DocumentCount) : IDomainEvent;

public sealed record SearchSuggestionGenerated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    SavedSearchId SavedSearchId,
    Guid OwnerUserId) : IDomainEvent;
