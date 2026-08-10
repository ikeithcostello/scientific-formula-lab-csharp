public static class PressureB { public static double ComputePartialPressure(double moles, double volume, double temp) { return volume <= 0 ? -1 : moles * 8.314 * temp / volume; } }
