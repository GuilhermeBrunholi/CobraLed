# CobraLed

Controle a iluminação do **Redragon Cobra M711 V2** pelo terminal no Windows.
Escolha uma cor RGB, use os efeitos internos do mouse ou confira os comandos com
um preview antes de aplicá-los.

Escrito em C# com **.NET 10** e **HidSharp 2.1.0**, usando o driver HID existente.
O projeto configura a iluminação do **perfil 1**.

> **Compatibilidade validada:** M711 V2 com VID `04D9`, PID `FC30` e revisão `0710`.
> Outras revisões não foram validadas. Antes de aplicar configurações, feche o
> software Redragon, inclusive na bandeja, e selecione o perfil 1.

[Começar](#começar) · [Comandos](#comandos) · [Efeitos](#efeitos-disponíveis) ·
[Diagnóstico](#diagnóstico) · [Protocolo](#protocolo) · [Licença](#licença)

## Começar

Para compilar e executar a partir do código, tenha Git e o SDK .NET 10 instalados.
O envio de comandos ao mouse requer Windows.

```powershell
git clone https://github.com/GuilhermeBrunholi/CobraLed.git
cd CobraLed
dotnet restore
dotnet build --no-restore
```

Confira a cor sem acessar o mouse:

```powershell
dotnet run -- preview 0 255 0 --apply
```

Com o software Redragon fechado e o perfil 1 ativo, aplique verde:

```powershell
dotnet run -- set 0 255 0 --apply
```

Outros exemplos, para executar individualmente:

```powershell
# Azul contínuo
dotnet run -- set 0 0 255 --apply

# Vermelho pulsante
dotnet run -- effect breathing 255 0 0 4 --apply

# Transição de cores
dotnet run -- effect rainbow 255 0 0 4 --apply

# Apagar a iluminação
dotnet run -- effect off 0 0 0 4 --apply
```

### Aplicação imediata com `--apply`

Coloque `--apply` ao final de `set` ou `effect` para enviar o comando de aplicação
após a configuração de iluminação. A cor estática e `breathing` tiveram aplicação
imediata confirmada; os demais efeitos foram observados na rodada de testes com
essa opção.

Sem `--apply`, o programa envia apenas a configuração de iluminação. No mouse
testado, foi necessário desconectar e reconectar o USB para visualizar a mudança.
As configurações podem persistir: a persistência da cor estática e de `random`
foi confirmada, mas a dos demais efeitos ainda não foi testada sistematicamente.

`preview` e `preview-effect` também aceitam `--apply`: mostram os dois relatórios
que seriam enviados, sem acessar HID.

## Comandos

| Comando | O que faz | Acesso ao mouse |
|---|---|---|
| `help` | Mostra a ajuda. | Nenhum |
| `modes` | Lista modos e status de validação. | Nenhum |
| `list` | Lista interfaces compatíveis e capacidades. | Consulta, sem enviar configurações |
| `preview R G B` | Mostra o relatório de cor estática. | Nenhum |
| `set R G B` | Configura uma cor estática no perfil 1. | Envia configuração |
| `preview-effect MODE R G B SPEED` | Mostra o relatório de um efeito. | Nenhum |
| `effect MODE R G B SPEED` | Configura um efeito no perfil 1. | Envia configuração |

- **R, G e B:** inteiros de `0` a `255`.
- **SPEED:** inteiro de `1` a `8`. Em `breathing`, `1` pulsa mais rápido e `8`
  mais lentamente. A relação nos outros modos ainda não foi comparada.
- **MODE:** um dos nomes da tabela abaixo. Comandos e modos ignoram
  maiúsculas/minúsculas.
- **`--apply`:** opção ao final de `set`, `effect` e seus previews.

`set` usa `static` com velocidade `0`, a combinação validada para cor contínua.
Argumentos inválidos encerram a execução antes de acessar HID.

## Efeitos disponíveis

Os resultados abaixo foram observados manualmente em **14/10/2026**, no
M711 V2 `04D9:FC30`, revisão `0710`, perfil 1. A rodada usou `--apply` e velocidade
`4`, exceto a comparação de velocidade de `breathing` e o teste anterior de
`wave_legacy`. São observações visuais de um dispositivo.

| Modo | Comportamento observado |
|---|---|
| `static` | Cor contínua. Use `set R G B --apply` para a combinação validada. |
| `breathing` | Mouse inteiro pulsa na cor escolhida; velocidade 1 mais rápida que 8. |
| `breathing_rainbow` | Mouse inteiro pulsa junto e muda de cor entre pulsações. |
| `rainbow` | Transição suave entre cores, sempre aceso. |
| `flashing` | Pisca na cor escolhida; testado em vermelho. |
| `wave` | Várias cores; trocar RGB vermelho por azul não alterou cores nem movimento. |
| `reactive_button` | Luz sempre acesa, com mudança de cor ao clicar. |
| `reactive` | Apagado em repouso; troca de cor ao mover ou clicar. |
| `random` | Uma parte pisca enquanto outra permanece apagada; persistiu após reconexão USB. |
| `alternating` | Troca de cor em forma de ondas. |
| `off` | Iluminação completamente apagada. |
| `wave_legacy` | **Experimental:** pulsação semelhante a `breathing`; distinção inconclusiva. |

Os 11 modos originais funcionaram também após reconexão USB nos testes do envio
isolado. Isso não valida todas as combinações de RGB e velocidade. Alguns modos
podem ignorar RGB; em `wave`, a comparação foi limitada a vermelho e azul.
`effect static` com velocidade de 1 a 8 e `--apply` ainda não foi caracterizado.

`wave_legacy` usa um par de bytes encontrado no OpenRGB. Ainda não está confirmado
como um efeito distinto ou como uma onda no V2. Veja as
[fontes e o roteiro de comparação](analysis/internal-modes.md).

## Diagnóstico

Se o comando for aceito, mas a iluminação não mudar:

1. Feche completamente o software Redragon, inclusive na bandeja.
2. Confira se o perfil 1 está ativo. CobraLed não troca o perfil ativo.
3. Execute `dotnet run -- list` para conferir as interfaces encontradas.
4. Teste uma cor diferente da atual com `dotnet run -- set 0 0 255 --apply`.
5. Se estiver usando o envio sem `--apply`, desconecte e reconecte o USB,
   conforme necessário no dispositivo testado.

A mensagem de sucesso de `SetFeature` indica que o Windows aceitou o envio;
confira visualmente o resultado no mouse. O programa não repete o envio
nem tenta outras interfaces automaticamente.

Ao relatar um problema, inclua o modelo e a revisão do mouse, o comando usado,
a saída do terminal e se a mudança ocorreu antes ou depois de reconectar o USB.
Revise caminhos e identificadores locais antes de publicar logs ou capturas.

## Desenvolvimento e testes

```powershell
dotnet build -c Release
dotnet run --project tests/CobraLed.Tests.csproj
```

Os testes usam transporte falso e não enumeram, abrem ou enviam comandos a
dispositivos. O executável de testes encerra com código diferente de zero se
alguma verificação falhar. Os previews, `modes` e `help` também não acessam HID.

| Arquivo | Responsabilidade |
|---|---|
| `Program.cs` | Entrada do programa e tratamento de erros. |
| `Cli.cs` | Argumentos, ajuda, roteamento e previews. |
| `Lighting.cs` | Modos e construção dos relatórios. |
| `HidTransport.cs` | Seleção de interface e envio HID. |
| `tests/Program.cs` | Verificações de bytes, modos, limites e seleção de dispositivos. |
| `analysis/` | Análise da captura USB e investigação dos modos. |

### Gerar executável para Windows x64

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

O resultado fica em `bin/Release/net10.0/win-x64/publish/`. Essa publicação inclui
o runtime .NET. Na pasta de saída, execute:

```powershell
.\CobraLed.exe set 0 255 0 --apply
```

## Protocolo

O envio padrão contém um relatório lógico de 16 bytes, incluindo o Report ID `02`:

```text
02 F3 49 04 07 00 00 00 RR GG BB MM SS NN 02 00
```

RGB ocupa os offsets `8/9/10`, modo os offsets `11/13` e velocidade o offset `12`.
Os demais campos permanecem iguais à captura validada.

Com `--apply`, um segundo relatório é enviado após um intervalo de 35 ms:

```text
02 F1 02 04 00 00 00 00 00 00 00 00 00 00 00 00
```

O transporte aceita apenas VID/PID `04D9:FC30` e interface `MI_02`, selecionando
uma única coleção com Feature Report de pelo menos 16 bytes. Na unidade testada,
a coleção `col02` anuncia 2048 bytes, mas o programa solicita exatamente 16 bytes;
o número da coleção não é fixado no código. Zero ou múltiplas coleções compatíveis,
ou falha na consulta de capacidade de uma coleção `MI_02`, encerram sem envio.
A revisão `0710` identifica a unidade testada e não é um filtro do programa.

O tamanho efetivo da transferência USB pode ser confirmado por captura:
`SET_REPORT`, `wValue=0x0302`, `wIndex=2` e `wLength=16`.

<details>
<summary>Mapeamento dos modos em hexadecimal</summary>

| Modo | MM / NN |
|---|---|
| `off` | `00 / 00` |
| `breathing_rainbow` | `01 / 01` |
| `static` | `01 / 02` |
| `breathing` | `01 / 04` |
| `rainbow` | `01 / 08` |
| `flashing` | `01 / 10` |
| `wave` | `02 / 00` |
| `reactive_button` | `03 / 00` |
| `random` | `04 / 00` |
| `alternating` | `06 / 00` |
| `reactive` | `07 / 00` |
| `wave_legacy` (experimental) | `01 / 00` |

</details>

O projeto configura somente a iluminação do perfil 1. Não altera DPI, botões,
firmware ou driver, não seleciona o perfil ativo e não configura os outros perfis.
Os efeitos são internos ao mouse; não há controle individual de LEDs, reação à
música, loops de RGB, interface gráfica ou modo temporário conhecido.

A análise das sequências originais, incluindo comandos que ficaram fora do
transporte, está em [analysis/capture-findings.md](analysis/capture-findings.md).
A função geral do comando `02 F1 02 04` ainda não foi caracterizada; a sequência
implementada foi validada para cor imediata e usada nos testes de efeitos descritos.

## Licença

Distribuído sob **GPL-3.0-or-later**. Consulte [LICENSE](LICENSE) e as
[atribuições em NOTICE.md](NOTICE.md).

Os mapeamentos de protocolo foram adaptados de
[dokutan/mouse_m908](https://github.com/dokutan/mouse_m908).
O candidato `wave_legacy` foi identificado no
[OpenRGB](https://github.com/CalcProgrammer1/OpenRGB/tree/master/Controllers/RedragonController).
