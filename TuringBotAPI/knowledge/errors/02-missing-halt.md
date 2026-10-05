---
id: errors-missing-halt
category: errors
title: Falta Aceitar ou Rejeitar
level_id:
---

Sintoma:
O circuito percorre a esteira e encerra, mas a fábrica não registra aceite. O trainee acha que “parar” já vale.

Causa na bancada:
Falta estado final de Aceitar ou Rejeitar. Aceite só existe se o sinal chegar num bloco Aceitar. Sem esses blocos, a máquina não declara a linguagem.

Como tratar:
Pergunte se o nível pede aprovar ou recusar o lote. Se pede, o fio do fim útil precisa entrar em Aceitar ou em Rejeitar, não ficar solto.

Como notar na bancada:
- Não há bloco Aceitar nem Rejeitar, ou eles existem mas nenhum fio chega neles.
- Níveis sem esses blocos (só movimento) não precisam disto.

Não faça:
- Não monte o critério inteiro do nível. Só lembre que parar no meio não é Aceitar.
