// SPDX-License-Identifier: GPL-3.0-or-later
using CobraLed;
int checks = 0;
void Check(bool ok, string label) { if (!ok) throw new Exception(label); checks++; }
void Reject(Action action, string label) {
    try { action(); } catch (ArgumentException) { checks++; return; }
    throw new Exception(label);
}
var fake = new FakeTransport();
int Run(params string[] args) => Cli.Run(args, fake, TextWriter.Null);
Check(Convert.ToHexString(Lighting.Build(0, 255, 0)) == "02F349040700000000FF000100020200", "validated static packet");
var golden = new (string Name, byte A, byte B)[] {
    ("off",0,0), ("breathing_rainbow",1,1), ("static",1,2), ("breathing",1,4),
    ("rainbow",1,8), ("flashing",1,16), ("wave",2,0), ("reactive_button",3,0),
    ("random",4,0), ("alternating",6,0), ("reactive",7,0), ("wave_legacy",1,0) };
Check(Lighting.Modes.Count == golden.Length, "mode count");
foreach (var (name, a, b) in golden) {
    foreach (var speed in new[] {1,8}) {
        var packet = Lighting.Build(17, 128, 255, name, speed);
        var expected = new byte[] {2,243,73,4,7,0,0,0,17,128,255,a,(byte)speed,b,2,0};
        Check(packet.SequenceEqual(expected), $"all bytes {name}/{speed}");
        Run("preview-effect", name, "17", "128", "255", speed.ToString());
    }
}
Run("preview", "0", "255", "0"); Run("modes"); Run("help");
Check(fake.Lists == 0 && fake.Sends == 0, "offline commands never touch transport");
Check(!Lighting.FindMode("wave_legacy").ValidatedAfterReconnect &&
    Lighting.FindMode("wave").ValidatedAfterReconnect, "legacy candidate not marked physically validated");
var modeOutput = new StringWriter();
Cli.Run(new[] {"modes"}, fake, modeOutput);
Check(modeOutput.ToString().Split(Environment.NewLine).Single(line =>
    line.StartsWith("wave_legacy")).Contains("experimental"), "modes labels legacy candidate experimental");
foreach (var bad in new[] {"-1", "256", "1.5", "+1", " 1", "999999999999", "NaN", ""}) {
    for (int i = 1; i <= 3; i++) {
        var invalidArgs = new[] {"set", "0", "0", "0"}; invalidArgs[i] = bad;
        Reject(() => Run(invalidArgs), "invalid RGB");
    }
    Reject(() => Run("effect", "wave", "0", "0", "0", bad), "invalid speed");
}
foreach (var speed in new[] {"0", "9"})
    Reject(() => Run("effect", "wave", "0", "0", "0", speed), "speed bounds");
Reject(() => Run("effect", "missing", "0", "0", "0", "1"), "unknown mode");
foreach (var command in new[] {"list", "modes", "preview", "preview-effect", "set", "effect", "help"})
    Reject(() => Run(command, "extra"), "argument count");
Reject(() => Run("unknown"), "unknown command");
Reject(() => Lighting.Build(0,0,0,"wave",0), "builder zero speed");
Reject(() => Lighting.Build(0,0,0,"static",9), "builder speed maximum");
Check(fake.Sends == 0 && fake.Lists == 0, "invalid input never touches transport");
Run("set", "255", "0", "128");
Check(fake.Sends == 1 && Convert.ToHexString(fake.Last!) == "02F3490407000000FF00800100020200", "single static send");
Run("effect", "WAVE", "0", "255", "0", "8");
Check(fake.Sends == 2 && fake.Last![11] == 2 && fake.Last[12] == 8, "effect dispatch");
Run("list"); Check(fake.Lists == 1, "list dispatch");
var single = Lighting.BuildReports(Lighting.Build(0,0,255), false);
Check(single.Length == 1 && Convert.ToHexString(single[0]) == "02F34904070000000000FF0100020200", "default single isolated report");
var applying = Lighting.BuildReports(Lighting.Build(0,0,255), true);
Check(applying.Length == 2 && applying[0].SequenceEqual(single[0]) &&
    Convert.ToHexString(applying[1]) == "02F10204000000000000000000000000", "apply sequence exact bytes; no other-profile writes");
Reject(() => Lighting.BuildReports(new byte[16], true), "reject nonlighting report");
var previewOutput = new StringWriter();
Cli.Run(new[] {"preview", "0", "0", "255", "--apply"}, fake, previewOutput);
Run("preview-effect", "wave", "255", "0", "0", "4", "--apply");
Check(fake.Sends == 2 && fake.Lists == 1 && previewOutput.ToString().Split(
    Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).SequenceEqual(
    new[] {"02F34904070000000000FF0100020200", "02F10204000000000000000000000000"}), "apply previews offline with exact sequence");
Run("set", "0", "0", "255", "--apply");
Check(fake.Sends == 3 && fake.Apply, "apply transport dispatch");
Reject(() => Run("set", "0", "0", "255", "--apply", "--apply"), "duplicate apply option");
Reject(() => Run("preview", "--apply", "0", "0", "255"), "misplaced apply option");
var target = new DeviceIdentity(0x04d9,0xfc30,@"\\?\hid#vid_04d9&pid_fc30&mi_02#abc", 16);
Check(DeviceSelection.IsTarget(target), "MI_02 target");
foreach (var d in new[] {
    target with { VendorId = 1 }, target with { ProductId = 1 },
    target with { Path = "mi_020" }, target with { Path = "xmi_02" },
    target with { Path = "hid#mi_01#abc" } }) Check(!DeviceSelection.IsTarget(d), "reject wrong interface");
Check(DeviceSelection.Select(new[] {target with {ProductId=1}, target}) == 1, "unique selection");
var col01 = target with { Path = @"\\?\hid#vid_04d9&pid_fc30&mi_02&col01#8&159ad57e&0&0000", FeatureReportLength = 0 };
var col02 = target with { Path = @"\\?\hid#vid_04d9&pid_fc30&mi_02&col02#8&159ad57e&0&0001", FeatureReportLength = 2048 };
Check(DeviceSelection.Select(new[] {
    target with { Path = "hid#mi_01#abc", FeatureReportLength = 0 }, col01, col02,
    target with { Path = "hid#mi_00#abc", FeatureReportLength = 0 } }) == 2,
    "reported four-collection layout selects col02");
Check(DeviceSelection.Select(new[] {col02, col01}) == 0, "selection independent of enumeration order");
foreach (var devices in new[] {
    Array.Empty<DeviceIdentity>(), new[] {target,target}, new[] {col01},
    new[] {target with { FeatureReportLength = 15 }},
    new[] {col02, col02 with { Path = col02.Path + "other-mouse" }},
    new[] {col02, col01 with { FeatureReportLength = null }} }) {
    bool rejected = false;
    try { DeviceSelection.Select(devices); } catch (InvalidOperationException) { rejected = true; }
    Check(rejected, "missing/ambiguous selection");
}
// No HidTransport instance is created by this test executable.
Console.WriteLine($"PASS: {checks} verificações; nenhum dispositivo acessado.");
sealed class FakeTransport : ILedTransport {
    public int Lists; public int Sends; public byte[]? Last; public bool Apply;
    public int List(TextWriter output) { Lists++; return 0; }
    public void Send(byte[] report, TextWriter output, bool apply = false) { Sends++; Last = report.ToArray(); Apply = apply; }
}
