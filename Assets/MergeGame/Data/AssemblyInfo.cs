using System.Runtime.CompilerServices;

// Authoring assets are read-only at runtime; the seeder and the tests fill them through internal setters.
[assembly: InternalsVisibleTo("MergeGame.Editor")]
[assembly: InternalsVisibleTo("MergeGame.Data.Tests")]
