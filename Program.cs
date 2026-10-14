// SPDX-License-Identifier: GPL-3.0-or-later
using CobraLed;
try { return Cli.Run(args, new HidTransport(), Console.Out); }
catch (Exception ex) { Console.Error.WriteLine($"Falha: {ex.Message}"); return 1; }
