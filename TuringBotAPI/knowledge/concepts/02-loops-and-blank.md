---
id: concepts-loops-and-blank
category: concepts
title: Loops e o vazio no fim da fita
level_id:
---

Quando o comprimento da entrada é desconhecido, não dá para colocar um bloco de movimento por posição na esteira. O caminho certo é montar um loop: um fio que volta para um bloco que já rodou.

O material vazio marca o fim da entrada. Configure uma condição para “vazio”: porta verdadeira sai do loop (aceitar, ajustar material no fim etc.); porta falsa continua avançando.

Um loop típico: mover, (talvez ajustar material), inspecionar vazio e, se não for vazio, voltar a mover. Sem a saída no vazio, a esteira entra em ciclo infinito.
