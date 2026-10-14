// SPDX-License-Identifier: GPL-3.0-or-later
using HidSharp;
using System.Text.RegularExpressions;
namespace CobraLed;
public interface ILedTransport {
    int List(TextWriter output);
    void Send(byte[] report, TextWriter output, bool apply = false);
}
public sealed record DeviceIdentity(int VendorId, int ProductId, string Path, int? FeatureReportLength = null);
public static class DeviceSelection
{
    public static bool IsTarget(DeviceIdentity d) => d.VendorId == 0x04D9 && d.ProductId == 0xFC30
        && Regex.IsMatch(d.Path, @"(?:^|[&#\\])mi_02(?=[&#\\]|$)", RegexOptions.IgnoreCase);
    public static int Select(IReadOnlyList<DeviceIdentity> devices) {
        if (devices.Any(d => IsTarget(d) && d.FeatureReportLength is null))
            throw new InvalidOperationException("Não foi possível consultar a capacidade de uma coleção MI_02. Encerrando sem envio; execute list.");
        var indices = Enumerable.Range(0, devices.Count).Where(i =>
            IsTarget(devices[i]) && devices[i].FeatureReportLength >= 16).ToArray();
        if (indices.Length != 1) throw new InvalidOperationException(
            $"Esperada exatamente uma coleção 04D9:FC30 / MI_02 com Feature Report de pelo menos 16 bytes; encontradas {indices.Length}. Execute list.");
        return indices[0];
    }
}
public sealed class HidTransport : ILedTransport
{
    public int List(TextWriter output) {
        RequireWindows();
        var devices = DeviceList.Local.GetHidDevices(0x04D9, 0xFC30).ToArray();
        output.WriteLine($"Interfaces encontradas: {devices.Length}");
        foreach (var d in devices) {
            output.WriteLine(d.DevicePath);
            try { output.WriteLine($"  Feature máximo: {d.GetMaxFeatureReportLength()} bytes"); }
            catch (Exception ex) { output.WriteLine($"  Consulta indisponível: {ex.Message}"); }
        }
        return devices.Length == 0 ? 1 : 0;
    }
    public void Send(byte[] report, TextWriter output, bool apply = false) {
        var reports = Lighting.BuildReports(report, apply);
        RequireWindows();
        var devices = DeviceList.Local.GetHidDevices(0x04D9, 0xFC30).ToArray();
        var identities = devices.Select(d => {
            var identity = new DeviceIdentity(d.VendorID, d.ProductID, d.DevicePath);
            if (!DeviceSelection.IsTarget(identity)) return identity;
            // Query only target collections; a failed query aborts before opening HID.
            return identity with { FeatureReportLength = d.GetMaxFeatureReportLength() };
        }).ToArray();
        var index = DeviceSelection.Select(identities);
        var device = devices[index];
        output.WriteLine($"Interface: {device.DevicePath}");
        output.WriteLine($"Feature máximo: {identities[index].FeatureReportLength} bytes; tamanho solicitado: {report.Length} bytes (Report ID 02).");
        using var stream = device.Open();
        // The collection maximum is a capability, not this report's wire length.
        // Request exactly the captured 16 bytes. No automatic retries.
        for (int i = 0; i < reports.Length; i++) {
            if (i > 0) Thread.Sleep(35);
            stream.SetFeature(reports[i], 0, reports[i].Length);
        }
    }
    private static void RequireWindows() {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Transporte HID disponível somente no Windows.");
    }
}
