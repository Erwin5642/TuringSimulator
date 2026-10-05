---
id: errors-incomplete-branch
category: errors
title: Material ou lote não coberto
level_id:
---

Sintoma:
Um Começar parece certo e a fábrica mesmo assim recusa. Outro lote traz um material que o circuito ignora.

Causa na bancada:
Transição incompleta: a condição só cobre um material; o outro ramo não trata o que aparece na esteira. O circuito precisa lidar com todos os lotes, não só com o que está visível agora.

Como tratar:
Peça para apertar Começar de novo e olhar a esteira. Pergunte o que o ramo falso da condição faz quando a peça não é a do cartão.

Como notar na bancada:
- Condição com as duas saídas no mesmo bloco, ou um ramo que não move nem decide Aceitar/Rejeitar.
- Só um material tratado; os outros caem num encerramento solto.

Não faça:
- Não nomeie o lote que falhou nem descreva a entrada escondida.
