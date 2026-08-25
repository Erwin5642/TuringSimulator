---
id: goals-how-levels-are-validated
category: goals
title: Como o nível é validado
level_id:
---

Cada nível tem um teste principal e mais quatro cenários de fita. São cinco fitas no total, cada uma com um começo de cabeçote e um resultado esperado: Aceitar ou Rejeitar (ou a fita final certa nos níveis de manipulação de materiais).

O circuito que você montou roda em todos os cenários. Passar em um só não fecha o nível. O programa precisa se comportar certo em fitas curtas, longas e nos casos-limite.

Quando o sinal chega em Aceitar, a entrada foi aprovada. Se chega em Rejeitar, a entrada falhou. Se a energia fica sem saída, o programa só encerra — a peça não vai para módulo nenhum. Esse encerramento não é Aceitar; a máquina conta como rejeitar. Se o programa não para, o cenário falha.
