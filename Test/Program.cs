using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace Test
{
	static class Program
	{
		static internal bool RunProtoBufTests = false;
		static internal bool QuickRun = false;

		static void Main(string[] args)
		{
			var tester = new Tester();
			tester.Run();
		}
	}
}
