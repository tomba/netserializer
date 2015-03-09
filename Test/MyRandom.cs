using System;

namespace Test
{
	class MyRandom
	{
		Random m_random;
		ulong m_x;

		public MyRandom(ulong seed)
		{
			if (seed == 0)
				throw new Exception();

			m_random = new Random((int)seed);
			m_x = seed;
		}

		ulong NextU64()
		{
			m_x ^= m_x >> 12;
			m_x ^= m_x << 25;
			m_x ^= m_x >> 27;
			return m_x * 2685821657736338717UL;
		}

		public ulong Next()
		{
			ulong s = NextU64();

			s -= 1;
			s &= 0x7;

			switch ((int)s)
			{
				case 0:
					return 0;
				case 1:
					return NextU64() & 0xff;
				case 2:
					return NextU64() & 0xffff;
				case 3:
					return NextU64() & 0xffffff;
				case 4:
					return NextU64() & 0xffffffff;
				case 5:
					return NextU64() & 0xffffffffffUL;
				case 6:
					return NextU64() & 0xffffffffffffUL;
				case 7:
					return NextU64() & 0xffffffffffffffUL;
				default:
					throw new Exception();
			}
		}

		public int Next(int max)
		{
			return m_random.Next(max);
		}

		public int Next(int min, int max)
		{
			return m_random.Next(min, max);
		}

		public double NextDouble()
		{
			return m_random.NextDouble();
		}
	}
}
