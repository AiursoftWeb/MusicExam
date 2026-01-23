using Microsoft.VisualStudio.TestTools.UnitTesting;

// Configure MSTest to not run tests in parallel
// This is required to avoid MSTEST0001 warning
[assembly: Parallelize(Scope = ExecutionScope.MethodLevel, Workers = 0)]
