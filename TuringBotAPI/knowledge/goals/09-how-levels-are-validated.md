---
id: goals-how-levels-are-validated
category: goals
title: Como o nível é validado
level_id:
---

A esteira fica vazia enquanto você edita. Quando aperta Começar, a fábrica escolhe um lote ao acaso e coloca na esteira. O lote visível é só um dos lotes do nível.

O circuito precisa lidar com todos os lotes, não só com o que apareceu agora. Um Começar que deu certo não fecha o nível. Se qualquer lote falhar, a fábrica recusa o circuito.

Não existe lote principal fixo. Recomeçar e um novo Começar trazem outro lote, também ao acaso.

Quando o circuito não passa, o operário não diz qual lote falhou, não lista os lotes escondidos e não descreve a entrada problemática. O trainee descobre o buraco sozinho: aperte Começar de novo e observe a esteira. Se o lote visível pareceu certo e a fábrica mesmo assim recusou, o circuito ainda não cobre todos os casos.

Quando o sinal chega em Aceitar, a entrada daquele lote foi aprovada. Se chega em Rejeitar, aquele lote falhou. Se a energia fica sem saída, o programa só encerra — a peça não vai para bloco nenhum. Esse encerramento não é Aceitar; a máquina conta como rejeitar. Se o programa não para, aquele lote falha.
