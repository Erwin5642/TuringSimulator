---
id: concepts-loops-and-blank
category: concepts
title: Loops e o branco no fim da fita
level_id:
---

Quando o comprimento da esteira é desconhecido, não dá para colocar um bloco de movimento por célula. O jeito é o loop: um fio que volta para um bloco que já rodou.

O branco (slot vazio) marca o fim do lote. Configure uma condição para “branco”: porta verdadeira sai do loop (aceitar, escrever no fim, etc.); porta falsa continua andando.

Um loop típico: mover, (talvez escrever), inspecionar branco, se não for branco voltar a mover. Sem a saída no branco, o robô anda para sempre.
