using System;

namespace DerpTest
{
	class MainClass
	{
		struct foo : IDisposable
		{
			public static implicit operator foo(int x)
			{
				return new foo ();
			}

			public static implicit operator foo(bool f)
			{
				return new foo();
			}

			public void Dispose()
			{

			}
		}

		static foo x;
		static bool y;

		public static void Problem()
		{
			bool foo = false;
			using (foo ? y = x is foo ? null : (foo?)new foo()) {}
			using (foo ? y = x is foo ?      : (foo?)new foo()) {}
		}
	}
}
