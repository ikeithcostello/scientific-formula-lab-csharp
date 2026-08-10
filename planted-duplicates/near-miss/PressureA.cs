public static class PressureA { public static double ComputePartialPressure(double moles, double volume, double temp) { return volume <= 0 ? 0 : moles * 8.314 * temp / volume; } }
