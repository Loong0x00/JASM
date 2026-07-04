// Some tests mutate the process-global XDG_DATA_HOME environment variable (to redirect the XDG
// trash during delete tests). That makes parallel execution racy, so run tests serially.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
