using System;

namespace Test
{
	interface ITestSpec
	{
		ITest Create();
		Type Type { get; }
	}
}
