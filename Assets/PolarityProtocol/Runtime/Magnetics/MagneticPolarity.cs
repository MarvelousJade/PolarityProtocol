namespace PolarityProtocol.Magnetics
{
    public enum MagneticPolarity
    {
        Negative = -1,
        Positive = 1
    }

    public static class MagneticPolarityExtensions
    {
        public static string Label(this MagneticPolarity polarity)
        {
            return polarity == MagneticPolarity.Negative ? "BLUE (-)" : "RED (+)";
        }

        public static MagneticPolarity Opposite(this MagneticPolarity polarity)
        {
            return polarity == MagneticPolarity.Negative
                ? MagneticPolarity.Positive
                : MagneticPolarity.Negative;
        }
    }
}

