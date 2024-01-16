namespace FEXNA_Library
{
    public class Noise
    {
        internal int Seed { get; private set; }
        public Noise(int seed)
        {
            Seed = seed;
        }

        public double noise(int x)
        {
            return IntNoise(x, Seed);
        }
        public double noise(int x, int y)
        {
            return IntNoise(x, y, Seed);
        }
        public double noise(int x, int y, int z)
        {
            return IntNoise(x, y, z, Seed);
        }

        protected static double IntNoise(params int[] parameters)
        {
            double result = IntNoise(parameters[0]);
            if (parameters.Length > 0)
            {
                for (int i = 1; i < parameters.Length; i++)
                {
                    int value = (int)((result + 1) / 2 * int.MaxValue) ^ parameters[i];
                    result = IntNoise(value);
                }
            }
            return result;
        }
        protected static double IntNoise(int x)
        {
            x = (x << 13) ^ x;
            return (1.0 - ((x * (x * x * 0x15731 + 0x789221) + 0x1376312589) & 0x7fffffff) / 1073741824.0);
        }
    }
}
