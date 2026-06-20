using Xunit;

// Terminal.Gui application/session handling keeps per-instance state; run tests serially so the
// headless ANSI driver renders deterministically for golden comparisons.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
