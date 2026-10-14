// SPDX-License-Identifier: GPL-3.0-or-later
using System.Globalization;
namespace CobraLed;
public static class Cli
{
    public const string Help = """
        CobraLed — Redragon Cobra M711 V2 (04D9:FC30 / MI_02), perfil 1
          list                              Lista interfaces sem enviar comandos.
          modes                             Lista modos e status de validação.
          preview R G B                     Mostra cor estática sem acessar HID.
          set R G B                         Envia somente a cor do perfil 1.
          preview-effect MODE R G B SPEED   Mostra efeito sem acessar HID.
          effect MODE R G B SPEED           Envia efeito interno do perfil 1.
        RGB: 0..255. SPEED: 1..8. Use os nomes de modes.
        Feche o software Redragon antes de set/effect. As configurações podem persistir.
        Sem --apply, cor e efeitos exigiram reconexão USB no mouse testado.
        Opção --apply ao final de set/effect ou seus previews: acrescenta 02 F1 02 04.
        Cor e efeitos observados com --apply no mouse testado; veja resultados no README.
        wave_legacy permanece experimental; persistência ainda não verificada modo a modo.
        """;
    public static int Run(string[] args, ILedTransport transport, TextWriter output)
    {
        if (args.Length == 0) { output.WriteLine(Help); return 0; }
        var command = args[0].ToLowerInvariant();
        if (command is "help" or "--help") { Count(args, 1); output.WriteLine(Help); return 0; }
        if (command == "list") { Count(args, 1); return transport.List(output); }
        if (command == "modes") {
            Count(args, 1);
            foreach (var mode in Lighting.Modes)
                output.WriteLine($"{mode.Name,-20} {mode.First:X2}/{mode.Second:X2} " +
                    (mode.ValidatedAfterReconnect
                        ? "validado pelo usuário após reconexão USB"
                        : "experimental; comportamento distinto ainda não confirmado"));
            return 0;
        }
        bool apply = args[^1] == "--apply";
        if (apply) args = args[..^1];
        byte[] report;
        switch (command) {
            case "preview": case "set":
                Count(args, 4);
                report = Lighting.Build(Rgb(args[1]), Rgb(args[2]), Rgb(args[3])); break;
            case "preview-effect": case "effect":
                Count(args, 6);
                report = Lighting.Build(Rgb(args[2]), Rgb(args[3]), Rgb(args[4]),
                    args[1], Number(args[5], 1, 8, "Velocidade")); break;
            default: throw new ArgumentException("Comando desconhecido. Use help.");
        }
        foreach (var packet in Lighting.BuildReports(report, apply))
            output.WriteLine(Convert.ToHexString(packet));
        if (command is "preview" or "preview-effect") return 0;
        transport.Send(report, output, apply);
        output.WriteLine(apply
            ? "SetFeature aceito pelo Windows com --apply. Confira visualmente o resultado no perfil 1, sem reconectar o USB."
            : "SetFeature aceito pelo Windows. No mouse testado, retire e recoloque o USB para aplicar; confira visualmente no perfil 1.");
        return 0;
    }
    private static byte Rgb(string text) => (byte)Number(text, 0, 255, "RGB");
    private static int Number(string text, int min, int max, string field) =>
        int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value)
        && value >= min && value <= max ? value :
        throw new ArgumentException($"{field}: informe um inteiro de {min} a {max}.");
    private static void Count(string[] args, int count) {
        if (args.Length != count) throw new ArgumentException("Quantidade incorreta de argumentos. Use help.");
    }
}
