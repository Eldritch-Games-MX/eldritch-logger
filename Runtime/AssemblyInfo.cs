using System.Runtime.CompilerServices;

// Lets the package tests check internal state (cache sizes, running worker threads).
[assembly: InternalsVisibleTo("EldritchLogger.Test")]
[assembly: InternalsVisibleTo("EldritchLogger.Tests.Editor")]
