# CobraLed

CLI C# / .NET 10 para iluminação do perfil 1 do Redragon Cobra M711 V2 no Windows.
Dispositivo validado fisicamente: VID **04D9**, PID **FC30**, revisão **0710**, interface de configuração **MI_02**.
Usa HidSharp 2.1.0 com o driver HID existente.

## Compilar e testar sem acessar o mouse

```powershell
dotnet restore
dotnet build --no-restore
dotnet restore tests/CobraLed.Tests.csproj
dotnet run --project tests/CobraLed.Tests.csproj
dotnet run -- preview 0 255 0
dotnet run -- modes
dotnet run -- preview-effect wave 255 0 0 4
```

Os testes são um executável de verificações sem dependências de framework de testes;
o comando acima falha com código diferente de zero se alguma verificação falhar.
Usam transporte falso e nunca enumeram, abrem ou enviam comandos a dispositivos.
Verificação local realizada: restore, builds Debug/Release e 100 verificações
aprovadas, com zero avisos e erros de compilação. Nenhum comando foi enviado ao mouse.
`preview`, `preview-effect`, `modes` e `help` também não acessam HID.
`list` enumera somente 04D9:FC30 e consulta capacidades, sem enviar configurações.

## Comandos

```text
list
modes
preview R G B
set R G B
preview-effect MODE R G B SPEED
effect MODE R G B SPEED
help
```

RGB aceita inteiros decimais de 0 a 255; SPEED aceita 1 a 8, conforme o upstream.
Velocidade é o byte bruto do protocolo: em `breathing`, o usuário confirmou que
1 pulsa mais rápido e 8 mais lentamente. A relação nos demais modos ainda não foi
comparada. Nomes de comandos e modos ignoram maiúsculas/minúsculas.
Argumentos faltantes, extras, modos desconhecidos e valores inválidos encerram antes
de acessar HID. `set` usa modo static e velocidade 0, preservando a combinação validada.
Os 11 modos originais foram relatados como funcionais pelo usuário após reconexão USB.
Isso não valida todas as combinações de RGB e velocidade.

### Candidato adicional: wave_legacy

A investigação do OpenRGB encontrou o par `01/00`, diferente do `wave` original
(`02/00`). Foi adicionado como **wave_legacy experimental**. O usuário observou
pulsação semelhante a `breathing`, com alguma diferença percebida na pulsação.
Não está confirmado como um efeito distinto nem como uma onda no V2.
Veja [fontes e roteiro de comparação](analysis/internal-modes.md).

```powershell
dotnet run -- preview-effect wave_legacy 255 0 0 4 --apply
dotnet run -- effect wave_legacy 255 0 0 4 --apply
```

Compare manualmente com `wave` e `breathing` usando o mesmo RGB e velocidade.

### Aplicação imediata com --apply

A opção `--apply`, ao final de `set`, `effect`, `preview` ou `preview-effect`,
acrescenta somente `02 F1 02 04 00 00 00 00 00 00 00 00 00 00 00 00` após
o relatório de iluminação. Esse comando aparece no fim das cinco sequências curtas
da captura original. O usuário confirmou que essa sequência aplica a cor
imediatamente, sem reconectar o USB, no mouse testado. Uma rodada posterior
caracterizou também os efeitos usando `--apply`, conforme a tabela abaixo.
O usuário também confirmou que `random` permaneceu após reconectar o USB.
A persistência dos demais efeitos ainda não foi testada sistematicamente.
O envio tem intervalo de 35 ms, sem repetições nem fallback automático.
Não inclui a escrita em 0x0446, outros perfis ou a seleção do perfil ativo.

```powershell
# Sem acessar HID: conferir os dois relatórios.
dotnet run -- preview 0 0 255 --apply
# Teste manual: observar azul imediatamente, SEM reconectar primeiro.
dotnet run -- set 0 0 255 --apply
```

Use uma cor diferente da atual e mantenha o software Redragon fechado.
Se não mudar imediatamente, informe o resultado antes de reconectar. Se mudar,
confirme com outra cor e depois com um efeito:

```powershell
dotnet run -- set 255 0 0 --apply
dotnet run -- effect breathing 255 0 0 4 --apply
```

Os comandos sem `--apply` preservam o envio isolado já validado.
Consulte [a análise da captura](analysis/capture-findings.md).

## Validação física manual

Feche completamente o software Redragon, inclusive na bandeja. Use o perfil 1;
CobraLed não troca o perfil ativo. Execute cada comando separadamente e observe
cor, comportamento e velocidade antes de passar ao seguinte. No mouse testado,
sem `--apply`, foi necessário retirar e recolocar o USB após cada envio.
Para testes com aplicação, acrescente `--apply` ao final dos comandos abaixo:

```powershell
dotnet run -- list
dotnet run -- set 0 255 0

# Efeitos internos; um comando por teste, seguido de reconexão USB:
dotnet run -- effect off 255 0 0 4
dotnet run -- effect static 255 0 0 4
dotnet run -- effect breathing 255 0 0 4
dotnet run -- effect breathing_rainbow 255 0 0 4
dotnet run -- effect rainbow 255 0 0 4
dotnet run -- effect flashing 255 0 0 4
dotnet run -- effect wave 255 0 0 4
dotnet run -- effect reactive_button 255 0 0 4
dotnet run -- effect random 255 0 0 4
dotnet run -- effect alternating 255 0 0 4
dotnet run -- effect reactive 255 0 0 4

# Voltar à cor estática validada:
dotnet run -- set 0 255 0
```

Para modos reativos, observe também o resultado ao mover o mouse e clicar.
Alguns modos podem ignorar RGB. Após um efeito funcionar, compare SPEED 1 e 8
manualmente. Registre modo, RGB, velocidade, comportamento, saída do comando e
persistência após desconectar/reconectar. Sucesso de SetFeature não comprova o efeito.

**Validado fisicamente pelo usuário:** relatório isolado de 16 bytes, seleção
MI_02/col02, cor estática e funcionamento dos 11 modos originais após retirar e
recolocar o USB no M711 V2 testado. A persistência da cor também foi confirmada.
**Aplicação imediata validada:** cor estática com `--apply`, que envia o relatório
de iluminação seguido de `02 F1 02 04 ...`, sem reconexão USB. `breathing` também
teve aplicação sem reconexão explicitamente confirmada; os demais efeitos abaixo
foram observados na rodada de comandos com `--apply`.
**Limitação do envio isolado:** sem `--apply`, cor e efeitos exigiram reconexão USB.
O usuário confirmou que a versão inicial de oito relatórios aplicava a cor sem
reconexão. A necessidade de reconectar sem `--apply` é uma regressão da sequência reduzida,
não uma limitação comprovada do hardware.
**Ainda sem caracterização:** velocidade dos modos além de `breathing`, todas as
combinações de RGB/velocidade, `effect static` com velocidade 1–8 e aplicação com
`--apply`, e persistência de cada modo individual em reconexões posteriores sem novo envio.
O usuário confirmou persistência de `random` após reconexão.
Não houve teste físico automático. `wave_legacy` foi observado, mas permanece
experimental por não estar estabelecido como efeito distinto.

### Resultados observados pelo usuário em 14/10/2026

Dispositivo 04D9:FC30, revisão 0710, perfil 1; comandos executados individualmente
com `--apply`. Efeitos usaram velocidade 4, exceto a comparação de `breathing`.
São observações visuais, não medições de frequência nem garantia para outras revisões.

| Comando/modo | Parâmetros | Resultado |
|---|---|---|
| set (static) | RGB 0/255/0, velocidade 0 | Mouse inteiro verde, continuamente aceso, sem reconexão. |
| breathing | RGB 255/0/0, velocidades 1 e 8 | Aplicou sem reconexão; 1 rápido, 8 mais lento. |
| wave | RGB 255/0/0 e 0/0/255 | Várias cores; trocar vermelho por azul não mudou cores nem movimento. |
| reactive_button | RGB 0/255/0 | Luz sempre acesa; muda de cor ao clicar. |
| reactive | RGB 0/255/0 | Apagado em repouso; troca de cor ao mover ou clicar. |
| alternating | RGB 255/0/0 | Troca de cor em forma de ondas. |
| random | RGB 255/0/0 | Uma parte pisca enquanto outra permanece apagada; efeito permanece após reconectar o USB. |
| breathing_rainbow | RGB 255/0/0 | Mouse inteiro pulsa junto e muda de cor entre pulsações. |
| rainbow | RGB 255/0/0 | Transição suave entre cores, sempre aceso. |
| flashing | RGB 255/0/0 | Pisca somente em vermelho, respeitando o RGB nesse teste. |
| off | RGB 255/0/0 | Completamente apagado. |
| wave_legacy | RGB 255/0/0 | Em teste anterior, pulsação parecida com breathing; distinção ainda inconclusiva. |

Os resultados de `wave` sugerem que o RGB é ignorado nesse modo, mas a comparação
foi limitada a vermelho e azul. O movimento por partes em `random` comprova um
efeito interno com partes diferentes, não controle individual de LEDs pelo protocolo.

## Protocolo e escopo

Por padrão, é enviado exatamente um relatório lógico de 16 bytes:

```text
02 F3 49 04 07 00 00 00 RR GG BB MM SS NN 02 00
```

RGB fica nos offsets 8/9/10; modo nos offsets 11/13; velocidade no offset 12.
Todos os demais campos permanecem iguais à captura validada. Não envia inicialização,
configurações completas, DPI, botões ou iluminação de outros perfis.
Somente com `--apply` é enviado também o comando de aplicação descrito acima.
O transporte solicita `SetFeature` com exatamente os 16 bytes, incluindo Report ID 02,
sem completar o buffer até os 2048 bytes anunciados pela coleção. Esse máximo é
usado para verificar capacidade e selecionar a coleção, não como tamanho do pacote.
O tamanho efetivo na transferência USB só pode ser confirmado por captura.

### Diagnóstico: envio aceito sem mudança visual

A versão anterior completava o relatório com zeros até 2048 bytes. O usuário
relatou que o envio era aceito sem mudar a cor. Depois da mudança para 16 bytes,
confirmou o funcionamento da cor e de todos os modos após reconectar o USB.
Sem reconexão, o envio isolado continuou sem aplicação visual. Em teste posterior,
o usuário confirmou cor imediata com `--apply`. Os testes não isolam o efeito do
padding anterior, nem caracterizam todos os efeitos do comando adicional.
O programa agora imprime a capacidade e o tamanho solicitado e distingue aceitação
do Windows de aplicação pelo mouse. Não faz tentativas adicionais automáticas.
Feche o software Redragon, confira o perfil 1 ativo, teste uma cor diferente da atual
e use `--apply` para cor imediata; o envio isolado exige reconexão no mouse testado.
Se não funcionar, compare com o comando/código do teste isolado que funcionou.
Uma captura USBPcap pode conferir `SET_REPORT`, `wValue=0x0302`, `wIndex=2`,
`wLength=16` e os bytes efetivamente enviados, sem enviar configurações adicionais.

### Comparação com a versão inicial

A sequência inicial tinha oito relatórios: escrita em 0x0446, iluminação do perfil 1
em 0x0449, iluminação dos outros quatro perfis em 0x0451/0x0459/0x0461/0x0469,
`02 F1 02 04 ...` e escrita em 0x002C. Os quatro relatórios de outros perfis
continuam excluídos.

No upstream, 0x002C é usado por `write_profile()` para selecionar o perfil (byte 8).
Portanto, o último relatório inicial não pode ser tratado como um simples comando
de atualização dos LEDs. `02 F1 02 04 ...` também aparece na sequência de seleção
de perfil, mas seu efeito isolado não é documentado. A escrita em 0x0446 precede
os blocos de iluminação no upstream; seu significado isolado também não é explicado.
O teste com apenas iluminação e `02 F1 02 04 ...` confirmou cor imediata.
As escritas em 0x0446 e 0x002C não foram necessárias nesse teste e permanecem excluídas.

A captura fornecida foi analisada em `analysis/capture-findings.md`.
O envio padrão permanece restrito ao relatório de iluminação. A opção explícita
`--apply` acrescenta apenas `02 F1 02 04 ...`; essa sequência de dois relatórios
foi validada para cor imediata e usada na rodada de efeitos descrita acima.

Os pares de modo vêm de
[include/data.cpp](https://github.com/dokutan/mouse_m908/blob/master/include/data.cpp).
[include/m711/writers.cpp](https://github.com/dokutan/mouse_m908/blob/master/include/m711/writers.cpp)
confirma os offsets, Feature Report ID 2 e interface USB 2. Seu envio completo de
configurações não é reproduzido. A tabela comum do upstream não prova suporte de
cada efeito no V2; a validação adicional dos 11 modos foi feita pelo usuário
no dispositivo identificado acima, sempre após reconexão USB.

| Modo | MM / NN |
|---|---|
| off | 00 / 00 |
| breathing_rainbow | 01 / 01 |
| static | 01 / 02 |
| breathing | 01 / 04 |
| rainbow | 01 / 08 |
| flashing | 01 / 10 |
| wave | 02 / 00 |
| reactive_button | 03 / 00 |
| random | 04 / 00 |
| alternating | 06 / 00 |
| reactive | 07 / 00 |
| wave_legacy (experimental, OpenRGB) | 01 / 00 |

O transporte aceita exclusivamente VID/PID 04D9:FC30 e o token MI_02 no caminho.
Somente coleções com Feature Report de pelo menos 16 bytes são candidatas.
Na listagem fornecida pelo usuário, MI_02/col01 tem capacidade zero e é descartada;
MI_02/col02 tem 2048 bytes e é selecionada. O número col02 não é fixado no código.
Zero ou múltiplas coleções compatíveis causam encerramento sem envio. Falha ao
consultar a capacidade de qualquer coleção MI_02 também encerra sem envio.
A revisão 0710 é a
revisão testada, não um filtro adicional. Não há fallback para outra interface.

Não há loops de RGB, reação à música, modo temporário conhecido, alteração de
firmware/driver ou GUI. As configurações podem persistir; execute os testes manualmente.

## Organização

- `Cli.cs`: argumentos, ajuda, roteamento e previews.
- `Lighting.cs`: modos e construção pura do relatório.
- `HidTransport.cs`: seleção restrita e envio HID único.
- `tests/Program.cs`: bytes completos, modos, limites, roteamento e ambiguidade.

## Publicar Windows x64

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Saída: `bin/Release/net10.0/win-x64/publish`.

## Licença

GPL-3.0-or-later; consulte [LICENSE](LICENSE) e [NOTICE.md](NOTICE.md).
Protocolo e mapeamentos derivados de [dokutan/mouse_m908](https://github.com/dokutan/mouse_m908).
