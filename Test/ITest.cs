using System;

namespace Test
{
	interface ITest
	{
		bool CanRun(ISerializerSpecimen specimen);
		void Prepare();
		void Unprepare();
		void Run(ISerializerSpecimen specimen);
	}
}
