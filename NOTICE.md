# Atribuição e licença

CobraLed é distribuído sob GNU General Public License, versão 3 ou qualquer versão
posterior (SPDX: GPL-3.0-or-later), sem garantia. Consulte LICENSE.

Os mapeamentos de iluminação e sua aplicação aos offsets do relatório M711 foram
adaptados do projeto mouse_m908, de dokutan e colaboradores:
https://github.com/dokutan/mouse_m908

Arquivos consultados em 14 de outubro de 2026 (branch master):
- include/data.cpp: _c_lightmode_values, _c_lightmode_strings, _c_speed_min/max.
- include/m711/writers.cpp: offsets de RGB, modo e velocidade, Report ID e interface.

Candidato experimental wave_legacy (01/00) identificado no código OpenRGB, de
Adam Honse (CalcProgrammer1) e colaboradores:
https://github.com/CalcProgrammer1/OpenRGB/tree/master/Controllers/RedragonController
RedragonMouseController.h/.cpp e RGBController_RedragonMouse.cpp, consultados
em 14 de outubro de 2026. Esses arquivos declaram GPL-2.0-or-later; CobraLed
continua distribuído sob GPL-3.0-or-later. O par foi documentado por comparação
de protocolo; não foi incorporada a troca de perfil ou a escrita completa do OpenRGB.

O upstream declara GPL versão 3 ou, a seu critério, qualquer versão posterior.
CobraLed preserva essa licença e atribuição. A adaptação implementa somente o
relatório isolado de iluminação do perfil 1 no Windows e não reproduz a sequência
completa de configurações do upstream.
A opção --apply usa o comando 02 F1 02 04 observado na captura original;
a sequência iluminação seguida desse comando foi validada pelo usuário para
aplicação imediata de cor e usada nos testes de efeitos documentados no README.
A função geral do comando ainda não foi caracterizada. O usuário confirmou
persistência de `random` após reconexão; a verificação dos demais efeitos
continua pendente. Não inclui escrita de seleção de perfil.

A base do relatório de 16 bytes e a validação física de cor e persistência foram
fornecidas pelo usuário a partir de captura e testes no M711 V2 04D9:FC30, revisão 0710.

Dependência: HidSharp 2.1.0, de James F. Bellinger / Zero7, distribuída sob Apache
conforme os metadados do pacote NuGet (mudança de licença registrada na versão 2.0).
Licença da dependência: http://www.zer7.com/files/oss/hidsharp/LICENSE.txt
Sua licença própria não altera a licença GPL-3.0-or-later do código CobraLed.
