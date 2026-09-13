---
id: errors-tape-as-memory
category: errors
title: Esteira como memória
level_id:
---

Sintoma:
O circuito tenta lembrar de cabeça quantas peças viu e se perde. Ou apaga a entrada cedo demais. Ou um lote todo vazio quebra o que só funcionava com peças na frente.

Causa na bancada:
Tratar a máquina como autômato finito: só estados no circuito, sem recado na esteira. Colocar ou remover material é memória. Marcar peça já vista evita contar de novo. Lote vazio também é caso: às vezes Aceitar, às vezes Rejeitar, conforme o objetivo.

Como tratar:
Pergunte como o circuito vai reconhecer uma peça já casada na próxima passada. Se precisa lembrar, ajuste o material já visto em vez de só contar no fio. Confira também o que fazer quando a posição atual já é vazio.

Como notar na bancada:
- Várias passadas sem bloco de materiais para marcar.
- Materiais que apagam a entrada antes de terminar de inspecionar.
- Nenhum ramo para esteira já vazia no Começar.

Não faça:
- Não monte o esquema de marcação do nível. Só lembre que a esteira guarda recado.
