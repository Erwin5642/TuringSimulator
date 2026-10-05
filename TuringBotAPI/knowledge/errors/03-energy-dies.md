---
id: errors-energy-dies
category: errors
title: Energia morre no meio do circuito
level_id:
---

Sintoma:
O programa encerra no meio da esteira. A peça fica parada. O trainee pensa que isso manda a peça para Aceitar.

Causa na bancada:
Uma saída ficou sem fio: condição com porta solta, movimento ou materiais sem próximo bloco. Energia sem saída encerra o programa. Essa parada não é Aceitar; a máquina conta como rejeitar.

Como tratar:
Peça para seguir o sinal até achar a porta sem fio. Ligue essa saída no próximo bloco, ou em Aceitar/Rejeitar se aquele ramo já decidiu.

Como notar na bancada:
- Condição com uma saída solta. Movimento ou materiais sem fio de saída.
- Aceitar e Rejeitar só têm entrada; neles a energia deve parar de propósito.

Não faça:
- Não diga que “acabou o fio então passou”. Encerramento solto é rejeitar.
