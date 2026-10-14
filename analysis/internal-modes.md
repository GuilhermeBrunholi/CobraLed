# Investigação de modos internos adicionais

Pesquisa em 14 de outubro de 2026. Nenhum acesso HID ou envio físico foi realizado.

## Fontes e resultado

- [mouse_m908/include/data.cpp](https://github.com/dokutan/mouse_m908/blob/master/include/data.cpp)
  define os 11 pares que já existiam no CobraLed. Não foi encontrado outro par nessa tabela.
- A [página oficial do Cobra V2](https://www.redragon.com.br/cobrav2) anuncia sete modos
  de iluminação, sem documentar seus bytes. Isso não prova que todos os 11 pares
  produzam comportamentos distintos, nem que exista um décimo segundo efeito.
- A captura original contém seis escritas de iluminação do perfil 1, todas com
  pares 01/02 (static) ou 01/04 (breathing). Não contém um modo desconhecido.
- O [header RedragonMouseController do OpenRGB](https://github.com/CalcProgrammer1/OpenRGB/blob/master/Controllers/RedragonController/RedragonMouseController.h)
  enumera Wave=00, RandomBreathing=01, Static=02, Breathing=04, Rainbow=08 e Flashing=10.
  O [writer correspondente](https://github.com/CalcProgrammer1/OpenRGB/blob/master/Controllers/RedragonController/RedragonMouseController.cpp)
  fixa o primeiro byte de modo em 01; seu Wave resulta em 01/00. Os outros pares
  já existem no CobraLed. O mesmo writer implementa aplicação com 02 F1 02 04.

## Candidato wave_legacy

Atualização dos testes do usuário: `wave_legacy` funcionou com pulsação parecida
com `breathing`. Houve relato de diferença na pulsação, mas ainda não está
estabelecido como um efeito distinto. Os resultados da rodada dos demais modos
estão na tabela de validação do README.

Foi adicionado apenas o par documentado **01/00**, chamado `wave_legacy` para
distingui-lo do `wave` 02/00 do mouse_m908. É um candidato de protocolo,
não uma promessa de efeito visual novo. O [relato de Wave no OpenRGB](https://gitlab.com/CalcProgrammer1/OpenRGB/-/issues/1531)
descreve comportamento de respiração em um M711; essa observação de outro usuário
não determina o resultado na revisão 0710 deste workspace.

O relatório preserva os demais campos da captura do V2, inclusive tamanho 07 e
byte 14=02. Não copia o relatório de seis bytes de dados nem a troca de perfil
feita pelo OpenRGB. Velocidade continua restrita a 1–8 do upstream.

Teste manual, com software Redragon fechado e perfil 1 ativo:

```powershell
# Previews offline:
dotnet run -- preview-effect wave_legacy 255 0 0 4 --apply
dotnet run -- preview-effect wave 255 0 0 4 --apply
# Executar um por vez e observar sem reconectar:
dotnet run -- effect wave_legacy 255 0 0 4 --apply
dotnet run -- effect wave 255 0 0 4 --apply
dotnet run -- effect breathing 255 0 0 4 --apply
# Restaurar cor estática:
dotnet run -- set 0 255 0 --apply
```

Registrar se wave_legacy é distinto, igual a wave, igual a breathing, sem efeito
ou exige reconexão. Se for distinto, comparar velocidades 1 e 8 em testes separados.
Para outros candidatos, capturar cada modo do software oficial mantendo RGB,
velocidade e perfil constantes; comparar offsets 11/13 do relatório em 0x0449.
Não há varredura de bytes desconhecidos, loops nem comandos de outros modelos.
