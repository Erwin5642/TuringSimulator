---
id: errors-forgotten-blank
category: errors
title: Varreu e esqueceu o vazio
level_id:
---

Sintoma:
O circuito trata as peças e depois se perde no fim da linha, ou nunca para de avançar. Checar vazio na hora errada também faz a etapa de material nunca rodar nas peças certas.

Causa na bancada:
Varrer a esteira até o fim e esquecer o caso vazio. O vazio marca ausência de peça e costuma ser a saída do loop. Sem tratar vazio, não há ramo para encerrar a varredura.

Como tratar:
Pergunte quando a condição de vazio dispara. Depois de mover e, se for o caso, ajustar material, o vazio deve sair do loop. Não cheque vazio antes da peça que ainda precisa de trabalho.

Como notar na bancada:
- Loop de movimento sem condição de vazio.
- Condição de vazio cedo demais, antes de mover ou antes de ajustar o material.

Não faça:
- Não desenhe o loop completo. Só o caso vazio que falta.
