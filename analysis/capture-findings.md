# Análise de cobra-leds.pcapng

Arquivo fornecido pelo usuário: `C:\Users\Guilherme\Documents\cobra-leds.pcapng`.
Análise somente de arquivo, sem acesso HID. Os números abaixo são frames da captura.
SHA-256: `6BD0A5B162BF5B4E6D6076E70D4304994C9FA1FBF5C47C4C7FF0947BCA9C1441`.

Há cinco sequências curtas com oito SET_REPORT cada:

| Frames | Perfil 1 |
|---|---|
| 7735–7749 | vermelho, breathing, velocidade 4 |
| 8717–8731 | vermelho, static, velocidade 0 |
| 15683–15699 | verde, static, velocidade 0 |
| 15897–15913 | azul, static, velocidade 0 |
| 15915–15929 | azul, static, velocidade 0 |

Cada sequência tem a mesma estrutura:

1. Escrita em 0x0446: `02F34604020000000000000000000000`.
2. Iluminação do perfil 1 em 0x0449, com RGB/modo/velocidade variáveis.
3. Quatro escritas de iluminação dos demais perfis, em 0x0451, 0x0459, 0x0461 e 0x0469.
4. `02F10204000000000000000000000000`.
5. `02F32C00020000000000000000000000`.

Os envios têm bmRequestType 0x21, bRequest 0x09, wValue 0x0302,
wIndex 2 e wLength 16. Os campos USBPcap identificam barramento 7, dispositivo 2.
O intervalo entre escritas de iluminação é aproximadamente 31 ms; a seleção de
perfil ocorre aproximadamente 1 ms depois de 02 F1 02 04.

Também há um bloco maior, nos frames 10125–10329, com configurações gerais,
DPI e botões. Esse bloco não é necessário para reproduzir as cinco sequências
curtas e não foi incorporado ao transporte.

## Evidência e limites

O usuário relata aplicação imediata com a implementação inicial de oito relatórios
e aplicação somente após reconexão com o relatório isolado atual. A captura confirma
os comandos adicionais, mas não contém uma experiência em que cada um é omitido
separadamente. Não permite identificar causalmente qual comando é necessário.

No [data.cpp do M711](https://github.com/dokutan/mouse_m908/blob/master/include/m711/data.cpp),
0x002C aparece na sequência de seleção de perfil; o
[writer](https://github.com/dokutan/mouse_m908/blob/master/include/m711/writers.cpp)
altera seu byte 8 para escolher o perfil. Não é um comando exclusivo de LEDs.
02 F1 02 04 também aparece nesse contexto, mas sua função isolada não é descrita.

Teste realizado pelo usuário: iluminação do perfil 1 seguida apenas de 02 F1 02 04,
usando --apply. O usuário confirmou que a cor foi aplicada imediatamente sem
reconectar o USB. Isso valida essa sequência para cor no mouse testado; não
caracteriza a função geral do comando nem comprova aplicação imediata dos efeitos.
Em rodada posterior, o usuário confirmou aplicação imediata de `breathing` e
observou os demais efeitos usando `--apply`; consulte a tabela de resultados no README.
Não houve envio automático durante análise, build ou testes. Não envia os outros perfis,
a seleção de perfil ou a escrita de significado desconhecido em 0x0446.

## Reprodução da extração

`extract_reports.py` lê os blocos pcapng e cabeçalhos USBPcap desta captura e grava
`set-reports.json` com os SET_REPORT observados. É uma ferramenta de análise desta
captura little-endian USBPcap, não um decodificador genérico de pcapng.

```powershell
python analysis/extract_reports.py 'C:\Users\Guilherme\Documents\cobra-leds.pcapng'
```
