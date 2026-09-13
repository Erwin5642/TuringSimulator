---
id: errors-infinite-loop
category: errors
title: Ciclo infinito na esteira
level_id:
---

Sintoma:
A esteira não para. A fábrica corta a execução e aquele lote falha.

Causa na bancada:
Loop sem saída no vazio; fio que volta sem nunca mover a esteira; ou o bloco de materiais reescreve o mesmo material e a condição sempre toma o mesmo ramo. Sem a saída no vazio, a esteira entra em ciclo infinito.

Como tratar:
Pergunte o que faz o sinal deixar o loop. Tem que haver movimento e um ramo que não volta — em geral o vazio, ou Aceitar/Rejeitar.

Como notar na bancada:
- Fio de volta para o mesmo bloco sem condição de saída.
- Loop que só inspeciona e não move.
- Escrita que mantém a peça igual à do cartão da condição que reentra.

Não faça:
- Não cite limite interno de passos. Diga que a fábrica corta se o programa não para.
