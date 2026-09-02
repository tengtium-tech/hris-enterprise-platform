using System.Runtime.CompilerServices;

// Grants Hris.Foundation.Authorization.Tests direct access to this assembly's own
// internal command/query handlers, per coding-standards.md's Application Layer
// convention (handlers stay internal, reachable only through MediatR's own
// DI-resolved dispatch -- Directory.Build.props' own CA1812 suppression states the
// same reasoning). InternalsVisibleTo is the standard mechanism for unit-testing an
// internal implementation directly, from a dedicated test assembly, without loosening
// this assembly's own public API surface for every other consumer.
[assembly: InternalsVisibleTo("Hris.Foundation.Authorization.Tests")]
