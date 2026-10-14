// SPDX-License-Identifier: GPL-3.0-or-later
namespace CobraLed;
public sealed record LightMode(string Name, byte First, byte Second, bool ValidatedAfterReconnect = true);
public static class Lighting
{
    public static byte[][] BuildReports(byte[] lightingReport, bool apply)
    {
        if (lightingReport.Length != 16 || !lightingReport.AsSpan(0, 8).SequenceEqual(
            Convert.FromHexString("02F3490407000000")) || lightingReport[14] != 2 || lightingReport[15] != 0)
            throw new ArgumentException("Relatório inválido de iluminação do perfil 1.");
        // Captured after all five lighting blocks. Its isolated behavior is experimental.
        // Do not include the subsequent profile-selection write or other profiles.
        return apply ? new[] { lightingReport.ToArray(),
            Convert.FromHexString("02F10204000000000000000000000000") }
            : new[] { lightingReport.ToArray() };
    }

    // mouse_m908/include/data.cpp: _c_lightmode_values.
    public static IReadOnlyList<LightMode> Modes { get; } = Array.AsReadOnly(new[] {
        new LightMode("off", 0, 0), new LightMode("breathing_rainbow", 1, 1),
        new LightMode("static", 1, 2), new LightMode("breathing", 1, 4),
        new LightMode("rainbow", 1, 8), new LightMode("flashing", 1, 16),
        new LightMode("wave", 2, 0), new LightMode("reactive_button", 3, 0),
        new LightMode("random", 4, 0), new LightMode("alternating", 6, 0),
        new LightMode("reactive", 7, 0),
        // OpenRGB RedragonMouseController: Wave=0, with first mode byte fixed at 1.
        // This differs from upstream mouse_m908 Wave=02/00; behavior on V2 unverified.
        new LightMode("wave_legacy", 1, 0, false) });
    public static LightMode FindMode(string name) => Modes.SingleOrDefault(
        m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
        ?? throw new ArgumentException($"Modo desconhecido: {name}. Use modes.");
    public static byte[] Build(byte r, byte g, byte b, string mode = "static", int speed = 0)
    {
        var selected = FindMode(mode);
        // Zero preserves the physically validated static-color report.
        if (speed is < 0 or > 8 || (speed == 0 && selected.Name != "static"))
            throw new ArgumentException("Velocidade: 1 a 8 (0 somente para static).");
        var report = Convert.FromHexString("02F34904070000000000000100020200");
        report[8] = r; report[9] = g; report[10] = b;
        report[11] = selected.First; report[12] = (byte)speed; report[13] = selected.Second;
        return report;
    }
}
